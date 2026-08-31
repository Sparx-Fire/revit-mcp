param(
    [Parameter(Mandatory)]
    [ValidatePattern('^R(20|21|22|23|24|25|26)$')]
    [string]$RevitVersion,
    [switch]$SkipServer
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$year = @{R20=2020;R21=2021;R22=2022;R23=2023;R24=2024;R25=2025;R26=2026}[$RevitVersion]
$configName = "Release $RevitVersion"    # e.g. "Release R26"
$staging = "$root\build"

# ──────────────────────────────────────────────
# [0/5] Clean staging directory
# ──────────────────────────────────────────────
Write-Host "=== [0/5] Cleaning staging: build\ ==="
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

# ──────────────────────────────────────────────
# [1/5] Build Plugin (RevitMCPPlugin.csproj)
#       OutputPath = bin\Release\{year}\  (e.g. bin\Release\2026\)
# ──────────────────────────────────────────────
Write-Host "=== [1/5] Building Plugin ($RevitVersion) ==="
dotnet build -c $configName "$root\plugin\RevitMCPPlugin.csproj"
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed" }
$pluginOut = "$root\plugin\bin\Release\$year"

# ──────────────────────────────────────────────
# [2/5] Build Command Set (RevitMCPCommandSet.csproj)
#       Default output: bin\Release R26\
# ──────────────────────────────────────────────
Write-Host "=== [2/5] Building Command Set ($RevitVersion) ==="
dotnet build -c $configName "$root\commandset\RevitMCPCommandSet.csproj"
if ($LASTEXITCODE -ne 0) { throw "Command set build failed" }
$cmdOut = "$root\commandset\bin\$configName"

# ──────────────────────────────────────────────
# [3/5] Build MCP Server (unless -SkipServer)
# ──────────────────────────────────────────────
if (-not $SkipServer) {
    Write-Host "=== [3/5] Building MCP Server ==="
    Push-Location "$root\server"
    try {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
    } finally {
        Pop-Location
    }
}

# ──────────────────────────────────────────────
# [4/5] Stage all output to build\
# ──────────────────────────────────────────────
Write-Host "=== [4/5] Staging to build\ ==="

# 4.1 .addin
Copy-Item "$root\plugin\mcp-servers-for-revit.addin" "$staging\" -Force

# 4.2 revit_mcp_plugin root DLLs (from plugin build output)
$pluginDir = "$staging\revit_mcp_plugin"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Get-ChildItem "$pluginOut\*.dll" | Copy-Item -Destination $pluginDir -Force

# 4.3 Commands\RevitMCPCommandSet\{year}\*.dll (from commandset build output)
$cmdYearDir = "$pluginDir\Commands\RevitMCPCommandSet\$year"
New-Item -ItemType Directory -Force -Path $cmdYearDir | Out-Null
# command.json → Commands\RevitMCPCommandSet\
Copy-Item "$root\command.json" "$pluginDir\Commands\RevitMCPCommandSet\" -Force
# *.dll → Commands\RevitMCPCommandSet\{year}\
Get-ChildItem "$cmdOut\*.dll" | Copy-Item -Destination $cmdYearDir -Force

# 4.4 runtime → revit_mcp_plugin\runtime\ (unless -SkipServer)
if (-not $SkipServer) {
    Write-Host "  Bundling Runtime..."
    $runtimeDir = "$pluginDir\runtime"
    New-Item -ItemType Directory -Force -Path $runtimeDir | Out-Null
    $nodeExe = (Get-Command node.exe -ErrorAction Stop).Source
    Copy-Item "$nodeExe" "$runtimeDir\node.exe" -Force
    robocopy "$root\server\build" "$runtimeDir\build" /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    robocopy "$root\server\node_modules" "$runtimeDir\node_modules" /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    Copy-Item "$root\server\package.json" "$runtimeDir\" -Force
    $runtimeMB = (Get-ChildItem $runtimeDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "  Runtime bundled: $([math]::Round($runtimeMB, 1)) MB"
}

# ──────────────────────────────────────────────
# [5/5] Summary
# ──────────────────────────────────────────────
Write-Host ""
Write-Host "=== Build Complete ==="
Write-Host "  Staging: build\"
Write-Host ""
Write-Host "  build\mcp-servers-for-revit.addin"
Write-Host "  build\revit_mcp_plugin\"
Write-Host "    +-- RevitMCPPlugin.dll + deps"
Write-Host "    +-- Commands\RevitMCPCommandSet\$year\"
Write-Host "    |   +-- RevitMCPCommandSet.dll + deps"
if (-not $SkipServer) {
    Write-Host "    +-- runtime\"
    Write-Host "        +-- node.exe"
    Write-Host "        +-- build\"
    Write-Host "        +-- node_modules\"
}
Write-Host ""
$totalMB = (Get-ChildItem $staging -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "  Total size: $([math]::Round($totalMB, 1)) MB"
Write-Host ""
Write-Host "  Next step: .\build_installer_iss.ps1"
Write-Host "    (compiles plans\mcp-servers-for-revit-安装脚本.iss via ISCC)"
