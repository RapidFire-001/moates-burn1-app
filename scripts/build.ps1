# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Rapidfire
[CmdletBinding()]
param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    if (!(Test-Path -LiteralPath $compiler)) { throw 'Install .NET Framework 4.x and its C# compiler on Windows.' }
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    $sources = @('src\BurnModern.cs','src\RomStacker.cs','src\FordF3.cs','src\AssemblyInfo.cs')
    $shared = @('/nologo','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/resource:docs\Test-record.txt,BurnModern.TestRecord','/resource:assets\BurnModern-chip.ico,BurnModern.ChipIcon')
    & $compiler @shared /target:winexe /out:artifacts\BurnModern-v21.exe /win32icon:assets\BurnModern-chip.ico @sources
    if ($LASTEXITCODE -ne 0) { throw 'App compilation failed.' }
    if ($Test) {
        New-Item -ItemType Directory -Path artifacts\tests -Force | Out-Null
        foreach ($name in @('FinalWorkflow','FinalVerify','TestBlank','TestStacker','TestPreferences','TestDisconnect','CheckButtons')) {
            & $compiler @shared /target:exe "/main:$name" "/out:artifacts\tests\$name.exe" @sources "tests\$name.cs"
            if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $name" }
            & ".\artifacts\tests\$name.exe"
            if ($LASTEXITCODE -ne 0) { throw "Test failed: $name" }
        }
    }
    Copy-Item -LiteralPath LICENSE,NOTICE -Destination artifacts
    Write-Output 'Build completed. No application was launched and no hardware operations were requested.'
} finally { Pop-Location }
