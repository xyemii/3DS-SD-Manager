# 3DS SD Manager

<img src="assets/icon.png" width="96" align="right" alt="App icon: a microSD card with a white h on a blue label">

A small Windows tool for the SD card of a (modded) Nintendo 3DS. It finds your homebrew, lets you delete what you
select, removes macOS/Windows junk files, keeps the core custom-firmware modules up to date, can format the card
as FAT32, checks whether the card really has the capacity it claims, and can lay out the files needed to start
modding a fresh card.

> **Status: early version.** Deleting, formatting and the capacity test work directly on the card. Back up
> anything important first (especially `/gm9/backups` and save-game backups such as `/3ds/Checkpoint/saves`),
> and try the destructive features on a spare card before using them on your main one. See [Disclaimer](#disclaimer).

## Features

| Tab | What it does |
| --- | --- |
| **1) Starter pack** | Choose *Old* or *New 3DS*. Downloads the latest **Luma3DS** and **boot9strap** from their official GitHub releases onto the SD card and, optionally, the *finalize* files for the second phase of the [3ds.hacks.guide](https://3ds.hacks.guide). Can clean up unnecessary files first (after confirmation). |
| **2) Modules and updates** | Compares the modules found on the card (Luma3DS, GodMode9, Universal-Updater, Checkpoint, FBI, ftpd, Anemone3DS) with the latest GitHub releases and updates the ones you tick. |
| **Homebrew** | Lists homebrew found on the card (`.3dsx` apps with name/author read from the file, CIA installers, Luma plugins and payloads, themes, cheats, ...). Tick entries and delete them. Core CFW and backup folders are marked red and need a second confirmation. |
| **Junk (macOS/Windows)** | Finds `._*` AppleDouble files, `.DS_Store`, `.Spotlight-V100`, `.Trashes`, `.fseventsd`, `.TemporaryItems`, `__MACOSX`, `Thumbs.db`, `desktop.ini` and leftover ZIP/7z/RAR files in the root. All are preselected. |
| **SD tools** | **Format as FAT32** (Windows' own formatter up to 32 GB, a built-in FAT32 formatter above that) and a **capacity test** in the style of H2testw that detects fake cards. |

What it deliberately does **not** do: run the exploit for you (that happens on the console - see the guide), list or
delete anything in `/Nintendo 3DS` (your installed titles and save data) or `/DCIM` (the junk scan is the only thing
that looks inside, and it only removes the junk files named above), or ship any third-party binaries. Everything is
downloaded at runtime from the upstream projects' official releases.

## Requirements

* Windows 10 or 11 (the .NET Framework 4.x that ships with Windows is enough)
* Internet access for the starter pack and module updates
* Administrator rights for formatting (the app can restart itself elevated)

## Build

No SDK needed - the build script uses the C# compiler that ships with Windows:

```bat
build.bat
```

This produces `bin\SD-Manager-3DS.exe`. The repository also contains a GitHub Actions workflow that builds the
executable on a Windows runner and uploads it as a build artifact.

## Usage

1. Insert the SD card and start `SD-Manager-3DS.exe`. The card is detected automatically (or pick a drive/folder at the top).
2. For a fresh card: *SD tools* -> format as FAT32 if needed, then *Starter pack* -> choose your model -> *Install pack to SD*.
   Then follow the guide for your model (the *Exploit page for my model* button opens it) and come back for *Phase 2*.
3. To tidy up: scan, then use the *Homebrew* and *Junk* tabs to delete what you no longer want.
4. To update: *Modules and updates* -> *Check for updates* -> *Update selected*.

Notes:

* The app only knows module versions it installed itself (it stores them in a hidden `.sdmanager3ds.txt` on the card);
  everything else shows as *unknown*. Apps installed as CIA in the HOME Menu are best updated on the console with Universal-Updater.
* The capacity test only tests the **free** space. Empty the card first for a full check. On slow cards it can take
  roughly an hour per 32 GB.
* Exploit-specific files (for example the super-skaterhax ZIP for your region and system version) depend on your
  console and are not downloaded by the app - follow the guide.

## Disclaimer

This is an unofficial community tool and is not affiliated with or endorsed by Nintendo or the authors of
Luma3DS, boot9strap, GodMode9 or the 3DS Hacks Guide. It does not contain or distribute games, keys or other
copyrighted material. Modifying a console and deleting or formatting storage carries risk, and you are responsible
for what you do with your devices. The software is provided as is, without warranty (see [LICENSE](LICENSE)).

## Credits

* [3DS Hacks Guide](https://3ds.hacks.guide) - the setup instructions this pack follows
* [Luma3DS](https://github.com/LumaTeam/Luma3DS), [boot9strap](https://github.com/SciresM/boot9strap),
  [GodMode9](https://github.com/d0k3/GodMode9), [Universal-Updater](https://github.com/Universal-Team/Universal-Updater),
  [Checkpoint](https://github.com/FlagBrew/Checkpoint), [FBI](https://github.com/nh-server/FBI-NH),
  [ftpd](https://github.com/mtheall/ftpd), [Anemone3DS](https://github.com/astronautlevel2/Anemone3DS)
* The app icon is original artwork (a microSD card with an "h"); it is not the official Homebrew logo.
  The letter was drawn with the [Poppins](https://fonts.google.com/specimen/Poppins) font (SIL Open Font License).

## License

[MIT](LICENSE)
