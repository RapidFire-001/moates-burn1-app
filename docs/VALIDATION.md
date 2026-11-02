# Review validation

Prepared and checked 2 November 2026. No hardware operation was performed.

## Source and build

The three application implementation bodies match the workspace v21 originals after removing only the new two-line MIT/Rapidfire headers. A separate AssemblyInfo.cs adds product/version metadata and Rapidfire company/copyright credit. Original workspace sources were not changed. The original recorded executable SHA-256 is `F73372BC870DC01F08D70456926000A66C9EA2DB5EA188C5B54B082C38E2F9E5`.

A fresh build from this review repository completed with the Windows .NET Framework C# compiler. The final binary and ZIP hashes are recorded in the release SHA256SUMS.txt, rather than pinning a previous compiler invocation here. The ZIP is made from an explicit ten-file allowlist; repeated packaging of the same binary was verified byte-identical. This does not make the legacy compiler output itself byte-deterministic. The embedded historical record has its local COM-port detail omitted. License headers, Rapidfire assembly metadata, compiler metadata and this resource change mean the new executable is not expected to match the original binary. LICENSE and NOTICE are copied beside the built app. No pre-existing executable is proposed for Git inclusion.

## Software checks

`scripts/build.ps1 -Test` passed all seven programs:

- FinalWorkflow: all twelve profile permissions, flash blank requirement, offset placement, AT29C256 page alignment/preservation and final-address packets.
- FinalVerify: comparison range, mismatch count/first address/values, final 512 KiB byte, invalid placement and verify controls for all profiles.
- TestBlank: full FF and first/last non-FF detection/counts at 4/16/32/64/128/256/512 KiB, plus disconnected/busy guards and minimum-size layout.
- TestStacker: ordered/repeated input, FF bank padding, unchanged source buffers, full 512 KiB output and oversized/invalid input rejection.
- TestPreferences: all twelve stable keys, missing/invalid settings, restart restoration and manual selection.
- TestDisconnect: offline/connected/busy controls, idle disconnect, retained profile and button bounds.
- CheckButtons: all main buttons fit at 1040x700 and 1120x790; 288 profile/state interlock assertions, disconnect guard, added read checksums and all original-F3 full-slot write addresses.

These are software assertions, not a new live test of each GUI interaction or physical chip. The historical record is retained with its limitations; new profiles remain untested on hardware.

## Publication-content audit

The source workspace was not a Git repository. The review directory has a newly initialized local `main` branch, no commits and no configured remote; there is no inherited Git history to publish. Only the explicit source/assets/tests/scripts/docs allowlist is staged. Generated artifacts are ignored.

Pattern checks of the selected text files found no recognizable GitHub token, AWS key ID, private-key block, credential-bearing URL, password/secret/token label or local user-home path. Manual review covered source imports, external references, selected tests and documentation. Pattern checks cannot guarantee the absence of every possible secret.

Vendor installers, firmware/updater, chip BINs/backups, machine settings, detailed logs, protocol spreadsheet, research C sources and Python packages are excluded. No executable, firmware, archive, chip dump or machine settings file is staged. Checksums of all selected publication files except the manifest itself are listed in MANIFEST.sha256. Raw file bytes are preserved by .gitattributes.

## Readiness

MIT is applied to contributions Rapidfire has rights to license; copyright/creator credit and binary metadata use Rapidfire. Rapidfire-001 is only the GitHub account owner. The local review directory is moates-burn1-app. The actual reference-source download and both upstream repositories were verified at the identical revision documented in PROVENANCE.md; no license grant was found. History/manual/source comparison found no identified copied implementation passage but does not prove absence of adaptation. No concrete copied-expression rights blocker was identified; lack of a perfect clean-room record alone is not a release blocker. Public publication is user-authorized; authenticated upload remains necessary. The proposed GitHub repository name returned unauthenticated HTTP 404; private conflicts are unverified. Git is available and a credential helper is configured; GitHub CLI was not found and neither GH_TOKEN nor GITHUB_TOKEN was present. Automatic approval policy rejected noninteractive credential lookup. No credential values were printed or collected. Authenticated GitHub account/create permissions remain unverified. No push, remote creation or publication was attempted during these local checks.
