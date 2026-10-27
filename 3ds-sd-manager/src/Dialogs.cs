// ============================================================
//  3DS SD Manager - Dialogs.cs
//  Progress window and confirmation dialog for formatting
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SdManager3ds
{
    internal class ProgressForm : Form
    {
        private Label lbl;
        private ProgressBar bar;
        private Button btn;
        public bool Cancelled;

        public ProgressForm(string title)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ControlBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(560, 130);
            this.Font = new Font("Segoe UI", 9f);

            lbl = new Label();
            lbl.AutoSize = false;
            lbl.SetBounds(14, 14, 530, 40);
            lbl.Text = "Starting ...";

            bar = new ProgressBar();
            bar.SetBounds(14, 58, 530, 22);
            bar.Minimum = 0;
            bar.Maximum = 100;

            btn = new Button();
            btn.Text = "Cancel";
            btn.SetBounds(444, 92, 100, 28);
            btn.Click += delegate
            {
                Cancelled = true;
                btn.Enabled = false;
                lbl.Text = "Cancelling and cleaning up ...";
            };

            this.Controls.Add(lbl);
            this.Controls.Add(bar);
            this.Controls.Add(btn);
        }

        public void Report(string text, int percent)
        {
            if (!Cancelled) { lbl.Text = text; }
            if (percent < 0) { percent = 0; }
            if (percent > 100) { percent = 100; }
            bar.Value = percent;
            Application.DoEvents();
        }
    }

    internal class FormatDialog : Form
    {
        private TextBox txtLabel;
        private TextBox txtLetter;
        private Button btnOk;
        private Button btnCancel;
        private char letter;

        public string VolumeLabel
        {
            get { return txtLabel.Text; }
        }

        public FormatDialog(string driveText, char driveLetter, string extraWarning)
        {
            letter = char.ToUpperInvariant(driveLetter);
            this.Text = "Format SD card as FAT32";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(560, 270);
            this.Font = new Font("Segoe UI", 9f);

            Label warn = new Label();
            warn.AutoSize = false;
            warn.SetBounds(14, 12, 532, 100);
            warn.ForeColor = Color.Firebrick;
            warn.Text = "WARNING: Formatting permanently erases ALL data on this drive!\r\n\r\n"
                + "Drive: " + driveText + "\r\n"
                + (string.IsNullOrEmpty(extraWarning) ? "" : "\r\n" + extraWarning);

            Label l1 = new Label();
            l1.AutoSize = false;
            l1.Text = "Card label (max. 11 characters):";
            l1.SetBounds(14, 126, 250, 20);
            txtLabel = new TextBox();
            txtLabel.Text = "3DS";
            txtLabel.MaxLength = 11;
            txtLabel.SetBounds(270, 122, 140, 24);

            Label l2 = new Label();
            l2.AutoSize = false;
            l2.Text = "To confirm, type the drive letter (" + letter + "):";
            l2.SetBounds(14, 164, 380, 20);
            txtLetter = new TextBox();
            txtLetter.MaxLength = 2;
            txtLetter.SetBounds(400, 160, 50, 24);

            btnOk = new Button();
            btnOk.Text = "Format";
            btnOk.SetBounds(330, 224, 110, 30);
            btnOk.Enabled = false;
            btnOk.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(448, 224, 100, 30);
            btnCancel.DialogResult = DialogResult.Cancel;

            txtLetter.TextChanged += delegate
            {
                string t = txtLetter.Text.Trim().TrimEnd(':').ToUpperInvariant();
                btnOk.Enabled = (t == letter.ToString());
            };

            this.Controls.Add(warn);
            this.Controls.Add(l1);
            this.Controls.Add(txtLabel);
            this.Controls.Add(l2);
            this.Controls.Add(txtLetter);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
