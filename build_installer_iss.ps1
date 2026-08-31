param(
    [ValidatePattern('^R(20|21|22|23|24|25|26)$')]
    [string]$RevitVersion = "R26",
    [switch]$SkipServer,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$issPath = "$root\plans\mcp-servers-for-revit-安装脚本.iss"
$year = @{R20=2020;R21=2021;R22=2022;R23=2023;R24=2024;R25=2025;R26=2026}[$RevitVersion]

# ──────────────────────────────────────────────────
# Step 1: Build → outputs to build\ (unless -SkipBuild)
# ──────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "=== [1/2] Build (Revit $RevitVersion) ==="
    $buildArgs = @("-RevitVersion", $RevitVersion)
    if ($SkipServer) { $buildArgs += "-SkipServer" }
    & "$root\build.ps1" $buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
} else {
    Write-Host "=== [1/2] Skip build (build\ must already exist) ==="
}

# ──────────────────────────────────────────────────
# Step 2: Compile Inno Setup installer
# ──────────────────────────────────────────────────
Write-Host "=== [2/2] Compiling Inno Setup Installer ==="

# Locate ISCC.exe
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
)
$iscc = $null
foreach ($c in $isccCandidates) {
    if (Test-Path $c) { $iscc = $c; break }
}

if (-not $iscc) {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════╗"
    Write-Host "║  Inno Setup Compiler (ISCC.exe) 未找到！         ║"
    Write-Host "║                                                  ║"
    Write-Host "║  请先安装 Inno Setup:                            ║"
    Write-Host "║  https://jrsoftware.org/isdl.php                 ║"
    Write-Host "║                                                  ║"
    Write-Host "║  安装后重新运行本脚本，或手动编译：               ║"
    Write-Host "║  ISCC.exe /DRevitYear=$year \                    ║"
    Write-Host "║         /DRevitVersionParam=$RevitVersion \      ║"
    Write-Host "║         ""$issPath""                             ║"
    Write-Host "╚══════════════════════════════════════════════════╝"
    return
}

$isccArgs = @(
    "/DRevitYear=$year",
    "/DRevitVersionParam=$RevitVersion",
    "`"$issPath`""
)

Write-Host "  ISCC: $iscc"
Write-Host "  Args: $isccArgs"
& $iscc $isccArgs
if ($LASTEXITCODE -ne 0) { throw "ISCC compile failed (exit code: $LASTEXITCODE)" }

# Show result
$outputDir = "$root\dist"
$result = Get-ChildItem $outputDir -Filter "mcp-servers-for-revit*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($result) {
    $size = $result.Length / 1MB
    Write-Host ""
    Write-Host "========================================"
    Write-Host "  Installer Created Successfully!"
    Write-Host "========================================"
    Write-Host "File: $($result.FullName)"
    Write-Host "Size: $([math]::Round($size, 1)) MB"
    Write-Host ""
    Write-Host "双击运行即可自动安装到:"
    Write-Host "  %APPDATA%\Autodesk\Revit\Addins\$year"
    Write-Host ""
}
