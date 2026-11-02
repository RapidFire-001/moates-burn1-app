# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Rapidfire
[CmdletBinding()]
param([string]$OutputDirectory = 'artifacts\release-v21.0.0')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'build.ps1') -Test
    $release=[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
    $allowed=[IO.Path]::GetFullPath((Join-Path $root 'artifacts'))+[IO.Path]::DirectorySeparatorChar
    if(!$release.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Release output must remain under artifacts.'}
    if(Test-Path -LiteralPath $release){throw 'Release output already exists. Choose a new -OutputDirectory under artifacts.'}
    New-Item -ItemType Directory -Path $release | Out-Null
    $staging=Join-Path $release 'package'
    New-Item -ItemType Directory -Path (Join-Path $staging 'docs') -Force | Out-Null
    # Explicit allowlist: never recursively copy build output or the workspace.
    Copy-Item -LiteralPath artifacts\BurnModern-v21.exe -Destination (Join-Path $staging 'BurnModern-v21.exe')
    Copy-Item -LiteralPath LICENSE,NOTICE,README.md -Destination $staging
    foreach($name in @('PROVENANCE.md','Test-record.txt','VALIDATION.md','MANIFEST.sha256')){
        Copy-Item -LiteralPath (Join-Path 'docs' $name) -Destination (Join-Path $staging "docs\$name")
    }
    $sourceManifestHash=(Get-FileHash docs\MANIFEST.sha256 -Algorithm SHA256).Hash.ToLowerInvariant()
    @("BurnModern v21.0.0",'Creator/copyright: Rapidfire','Intended source: https://github.com/Rapidfire-001/moates-burn1-app',"Source-manifest SHA-256: $sourceManifestHash",'Built from the checked-in sources with scripts/build.ps1; tests passed without hardware operations.','MIT applies only to contributions Rapidfire has rights to license. See LICENSE, NOTICE and docs/PROVENANCE.md.') | Set-Content (Join-Path $staging 'SOURCE.txt')
    $files=@(Get-ChildItem -LiteralPath $staging -Recurse -File | Sort-Object FullName)
    $files | ForEach-Object {"$((Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant())  $($_.FullName.Substring($staging.Length+1).Replace('\','/'))"} | Set-Content (Join-Path $staging 'SHA256SUMS.txt')
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath=Join-Path $release 'moates-burn1-app-v21.0.0-windows.zip'
    $zip=[IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach($file in Get-ChildItem -LiteralPath $staging -Recurse -File | Sort-Object FullName){
            $entryName=$file.FullName.Substring($staging.Length+1).Replace('\','/')
            $entry=$zip.CreateEntry($entryName,[IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime=[DateTimeOffset]::new(2026,11,2,0,0,0,[TimeSpan]::Zero)
            $inputStream=[IO.File]::OpenRead($file.FullName);$outputStream=$entry.Open()
            try {$inputStream.CopyTo($outputStream)} finally {$inputStream.Dispose();$outputStream.Dispose()}
        }
    } finally {$zip.Dispose()}
    Copy-Item -LiteralPath (Join-Path $staging 'BurnModern-v21.exe') -Destination $release
    @('BurnModern-v21.exe','moates-burn1-app-v21.0.0-windows.zip') | ForEach-Object {"$((Get-FileHash -LiteralPath (Join-Path $release $_)).Hash.ToLowerInvariant())  $_"} | Set-Content (Join-Path $release 'SHA256SUMS.txt')
    Write-Output "Release prepared: $release"
} finally {Pop-Location}
