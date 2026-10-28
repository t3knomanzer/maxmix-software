# Maxmix 1.5.9 - local build

Based on `t3knomanzer/maxmix-software`, master commit `b3d1937`.
This branch is a modified local build, not an upstream release.

## Changes

- One serial worker owns reads, writes and connection state. UI requests are queued.
- Fixed volume commands that incorrectly contained a 32-byte session instead of a 2-byte volume payload.
- Retry initial firmware detection during Nano boot; recover from incomplete frames and device errors.
- Pause USB access while the firmware tool is open, including across power-resume events.
- Transactional firmware reads: incomplete packets leave the previous state intact and return an error.
- Italian dark UI with connection details, reconnect and firmware upload commands.
- Functional double-click interval setting, negotiated with firmware 1.5.8 and newer.
- User-selectable RGB colors for input, applications, mute and disconnected effects, supported by firmware 1.5.9.
- Output volume, reverse input, symmetric application and independent game/chat half-ring patterns.
- Nonblocking LED frame generation at the existing brightness and update-rate limits.
- Offline installer containing the app, CH340/CH341 driver, Nano full/half-step firmware, avrdude, update tool and sources.

## End-user installation

Run `Maxmix-1.5.9-Setup.exe` on Windows 10 version 2004+ or Windows 11, Intel/AMD.
The installer requires the system .NET Framework 4.8 runtime. It does not download development tools.
The driver step requests administrator permission; application installation is per user.

Firmware upload supports Arduino Nano ATmega328P with an existing Arduino bootloader.
Use **Carica firmware**, select the COM port and encoder/bootloader settings.
A new Nano does not need to have Maxmix already installed. A chip with no bootloader requires an ISP programmer first.
Firmware is not flashed automatically during application installation.

Flash and EEPROM are backed up before writing. An unsuccessful backup stops the upload.
Avrdude checks the MCU signature and verifies the written flash; the tool then checks the running firmware version.
Backups and logs are stored under `%LOCALAPPDATA%\Maxmix` and are not part of the public source tree.

## Build

Prerequisites: Windows, Git, Python and Visual Studio Build Tools with desktop .NET/MSBuild support.

```powershell
.\tools\Build-Local.ps1 -Firmware -Tests
```

The script obtains .NET reference assemblies and PlatformIO in `.tools` as needed.
It builds the desktop solution, Nano firmware variants, desktop regression tests and native firmware/LED tests.
Hardware is not required for these automated tests.

For the installer, install the signed Inno Setup 6.7.4 compiler into `.tools\innosetup`, then run:

```powershell
.\tools\Build-Package.ps1
```

The result is `dist\Maxmix-1.5.9-Setup.exe`. The build verifies the driver catalog signature and hashes the bundled files.
The bundled source ZIP excludes build tools, device backups, logs and personal settings.

## Validation and limits

Local checks cover serial packet sizes, RGB byte order and settings persistence, lifecycle/USB ownership,
delayed boot, error recovery, firmware packet validation and deterministic LED frames.
Installer installation, file-integrity checks, deliberate corruption detection and uninstall were exercised in an isolated directory.
An actual Nano ATmega328P/CH340 was backed up, flashed and read-back verified using the bundled offline tool.
The LED patterns were also observed on the physical controller.

A fresh blank Nano, a second computer and prolonged suspend/resume operation have not been physically validated.
The installer is an unsigned local build. Legacy upstream CI/release workflows are retained; local checks do not imply CI success.
The `mod-buttons` and `remote-rendering` experimental branches were read for reference and are not merged here.
