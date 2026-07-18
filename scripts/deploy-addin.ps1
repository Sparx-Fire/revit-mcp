param(
    [string[]]$RevitVersions = @("2025", "2026")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (Get-Process -Name "Revit" -ErrorAction SilentlyContinue) {
    Write-Error "Revit is running. Close Revit before deploying the add-in."
}

foreach ($version in $RevitVersions) {
    $configName = "Release R" + $version.Substring(2)
    $sourceDir = Join-Path $repoRoot "plugin\bin\AddIn $version $configName"
    $targetDir = "C:\ProgramData\Autodesk\Revit\Addins\$version"

    if (-not (Test-Path $sourceDir)) {
        Write-Warning "Build output not found: $sourceDir (run: dotnet build plugin/RevitMCPPlugin.csproj -c '$configName')"
        continue
    }
    if (-not (Test-Path $targetDir)) {
        Write-Warning "Revit $version addins folder not found: $targetDir"
        continue
    }

    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Recurse -Force
    Write-Host "Deployed Revit $version add-in to $targetDir"

    $staleManifest = Join-Path $targetDir "revit-mcp.addin"
    if (Test-Path $staleManifest) {
        Remove-Item $staleManifest
        Write-Host "Removed stale manifest: $staleManifest"
    }
}
