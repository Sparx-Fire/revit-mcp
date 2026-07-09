# Настройка Revit MCP для проектировщика (без разработки)
# Запуск: PowerShell → правый клик «Выполнить» или: .\scripts\setup-user.ps1

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Example = Join-Path $RepoRoot ".cursor\mcp.json.example"
$Target = Join-Path $RepoRoot ".cursor\mcp.json"

Write-Host ""
Write-Host "=== Настройка Revit MCP для Cursor ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $Example)) {
    Write-Host "Ошибка: не найден $Example" -ForegroundColor Red
    exit 1
}

if (Test-Path $Target) {
    Write-Host "Файл уже существует: $Target" -ForegroundColor Yellow
    $answer = Read-Host "Перезаписать? (y/n)"
    if ($answer -ne "y") {
        Write-Host "Пропуск копирования mcp.json"
    } else {
        Copy-Item $Example $Target -Force
        Write-Host "Обновлён: $Target" -ForegroundColor Green
    }
} else {
    New-Item -ItemType Directory -Force -Path (Split-Path $Target) | Out-Null
    Copy-Item $Example $Target -Force
    Write-Host "Создан: $Target" -ForegroundColor Green
}

Write-Host ""
Write-Host "Дальше вручную:" -ForegroundColor Cyan
Write-Host "  1. Установите Node.js 18+ (nodejs.org)"
Write-Host "  2. Установите плагин Revit из Releases в %AppData%\Autodesk\Revit\Addins\<версия>\"
Write-Host "  3. Revit: Open Server + Settings (все команды) + Save"
Write-Host "  4. Cursor: откройте папку $RepoRoot"
Write-Host "  5. Перезапустите Cursor"
Write-Host "  6. В чате: «Проверь связь с Revit по правилам revit-mcp»"
Write-Host ""
Write-Host "Руководство: docs\user-guide-revit-mcp.md" -ForegroundColor Green
Write-Host ""
