param(
    [string]$RevitVersion = "R26"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$year = @{R20=2020;R21=2021;R22=2022;R23=2023;R24=2024;R25=2025;R26=2026}[$RevitVersion]
$configName = "Release $RevitVersion"
$buildDir = "$root\plugin\bin\AddIn $year $configName"

if (-not (Test-Path $buildDir)) {
    throw "Build output not found: $buildDir`nRun build.ps1 -RevitVersion $RevitVersion first."
}

New-Item -ItemType Directory -Force -Path "$root\dist" | Out-Null
$zipFile = "$root\dist\mcp-servers-for-revit-Revit$year.zip"

Write-Host "=== Creating distribution ZIP ==="
if (Test-Path $zipFile) { Remove-Item $zipFile -Force }

# Stage: flatten addin to root + revit_mcp_plugin folder
$stage = "$root\dist\_stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

robocopy "$buildDir" $stage /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null

# Add install.ps1 at package root
@"
`$ErrorActionPreference = "Stop"
`$target = "`$env:APPDATA\Autodesk\Revit\Addins\$year"
`$source = Split-Path -Parent `$MyInvocation.MyCommand.Path

Write-Host "Installing to: `$target"

if (Test-Path "`$target\revit_mcp_plugin") { Remove-Item "`$target\revit_mcp_plugin" -Recurse -Force -ErrorAction Continue }
if (Test-Path "`$target\mcp-servers-for-revit.addin") { Remove-Item "`$target\mcp-servers-for-revit.addin" -Force -ErrorAction Continue }

New-Item -ItemType Directory -Force -Path "`$target" | Out-Null
Copy-Item "`$source\mcp-servers-for-revit.addin" "`$target\" -Force
Copy-Item "`$source\revit_mcp_plugin" "`$target\" -Recurse -Force

Write-Host "=== Install Complete ==="
Write-Host "Installed to: `$target"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Restart Revit (if running)"
Write-Host "2. Click 'Revit MCP Switch' in Add-Ins ribbon"
Write-Host "3. Configure AI client:"
Write-Host "   command: `$target\revit_mcp_plugin\runtime\node.exe"
Write-Host "   args:    `$target\revit_mcp_plugin\runtime\build\index.js"
"@ | Set-Content "$stage\install.ps1" -Encoding ASCII

Compress-Archive -Path "$stage\*" -DestinationPath $zipFile
Remove-Item $stage -Recurse -Force

Write-Host "Package: $zipFile"
Write-Host "Size: $((Get-Item $zipFile).Length / 1MB -as [int]) MB"
Write-Host ""
Write-Host "Distribution instructions:"
Write-Host "  1. Extract ZIP to any folder"
Write-Host "  2. Run install.ps1 (right-click -> Run with PowerShell)"
Write-Host "  3. Restart Revit"
Write-Host "  4. Click 'Revit MCP Switch' in Add-Ins tab"
