# Steam Coding Time Tracker (Kode Studio Sync)

[Versão em Português](./README.md)

> **Language / Stack:** `C# (.NET Framework 4.8 WinForms + System Tray)` | **Author:** `piercer`

## Overview

Automatically detects active code editors and IDEs on Windows and syncs your coding time with Steam (defaulting to the free **Kode Studio — AppID `779260`**, or any custom AppID).

- **Automatic IDE detection:** VS Code (`Code`), Cursor, Windsurf, Antigravity, Visual Studio (`devenv`), JetBrains (`pycharm64`, `idea64`, `rider64`, `webstorm64`, `goland64`, `clion64`), Sublime Text, Notepad++, Neovim, Android Studio, and Zed.
- **Steam Auto-Launch:** triggers `steam://rungameid/<AppID>` as soon as a monitored IDE starts.
- **Smart Auto-Close:** shuts down the configured Steam process after all IDEs close (with a configurable grace period).
- **Local history & System Tray:** logs sessions to `coding_sessions.csv` and runs quietly in the Windows System Tray.

## How to run / build

Run `piercer_steam_coding_tracker.exe` directly or rebuild with:

```bat
build.bat
```

---
Developed by **piercer**
