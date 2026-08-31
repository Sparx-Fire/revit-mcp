param(
    [ValidatePattern('^R(20|21|22|23|24|25|26)$')]
    [string]$RevitVersion = "R26"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$year = @{R20=2020;R21=2021;R22=2022;R23=2023;R24=2024;R25=2025;R26=2026}[$RevitVersion]

# ──────────────────────────────────────────────────
# Step 1: Build commandset (C# DLL)
# ──────────────────────────────────────────────────
Write-Host "=== [1/5] Building Command Set ($RevitVersion) ==="
dotnet build -c "Release $RevitVersion" "$root\commandset\RevitMCPCommandSet.csproj" --nologo
if ($LASTEXITCODE -ne 0) { throw "Command set build failed" }

# ──────────────────────────────────────────────────
# Step 2: Build MCP server (TypeScript)
# ──────────────────────────────────────────────────
Write-Host "=== [2/5] Building MCP Server ==="
Push-Location "$root\server"
try {
    npm install --silent 2>$null
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    npm run build --silent 2>$null
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}

# ──────────────────────────────────────────────────
# Step 3: Stage files for installer
# ──────────────────────────────────────────────────
Write-Host "=== [3/5] Staging Files ==="
$configName = "Release $RevitVersion"
$buildDir = "$root\plugin\bin\AddIn $year $configName"
if (-not (Test-Path $buildDir)) { throw "Build output not found: $buildDir" }

$stage = "$root\dist\_stage_installer"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path "$stage" | Out-Null

# Copy built plugin + runtime
robocopy "$buildDir" $stage /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null

# Write install.ps1 (post-extraction install script)
$installPs1 = @'
$ErrorActionPreference = "Stop"
$year = REVITYEAR
$target = "$env:APPDATA\Autodesk\Revit\Addins\$year"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$logFile = "$env:TEMP\mcp-revit-install-log.txt"

Start-Transcript -Path $logFile -Append | Out-Null
Write-Host "=== mcp-servers-for-revit Installer ==="
Write-Host "Target: $target"

# Remove old installation
@("revit_mcp_plugin", "mcp-servers-for-revit.addin") | ForEach-Object {
    $old = Join-Path $target $_
    if (Test-Path $old) {
        Remove-Item $old -Recurse -Force -ErrorAction Continue
        Write-Host "  Removed old: $_"
    }
}

# Create target & copy
New-Item -ItemType Directory -Force -Path "$target" | Out-Null
Copy-Item "$source\mcp-servers-for-revit.addin" "$target\" -Force
Copy-Item "$source\revit_mcp_plugin" "$target\" -Recurse -Force

Write-Host ""
Write-Host "=== Install Complete ==="
Write-Host "Installed to: $target"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Restart Revit (if running)"
Write-Host "  2. In Revit, go to Add-Ins tab -> Revit MCP Switch"
Write-Host ""
Start-Sleep 3
Stop-Transcript | Out-Null
'@ -replace 'REVITYEAR', "$year"

Set-Content "$stage\install.ps1" $installPs1 -Encoding ASCII

# ──────────────────────────────────────────────────
# Step 4: Create IExpress SED file (CRLF)
# ──────────────────────────────────────────────────
Write-Host "=== [4/5] Generating IExpress SED ==="

$exeName = "mcp-servers-for-revit-Revit$year-Installer.exe"
$installerExe = "$root\dist\$exeName"
if (Test-Path $installerExe) { Remove-Item $installerExe -Force }

# Build file list: all files in stage, relative to stage root
$fileList = @()
Get-ChildItem $stage -Recurse -File | ForEach-Object {
    $fileList.Add($_.FullName.Substring($stage.Length + 1))
}

# Generate SED file content as CRLF text
$crlf = "`r`n"
$sed = New-Object System.Text.StringBuilder

$sed.Append("[Version]").Append($crlf) | Out-Null
$sed.Append("Class=IEXPRESS").Append($crlf) | Out-Null
$sed.Append("SEDVersion=3").Append($crlf) | Out-Null
$sed.Append($crlf) | Out-Null

$sed.Append("[Options]").Append($crlf) | Out-Null
$sed.Append("PackagePurpose=InstallApp").Append($crlf) | Out-Null
$sed.Append("ShowInstallProgramWindow=0").Append($crlf) | Out-Null
$sed.Append("HideExtractAnimation=1").Append($crlf) | Out-Null
$sed.Append("UseLongFileName=1").Append($crlf) | Out-Null
$sed.Append("OnSuccessDialog=0").Append($crlf) | Out-Null
$sed.Append("OnFailureDialog=0").Append($crlf) | Out-Null
$sed.Append("ConfirmCancelCheck=0").Append($crlf) | Out-Null
$sed.Append("AllowContinueOnlyIf=No").Append($crlf) | Out-Null
$sed.Append("AllowSourceOverwrite=No").Append($crlf) | Out-Null
$sed.Append("DontDeleteTempFiles=1").Append($crlf) | Out-Null
$sed.Append($crlf) | Out-Null

$sed.Append("[Strings]").Append($crlf) | Out-Null
$sed.Append('InstallProgram=powershell.exe -ExecutionPolicy Bypass -File "%TMP%\install.ps1"').Append($crlf) | Out-Null
$sed.Append("ApplicationName=mcp-servers-for-revit (Revit $year)").Append($crlf) | Out-Null
$sed.Append("Author=mcp-servers-for-revit").Append($crlf) | Out-Null
$sed.Append("AppLaunched=Install Complete").Append($crlf) | Out-Null
$sed.Append("SourceDir=$stage").Append($crlf) | Out-Null
$sed.Append("TargetName=$installerExe").Append($crlf) | Out-Null
$sed.Append($crlf) | Out-Null

$sed.Append("[SourceDisksNames]").Append($crlf) | Out-Null
$sed.Append("Disk1={install}").Append($crlf) | Out-Null
$sed.Append($crlf) | Out-Null

$sed.Append("[SourceDisksFiles]").Append($crlf) | Out-Null
foreach ($f in $fileList) {
    $sed.Append("""$f""=Disk1").Append($crlf) | Out-Null
}

$sedPath = "$root\dist\installer.sed"
[System.IO.File]::WriteAllText($sedPath, $sed.ToString(), [System.Text.Encoding]::ASCII)
Write-Host "  SED: $sedPath ($($fileList.Count) files)"

# ──────────────────────────────────────────────────
# Step 5: Run IExpress to create .exe
# ──────────────────────────────────────────────────
Write-Host "=== [5/5] Creating EXE Installer ==="

$iexpress = "$env:SystemRoot\System32\iexpress.exe"

# IExpress must run in a new window (it's a GUI app)
$proc = Start-Process -FilePath $iexpress -ArgumentList @("/N", "/Q", """$sedPath""") -Wait -PassThru

if ($proc.ExitCode -ne 0) {
    # IExpress returns non-zero more often than not even on success
    # Only fail if the output .exe doesn't exist
    if (-not (Test-Path $installerExe)) {
        throw "IExpress failed. Output exe not found at: $installerExe"
    }
}

# ──────────────────────────────────────────────────
# Cleanup
# ──────────────────────────────────────────────────
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $sedPath -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================"
Write-Host "  Installer Created Successfully!"
Write-Host "========================================"
Write-Host "File: $installerExe"
$size = (Get-Item $installerExe).Length / 1MB
Write-Host "Size: $([math]::Round($size, 1)) MB"
Write-Host ""
Write-Host "Usage:"
Write-Host "  1. Copy the .exe to target machine"
Write-Host "  2. Double-click to run"
Write-Host "  3. The installer extracts files silently,"
Write-Host "     then runs install.ps1 automatically"
Write-Host "  4. Restart Revit"
Write-Host ""
