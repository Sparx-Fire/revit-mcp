$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = "$root\build"

# Validate build\ exists
if (-not (Test-Path $source)) {
    throw "build\ directory not found. Run build.ps1 first."
}
if (-not (Test-Path "$source\revit_mcp_plugin") -or -not (Test-Path "$source\mcp-servers-for-revit.addin")) {
    throw "build\ is incomplete. Run build.ps1 -RevitVersion R?? (e.g. R26) first."
}

# Detect Revit year from Commands\RevitMCPCommandSet\{year}\ subdirectory
$yearDir = Get-ChildItem "$source\revit_mcp_plugin\Commands\RevitMCPCommandSet" -Directory | Select-Object -First 1
if (-not $yearDir) {
    throw "Cannot determine Revit year: no version directory under Commands\RevitMCPCommandSet\"
}
$year = [int]$yearDir.Name
Write-Host "Build: $source (Revit $year)"

$target = "$env:APPDATA\Autodesk\Revit\Addins\$year"
$pluginTarget = "$target\revit_mcp_plugin"

Write-Host "Target: $target"

# Remove old installation (gracefully handle locked files)
if (Test-Path $pluginTarget) {
    $emptyDir = "$env:TEMP\_empty_for_rmdir"
    if (-not (Test-Path $emptyDir)) { New-Item -ItemType Directory -Force -Path $emptyDir | Out-Null }
    robocopy $emptyDir $pluginTarget /MIR /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    Remove-Item -LiteralPath $pluginTarget -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "$target\mcp-servers-for-revit.addin") {
    Remove-Item -LiteralPath "$target\mcp-servers-for-revit.addin" -Force -ErrorAction SilentlyContinue
}
# Also remove old leftover top-level RevitMCPCommandSet if present
$oldRootCmdSet = "$target\RevitMCPCommandSet"
if (Test-Path $oldRootCmdSet) {
    Remove-Item -LiteralPath $oldRootCmdSet -Recurse -Force -ErrorAction SilentlyContinue
}

# Copy new files
robocopy "$source\revit_mcp_plugin" "$pluginTarget" /E /R:2 /W:2 /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
Copy-Item -LiteralPath "$source\mcp-servers-for-revit.addin" -Destination "$target\" -Force -ErrorAction SilentlyContinue

# Generate commandRegistry.json from command.json
$commandJsonFile = "$pluginTarget\Commands\RevitMCPCommandSet\command.json"
$registryFile = "$pluginTarget\Commands\commandRegistry.json"
if (Test-Path $commandJsonFile) {
    Write-Host "Generating command registry from command.json..."
    $commandList = Get-Content $commandJsonFile -Raw | ConvertFrom-Json | Select-Object -ExpandProperty commands
    $registry = [PSCustomObject]@{
        Commands = $commandList | ForEach-Object {
            [PSCustomObject]@{
                commandName            = $_.commandName
                assemblyPath           = "RevitMCPCommandSet\{VERSION}\RevitMCPCommandSet.dll"
                enabled                = $true
                supportedRevitVersions = @($year)
                developer              = $_.developer
                description            = $_.description
            }
        }
    }
    $registry | ConvertTo-Json -Depth 5 | Set-Content $registryFile -Encoding UTF8
    Write-Host "commandRegistry.json generated with $($commandList.Count) commands"
}

Write-Host "=== Install Complete ==="
Write-Host "Installed to: $target"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Restart Revit (if running)"
Write-Host "2. Click 'Revit MCP Switch' in Add-Ins ribbon to start the service"
Write-Host "3. Configure AI client with the local runtime:"
Write-Host ""
Write-Host "   Claude Desktop config:"
Write-Host '   {'
Write-Host '       "mcpServers": {'
Write-Host '           "mcp-server-for-revit": {'
Write-Host "               ""command"": ""$pluginTarget\runtime\node.exe"","
Write-Host "               ""args"": [""$pluginTarget\runtime\build\index.js""]"
Write-Host '           }'
Write-Host '       }'
Write-Host '   }'
