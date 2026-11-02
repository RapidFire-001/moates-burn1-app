# Provenance, license scope and publication decisions

Reviewed 2 November 2026. Creator/copyright credit: **Rapidfire**. Intended GitHub owner: **Rapidfire-001**. Proposed repository: **moates-burn1-app**.

## License choice and scope

The user chose MIT for contributions they have rights to license and accepted commercial/closed-source reuse with notices retained. LICENSE credits Copyright (c) 2026 Rapidfire. NOTICE limits the grant to rights Rapidfire owns; it does not license Moates documents, Chris Handwerker's source, OneROM or any other third-party work. Attribution alone is not permission, and choosing MIT does not cure an unauthorized adaptation.

The app implementation files, assembly metadata, seven tests and build/asset/package scripts carry MIT/Rapidfire headers. The original workspace remains untouched. App source bodies match v21 after removing only the new two-line license header and normalizing the text encoding BOM. No executable behavior was intentionally changed for this licensing pass. The original geometric icon assets are included under the same scoped project grant.

## What the development history establishes

The available chat history was paged back to the first app creation and reference download steps. The first Windows UI was created before the driver downloads. Later serial work explicitly downloaded/read `burnio.c`, `promio.c`, `burn2.c` and `chipdata.c` from `ctmlr/Moates-BURN-Utility`, then downloaded/read its `Moates Hardware Protocols v19.xls`. Later debugging consulted both the C implementation and protocol spreadsheet again. The history therefore establishes exposure to the reference source; it does NOT establish a clean-room process.

The history view identifies edits but does not provide every original patch body. There is no original application Git history or archived sequence of all source revisions to independently reconstruct every intermediate edit. It would be inaccurate to certify that no adaptation ever occurred.

## Exact reference source and license findings

The actual download location was [ctmlr/Moates-BURN-Utility](https://github.com/ctmlr/Moates-BURN-Utility). The [official Moates BURN2 page](https://shop.moates.net/pages/burn2) links to [chandwer/Moates-BURN-Driver](https://github.com/chandwer/Moates-BURN-Driver). Both repositories currently resolve to the identical master revision:

`a8c066b3b23411eb0fe94f7363488f828569f773`

GitHub's repository metadata declares no license for either. Their license endpoints return 404. Recursive tree inspection shows README.md, the protocol spreadsheet, four C source files, three headers and Makefile, with no license file. The retrieved README/source/header/Makefile text contains no license or permission grant. The local four C reference files match the upstream files byte-for-byte. Their headers identify Chris Handwerker (2013), homebrewtechnology.org. No reference C code or spreadsheet is included in this publication repository. Research copies and comparison results remain outside it.

This is evidence of **no permission grant found**, not a determination that a permissive license exists or that public GitHub hosting grants redistribution rights.

## Source comparison and inference

Manual comparison covered the reference serial transport, read/write/erase routines and chip table against the corresponding C# routines. An auxiliary comparison of all three app sources against the upstream C/header files found zero shared normalized statements of at least 40 characters and zero shared string literals of at least 20 characters. These thresholds are a duplication check, not a copyright or derivation test; short copying or translated/rewritten adaptation can evade them.

Observed differences:

- The reference uses Linux file descriptors/termios, malloc buffers and blocking POSIX I/O. The app uses managed Windows SerialPort objects, timeout/version checks, explicit partial-read accumulation and disposal per operation.
- The reference read routine streams one pass into a file. The app validates each response checksum, reads twice into buffers, rejects differing passes and offers a separate backup save.
- The reference writer erases before writing. The app separates erase from program, prechecks flash blankness, preserves AT29C256 page surroundings and performs double-read verification afterward.
- The reference explicitly leaves AM29F040 erase unimplemented. The app implements bank erase plus full readback and a separate original-F3 workflow.
- The UI, ROM stacker, remembered profiles, file comparison, test-record dialog, operation interlocks and geometric icon have no counterpart in the reference C utility.

The common elements identified are hardware facts and protocol requirements: chip sizes, baud/framing, read/write/erase command bytes, address ordering, 256-byte block conventions, acknowledgment byte and additive checksum. No specific copied copyrightable implementation passage was identified. The evidence supports a separately expressed implementation using researched protocol facts; exposure and incomplete intermediate edit records prevent treating that inference as proof of independence.

## Exact remaining provenance decision / remediation

There is **no identified copied passage for which a specific permission request can currently be named**. Do not seek permission merely to use documented protocol facts, and do not claim that MIT or an acknowledgment licenses the reference driver.

Release assessment: no concrete copied/adapted expressive passage was identified after reviewing the history, actual reference files and app implementation. The shared protocol facts do not establish copying of expression. The absence of a perfect clean-room record is not treated as an automatic release blocker. No additional driver-code permission is currently identified as necessary for this snapshot. If a specific adaptation is subsequently identified:

1. Obtain a written grant from the relevant copyright holder covering the particular copied/adapted code, redistribution, modifications and MIT-compatible sublicensing, preserving any required notices; or
2. Replace the affected implementation using a separately documented specification of protocol facts, preferably with a reviewer/implementer who has not seen the reference expression, then rerun the software and appropriate hardware checks. Record the replacement and its provenance.

Do not relabel the reference C code as MIT, import it into this repository, or remove its attribution. This review cannot supply missing permission or certify a clean-room origin.

## Other material and dependencies

- [Moates BURN1](https://shop.moates.net/pages/burn1), [programming guide](https://shop.moates.net/pages/beginners-programming-chips) and [firmware support](https://shop.moates.net/pages/firmware-updates-support) provide hardware support/firmware context. The spreadsheet is excluded, not redistributed under MIT.
- OneROM inspired the charcoal/lime theme; no OneROM source, firmware, graphics or branding is included. Historical OneROM emulation tests remain distinct from physical-chip tests. [OneROM programmer precautions](https://github.com/piersfinlayson/one-rom/blob/main/docs/ROMS-GLORIOUS-ROMS.md#reading-one-rom-in-an-eprom-programmer) still apply.
- The icon is generated by the included geometric drawing script. Optional Pillow is not bundled or required to compile the checked-in assets.
- The app links only Windows/.NET Framework system assemblies; those runtimes and fonts are not redistributed.

## Exclusions and remaining release steps

All ROMs/BINs/chip dumps, MFW firmware, vendor installers/updaters, settings, protocol spreadsheets, C reference sources, Python research packages, private/session logs and old builds are excluded. Originals remain in the original workspace. Generated app/test executables remain in ignored artifacts and are not staged.

The proposed public name had no visible repository in the unauthenticated GitHub API check (404); private conflicts and create permission remain unverified. At the initial local review, no remote, commit, push or publication had been made. Noninteractive credential lookup was rejected by automatic approval policy; no credential was displayed or collected. Authentication must be confirmed through an allowed signed-in GitHub workflow before release.

The user has authorized public publication under Rapidfire-001/moates-burn1-app. The local rights review identifies no concrete copied expression blocking this snapshot. MIT/Rapidfire credit and exclusions are applied. Publication workflow: authenticated browser upload, verification of public source correspondence, and binary-release publication. No credentials are required in chat.
