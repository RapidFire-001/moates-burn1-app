# BurnModern / BURN Device Studio

A Windows desktop interface for the Moates BURN1 chip programmer. Version 21 by Rapidfire, prepared for the proposed `Rapidfire-001/moates-burn1-app` GitHub repository. This project is independent of Moates; it is not the vendor Flash & Burn software or device firmware.

**License:** MIT for contributions Rapidfire has rights to license; see [LICENSE](LICENSE) and [NOTICE](NOTICE). The grant does not cover third-party rights. The source-comparison and development-history review found no identified copied implementation passage, but cannot certify absence of adaptation; see [provenance](docs/PROVENANCE.md). No concrete copied-expression rights blocker was identified by the review. The implementation and historical hardware-test limits are documented below.

> **Microsoft Edge download notice.** SmartScreen may warn that a file isn’t commonly downloaded when it has limited download history. If Edge or your antivirus shows a malware warning, please send the exact message and the file name or release version so I can investigate. If the GitHub page is unavailable in Edge, you can also view it in Firefox or Chrome. See [Microsoft’s SmartScreen guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Features

- Connect/disconnect a BURN programmer through its serial port and remember a manually selected profile.
- Read a chip twice with block checksums and save a backup.
- Verify chip contents against a BIN at a selected offset.
- Run a read-only blank test without a BIN.
- Erase/program/verify where the selected profile permits it, with operation interlocks.
- Build a stacked BIN by ordering/repeating inputs and optionally padding fixed banks with FF bytes.
- Display the fixed historical test record bundled into the app.

## Build and software checks

Requirements: Windows with .NET Framework 4.x, its framework C# compiler, and Windows PowerShell 5.1 or newer. No NuGet packages or downloaded runtime dependencies are used by the app.

From the repository root:

```powershell
powershell -NoProfile -File .\scripts\build.ps1
powershell -NoProfile -File .\scripts\build.ps1 -Test
```

The executable is written to `artifacts\BurnModern-v21.exe`. The second command also compiles and runs seven software test programs; it does not open a serial port or issue chip commands. Tests enumerate available port names and may briefly construct off-screen UI windows. Screenshots and temporary preferences remain under ignored `artifacts/`.

The script compiles the three app implementation files plus AssemblyInfo metadata, embeds the icon and test record, and copies LICENSE/NOTICE beside the executable. Binary metadata credits Rapidfire. These instructions reproduce the build procedure; byte-identical executables are not promised because the framework compiler emits build-specific metadata. The pre-existing bench binary is deliberately not checked into source control. To build, test and prepare an explicit-file-list release ZIP, run `powershell -NoProfile -File .\scripts\package.ps1`. Outputs go to ignored `artifacts/release-v21.0.0/`; use a new `-OutputDirectory` under artifacts for another packaging run. The ZIP contains the executable, license/notices, documentation and checksums, with no chip data or settings.

The optional icon generator uses Python and Pillow. It is not needed to build: the generated PNG/ICO are checked in. Run `python scripts/make_chip_icon.py` from an environment with Pillow to regenerate them. No Pillow code is bundled.

## Use

Launch the built executable, choose the port and Connect, then confirm the selected chip and its placement before operating. Connecting restores a remembered selection; it does not detect the inserted chip. Open a BIN for programming or file verification. Read + backup and Blank test do not require a file. Erase and programming change chip contents and ask for confirmation. A file comparison only compares the file's range; full programming verification checks the whole chip.

Settings are stored beside the executable in `BurnModern.settings`; keep the app in a writable folder. The fixed Test record does not grow with use. The activity log is session-only.

## Profiles and evidence

| Profile | Capacity | App operations / evidence |
| --- | ---: | --- |
| SST27SF512 | 64 KiB | Read, erase, program/verify implemented; physical testing pending |
| AT29C256 | 32 KiB | Read and page-preserving program/verify; OneROM read emulation passed, physical chip untested |
| AM29F040 | 512 KiB | Read, erase, program/verify implemented; genuine part untested |
| W27C512 | 64 KiB | Experimental read and erase; earlier erase failed; program disabled; no OneROM test reported |
| SST39SF040 profile | 512 KiB | Experimental AM29F040 commands; passes on one physical chip of unconfirmed identity |
| 27C128 / 27C256 / 27C512 | 16 / 32 / 64 KiB | Read, file verify, blank test only; OneROM emulation reads passed; physical chips untested |
| 2732A / 27C32 | 4 KiB | Documented read protocol; physical testing pending |
| AM29F010 | 128 KiB | Experimental read, file verify and blank test; erase/program disabled |
| Original Ford F3 + F2A | 256 KiB selected slot | Original module only; full-slot workflow; hardware untested; not F3v2/v3 |
| SST39SF010A | 128 KiB | Experimental read, file verify and blank test; erase/program disabled |

Read-only profiles also permit file verification and blank testing. Permission to enable a button does not establish hardware compatibility. See [the historical record](docs/Test-record.txt) and [review checks](docs/VALIDATION.md). The installed bench firmware was 5.14.F. This release does not update firmware.

## Limits

Chip ID detection and verified socket-power control are not implemented. Disconnect clears the app session; it does not turn off USB or guarantee the socket is unpowered. Unplug before handling chips. A matching pair of all-FF reads alone cannot establish chip contact. Windowed UV EPROMs require external UV erasure. OneROM read emulation is separate from physical-chip validation; OneROM's documentation warns about reading it in EPROM programmers, so the historical emulation tests are not a recommendation to insert one into a programmer.

The SST39SF040-profile test chip has not been electronically identified. AM29F040 erase commands cannot be assumed correct for genuine SST parts with a different sector layout. The historical W27C512 erase failed. These experimental profiles retain their existing warnings and have not been promoted to validated general support.

This repository contains no vendor firmware, installers, drivers, firmware updater, chip dumps, customer BINs, credentials or third-party research tool sources. Ostrich2 is a separate project and is not included.

## Attribution and license

Moates is the hardware/protocol documentation source. Chris Handwerker's BURN driver was consulted during development; no license grant was found, and the history/source review identified no copied implementation passage. OneROM influenced the charcoal/lime theme, but no OneROM source or assets are included. The memory-chip icon is generated by the included drawing script. Evidence and conditional remediation are preserved in [PROVENANCE.md](docs/PROVENANCE.md).

Copyright (c) 2026 Rapidfire. MIT permits commercial and closed-source reuse subject to retaining its copyright and permission notices. Rapidfire-001 is only the intended GitHub account owner. Third-party attribution remains preserved separately, and MIT does not supply permission for third-party code.

## Support

For help with this software, email [Rapid Fire Software Support](mailto:support@rapidfiresoftware.net). Include the software version, what you were trying to do, and any error message you saw.
