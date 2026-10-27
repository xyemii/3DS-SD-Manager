// ============================================================
//  3DS SD Manager - DiskTools.cs
//  FAT32 formatter and SD card capacity test
// ============================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace SdManager3ds
{
    internal static class Native
    {
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 1;
        public const uint FILE_SHARE_WRITE = 2;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode,
            byte[] lpInBuffer, int nInBufferSize, byte[] lpOutBuffer, int nOutBufferSize,
            out int lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(IntPtr a, byte[] b, UIntPtr count);
    }

    // ------------------------------------------------------------
    //  FAT32 formatter (for cards > 32 GB, which Windows no longer formats as FAT32)
    // ------------------------------------------------------------
    internal static class FatFormatter
    {
        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint FSCTL_ALLOW_EXTENDED_DASD_IO = 0x00090083;
        private const uint IOCTL_DISK_GET_DRIVE_GEOMETRY = 0x00070000;
        private const uint IOCTL_DISK_GET_PARTITION_INFO_EX = 0x00070048;
        private const uint IOCTL_DISK_GET_LENGTH_INFO = 0x0007405C;
        private const uint IOCTL_DISK_SET_PARTITION_INFO_EX = 0x007C0CC;

        private static void PutU16(byte[] b, int off, int v)
        {
            b[off] = (byte)(v & 0xFF);
            b[off + 1] = (byte)((v >> 8) & 0xFF);
        }

        private static void PutU32(byte[] b, int off, uint v)
        {
            b[off] = (byte)(v & 0xFF);
            b[off + 1] = (byte)((v >> 8) & 0xFF);
            b[off + 2] = (byte)((v >> 16) & 0xFF);
            b[off + 3] = (byte)((v >> 24) & 0xFF);
        }

        private static void WriteAt(FileStream fs, long offset, byte[] data)
        {
            fs.Position = offset;
            fs.Write(data, 0, data.Length);
        }

        private static void WriteZeros(FileStream fs, long offset, long length)
        {
            byte[] z = new byte[1024 * 1024];
            long done = 0;
            while (done < length)
            {
                int n = (int)Math.Min((long)z.Length, length - done);
                fs.Position = offset + done;
                fs.Write(z, 0, n);
                done += n;
            }
        }

        public static string CleanLabel(string label)
        {
            if (label == null) { label = ""; }
            StringBuilder sb = new StringBuilder();
            foreach (char c in label.ToUpperInvariant())
            {
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' || c == '_' || c == '-') { sb.Append(c); }
                else { sb.Append('_'); }
                if (sb.Length >= 11) { break; }
            }
            string s = sb.ToString().Trim();
            if (s.Length == 0) { s = "3DS"; }
            return s;
        }

        public static void Format(char letter, string label, Action<string> log)
        {
            label = CleanLabel(label);
            string path = "\\\\.\\" + char.ToUpperInvariant(letter) + ":";
            SafeFileHandle h = Native.CreateFile(path, Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
            if (h.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException("Could not open the drive (Windows error " + err + "). Is the app running as administrator?");
            }

            FileStream fs = null;
            try
            {
                int ret;
                byte[] geo = new byte[24];
                if (!Native.DeviceIoControl(h, IOCTL_DISK_GET_DRIVE_GEOMETRY, null, 0, geo, geo.Length, out ret, IntPtr.Zero))
                {
                    throw new IOException("Could not read the disk geometry.");
                }
                int bps = BitConverter.ToInt32(geo, 20);
                if (bps < 512 || bps > 4096 || (bps & (bps - 1)) != 0)
                {
                    throw new IOException("Unexpected sector size: " + bps);
                }

                byte[] lenBuf = new byte[8];
                if (!Native.DeviceIoControl(h, IOCTL_DISK_GET_LENGTH_INFO, null, 0, lenBuf, lenBuf.Length, out ret, IntPtr.Zero))
                {
                    throw new IOException("Could not read the drive size.");
                }
                long totalBytes = BitConverter.ToInt64(lenBuf, 0);

                long hiddenBytes = 0;
                bool isMbr = false;
                byte[] pi = new byte[144];
                if (Native.DeviceIoControl(h, IOCTL_DISK_GET_PARTITION_INFO_EX, null, 0, pi, pi.Length, out ret, IntPtr.Zero))
                {
                    isMbr = (BitConverter.ToInt32(pi, 0) == 0);
                    hiddenBytes = BitConverter.ToInt64(pi, 8);
                }

                if (log != null) { log("Locking drive ..."); }
                bool locked = false;
                for (int i = 0; i < 10 && !locked; i++)
                {
                    locked = Native.DeviceIoControl(h, FSCTL_LOCK_VOLUME, null, 0, null, 0, out ret, IntPtr.Zero);
                    if (!locked) { Thread.Sleep(500); }
                }
                if (!locked)
                {
                    throw new IOException("The drive is still in use by another program (e.g. an open Explorer window). Please close everything that accesses the SD card and try again.");
                }
                Native.DeviceIoControl(h, FSCTL_DISMOUNT_VOLUME, null, 0, null, 0, out ret, IntPtr.Zero);
                Native.DeviceIoControl(h, FSCTL_ALLOW_EXTENDED_DASD_IO, null, 0, null, 0, out ret, IntPtr.Zero);

                long totalSectors = totalBytes / bps;
                if (totalSectors < 66600 || totalSectors > 0xFFFFFFFFL)
                {
                    throw new IOException("This disk size is not supported for FAT32.");
                }
                long hidden = hiddenBytes / bps;

                long clusterBytes;
                if (totalBytes <= 260L * 1024 * 1024) { clusterBytes = 512; }
                else if (totalBytes <= 8L * 1024 * 1024 * 1024) { clusterBytes = 4096; }
                else if (totalBytes <= 16L * 1024 * 1024 * 1024) { clusterBytes = 8192; }
                else if (totalBytes <= 32L * 1024 * 1024 * 1024) { clusterBytes = 16384; }
                else { clusterBytes = 32768; }
                long spc = clusterBytes / bps;
                if (spc < 1) { spc = 1; }

                long reserved = 32;
                long nfats = 2;
                long tmp1 = totalSectors - reserved;
                long tmp2 = (((long)bps / 4) * spc + nfats) / 2;
                long fatSize = (tmp1 + tmp2 - 1) / tmp2;
                long dataSectors = totalSectors - reserved - nfats * fatSize;
                long clusters = dataSectors / spc;
                if (clusters < 65525 || clusters > 0x0FFFFFF0L)
                {
                    throw new IOException("Cluster count (" + clusters + ") is invalid for FAT32.");
                }

                uint volumeId = (uint)(DateTime.Now.Ticks & 0xFFFFFFFFL);

                byte[] labelBytes = new byte[11];
                for (int i = 0; i < 11; i++) { labelBytes[i] = (byte)' '; }
                byte[] lab = Encoding.ASCII.GetBytes(label);
                Array.Copy(lab, labelBytes, Math.Min(11, lab.Length));

                byte[] boot = new byte[bps];
                boot[0] = 0xEB; boot[1] = 0x58; boot[2] = 0x90;
                byte[] oem = Encoding.ASCII.GetBytes("MSWIN4.1");
                Array.Copy(oem, 0, boot, 3, 8);
                PutU16(boot, 11, bps);
                boot[13] = (byte)spc;
                PutU16(boot, 14, (int)reserved);
                boot[16] = (byte)nfats;
                PutU16(boot, 17, 0);
                PutU16(boot, 19, 0);
                boot[21] = 0xF8;
                PutU16(boot, 22, 0);
                PutU16(boot, 24, 63);
                PutU16(boot, 26, 255);
                PutU32(boot, 28, (uint)hidden);
                PutU32(boot, 32, (uint)totalSectors);
                PutU32(boot, 36, (uint)fatSize);
                PutU16(boot, 40, 0);
                PutU16(boot, 42, 0);
                PutU32(boot, 44, 2);
                PutU16(boot, 48, 1);
                PutU16(boot, 50, 6);
                boot[64] = 0x80;
                boot[66] = 0x29;
                PutU32(boot, 67, volumeId);
                Array.Copy(labelBytes, 0, boot, 71, 11);
                byte[] fsType = Encoding.ASCII.GetBytes("FAT32   ");
                Array.Copy(fsType, 0, boot, 82, 8);
                boot[510] = 0x55;
                boot[511] = 0xAA;

                byte[] fsi = new byte[bps];
                PutU32(fsi, 0, 0x41615252);
                PutU32(fsi, 484, 0x61417272);
                PutU32(fsi, 488, (uint)(clusters - 1));
                PutU32(fsi, 492, 3);
                PutU32(fsi, 508, 0xAA550000);

                byte[] sig = new byte[bps];
                sig[510] = 0x55;
                sig[511] = 0xAA;

                fs = new FileStream(h, FileAccess.ReadWrite, 4096);

                if (log != null) { log("Writing boot sectors ..."); }
                WriteZeros(fs, 0, reserved * bps);
                WriteAt(fs, 0, boot);
                WriteAt(fs, 1L * bps, fsi);
                WriteAt(fs, 2L * bps, sig);
                WriteAt(fs, 6L * bps, boot);
                WriteAt(fs, 7L * bps, fsi);
                WriteAt(fs, 8L * bps, sig);

                long fatStart = reserved * bps;
                long fatBytes = fatSize * bps;
                for (int f = 0; f < nfats; f++)
                {
                    if (log != null) { log("Writing file allocation table " + (f + 1) + " of " + nfats + " ..."); }
                    long off = fatStart + f * fatBytes;
                    WriteZeros(fs, off, fatBytes);
                    byte[] first = new byte[bps];
                    PutU32(first, 0, 0x0FFFFFF8);
                    PutU32(first, 4, 0x0FFFFFFF);
                    PutU32(first, 8, 0x0FFFFFFF);
                    WriteAt(fs, off, first);
                }

                if (log != null) { log("Writing root directory ..."); }
                long dataStart = fatStart + nfats * fatBytes;
                WriteZeros(fs, dataStart, spc * bps);
                byte[] dirent = new byte[bps];
                Array.Copy(labelBytes, 0, dirent, 0, 11);
                dirent[11] = 0x08;
                WriteAt(fs, dataStart, dirent);
                fs.Flush();

                if (isMbr)
                {
                    try
                    {
                        byte[] st = new byte[20];
                        st[0] = 0;
                        st[4] = 0x0C;
                        Native.DeviceIoControl(h, IOCTL_DISK_SET_PARTITION_INFO_EX, st, st.Length, null, 0, out ret, IntPtr.Zero);
                    }
                    catch (Exception) { }
                }

                Native.DeviceIoControl(h, FSCTL_DISMOUNT_VOLUME, null, 0, null, 0, out ret, IntPtr.Zero);
                if (log != null) { log("Finished writing."); }
            }
            finally
            {
                if (fs != null) { fs.Dispose(); }
                else { h.Dispose(); }
            }
        }
    }

    // ------------------------------------------------------------
    //  Capacity test (detects fake SD cards)
    // ------------------------------------------------------------
    internal class CapResult
    {
        public bool Cancelled;
        public string Error;
        public long BlocksWritten;
        public long BlocksGood;
        public long FirstBad = -1;
        public long FreeAtStart;
        public double WriteMBps;
        public double ReadMBps;
    }

    internal static class CapTest
    {
        public const int Block = 4 * 1024 * 1024;
        private const long FileBlocks = 256;

        private static void Fill(byte[] buf, ulong[] tmp, long g)
        {
            ulong s = ((ulong)(g + 1)) * 0x9E3779B97F4A7C15UL;
            if (s == 0UL) { s = 1UL; }
            for (int i = 0; i < tmp.Length; i++)
            {
                s ^= s << 13;
                s ^= s >> 7;
                s ^= s << 17;
                tmp[i] = s;
            }
            Buffer.BlockCopy(tmp, 0, buf, 0, Block);
        }

        public static CapResult Run(string driveRoot, Action<string, int> progress, Func<bool> cancel)
        {
            CapResult res = new CapResult();
            string dir = Path.Combine(driveRoot, "SDTEST_TMP");
            try
            {
                DriveInfo di = new DriveInfo(driveRoot);
                long free = di.AvailableFreeSpace;
                res.FreeAtStart = free;
                long reserve = 16L * 1024 * 1024;
                long maxBlocks = (free - reserve) / Block;
                if (maxBlocks < 4) { throw new IOException("Not enough free space for the test."); }

                Directory.CreateDirectory(dir);
                byte[] buf = new byte[Block];
                ulong[] tmp = new ulong[Block / 8];

                // ---- Write ----
                Stopwatch sw = Stopwatch.StartNew();
                long g = 0;
                int fileIdx = 0;
                bool full = false;
                while (g < maxBlocks && !full)
                {
                    string fp = Path.Combine(dir, "t" + fileIdx.ToString("D5") + ".bin");
                    FileStream fs = new FileStream(fp, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.WriteThrough);
                    try
                    {
                        for (long b = 0; b < FileBlocks && g < maxBlocks; b++)
                        {
                            if (cancel()) { res.Cancelled = true; return res; }
                            Fill(buf, tmp, g);
                            try
                            {
                                fs.Write(buf, 0, Block);
                            }
                            catch (IOException)
                            {
                                full = true;
                                break;
                            }
                            g++;
                            if ((g % 4) == 0)
                            {
                                double sec = Math.Max(0.001, sw.Elapsed.TotalSeconds);
                                progress("Writing test data: " + Util.Fmt(g * Block) + " of " + Util.Fmt(maxBlocks * Block)
                                    + "  (" + ((g * Block) / 1048576.0 / sec).ToString("N1") + " MB/s)",
                                    (int)(g * 50 / maxBlocks));
                            }
                        }
                        try { fs.Flush(true); } catch (IOException) { full = true; }
                    }
                    finally
                    {
                        try { fs.Dispose(); } catch (IOException) { full = true; }
                    }
                    fileIdx++;
                }
                sw.Stop();
                res.BlocksWritten = g;
                res.WriteMBps = (g * (double)Block) / 1048576.0 / Math.Max(0.001, sw.Elapsed.TotalSeconds);

                // ---- Read and verify (unbuffered, so nothing is served from the Windows cache) ----
                IntPtr raw = Marshal.AllocHGlobal(Block + 4096);
                try
                {
                    long aligned = (raw.ToInt64() + 4095L) & ~4095L;
                    IntPtr abuf = new IntPtr(aligned);
                    Stopwatch sr = Stopwatch.StartNew();
                    long idx = 0;
                    for (int fi = 0; fi < fileIdx && idx < g; fi++)
                    {
                        string fp = Path.Combine(dir, "t" + fi.ToString("D5") + ".bin");
                        SafeFileHandle fh = Native.CreateFile(fp, Native.GENERIC_READ, Native.FILE_SHARE_READ, IntPtr.Zero,
                            Native.OPEN_EXISTING, Native.FILE_FLAG_NO_BUFFERING | Native.FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero);
                        if (fh.IsInvalid)
                        {
                            throw new IOException("Could not open the test file for reading (error " + Marshal.GetLastWin32Error() + ").");
                        }
                        try
                        {
                            for (long b = 0; b < FileBlocks && idx < g; b++)
                            {
                                if (cancel()) { res.Cancelled = true; return res; }
                                uint got;
                                bool ok = Native.ReadFile(fh, abuf, (uint)Block, out got, IntPtr.Zero);
                                bool good = false;
                                if (ok && got == (uint)Block)
                                {
                                    Fill(buf, tmp, idx);
                                    good = (Native.memcmp(abuf, buf, new UIntPtr((uint)Block)) == 0);
                                }
                                if (good) { res.BlocksGood++; }
                                else if (res.FirstBad < 0) { res.FirstBad = idx; }
                                idx++;
                                if ((idx % 4) == 0)
                                {
                                    double sec = Math.Max(0.001, sr.Elapsed.TotalSeconds);
                                    progress("Verifying test data: " + Util.Fmt(idx * Block) + " of " + Util.Fmt(g * Block)
                                        + "  (" + ((idx * Block) / 1048576.0 / sec).ToString("N1") + " MB/s)",
                                        50 + (int)(idx * 50 / Math.Max(1, g)));
                                }
                            }
                        }
                        finally
                        {
                            fh.Dispose();
                        }
                    }
                    sr.Stop();
                    res.ReadMBps = (idx * (double)Block) / 1048576.0 / Math.Max(0.001, sr.Elapsed.TotalSeconds);
                }
                finally
                {
                    Marshal.FreeHGlobal(raw);
                }
            }
            catch (Exception ex)
            {
                res.Error = ex.Message;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(dir)) { Util.ForceDelete(dir, true); }
                }
                catch (Exception) { }
            }
            return res;
        }
    }
}
