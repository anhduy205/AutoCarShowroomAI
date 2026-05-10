# =====================================================
# SCRIPT: Setup Car Images for Auto Car Showroom
# Chay script nay de copy anh xe vao wwwroot va cap nhat DB
# Usage: powershell -ExecutionPolicy Bypass -File setup-car-images.ps1
# =====================================================

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$wwwroot = Join-Path $root "Showroom.Web\wwwroot\uploads\cars"
$imgSrc = "C:\Users\Admin\.gemini\antigravity\brain\0ffeed09-b28b-492d-942a-f1e3cfbffb23"

# Map: CarId => generated image filename
$carImages = @{
    1 = "toyota_camry_1778337004572.png"
    2 = "toyota_corolla_cross_1778337019812.png"
    3 = "hyundai_accent_1778337034324.png"
    4 = "hyundai_tucson_1778337054842.png"
    5 = "ford_everest_1778337069570.png"
    6 = "mazda_cx5_1778337086838.png"
}

Write-Host "=== STEP 1: Copy images to wwwroot ===" -ForegroundColor Cyan

foreach ($entry in $carImages.GetEnumerator()) {
    $carId = $entry.Key
    $fileName = $entry.Value
    $destDir = Join-Path $wwwroot $carId
    $srcFile = Join-Path $imgSrc $fileName
    $destFile = Join-Path $destDir "main.png"

    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    if (Test-Path $srcFile) {
        Copy-Item $srcFile $destFile -Force
        Write-Host "  [OK] Car $carId : $fileName -> $destFile" -ForegroundColor Green
    } else {
        Write-Host "  [SKIP] Car $carId : Source not found: $srcFile" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== STEP 2: Update database ImageUrl ===" -ForegroundColor Cyan

$sqlScript = @"
UPDATE Cars SET ImageUrl = '/uploads/cars/1/main.png' WHERE Id = 1;
UPDATE Cars SET ImageUrl = '/uploads/cars/2/main.png' WHERE Id = 2;
UPDATE Cars SET ImageUrl = '/uploads/cars/3/main.png' WHERE Id = 3;
UPDATE Cars SET ImageUrl = '/uploads/cars/4/main.png' WHERE Id = 4;
UPDATE Cars SET ImageUrl = '/uploads/cars/5/main.png' WHERE Id = 5;
UPDATE Cars SET ImageUrl = '/uploads/cars/6/main.png' WHERE Id = 6;
SELECT Id, Name, ImageUrl FROM Cars ORDER BY Id;
"@

try {
    sqlcmd -S ".\SQLEXPRESS" -E -d "AutoCarShowroomDb" -Q $sqlScript
    Write-Host ""
    Write-Host "=== ALL DONE ===" -ForegroundColor Green
    Write-Host "Restart app (dotnet run) to see images on the website." -ForegroundColor White
} catch {
    Write-Host "  [ERROR] SQL update failed: $_" -ForegroundColor Red
    Write-Host "  Try running manually: sqlcmd -S .\SQLEXPRESS -E -d AutoCarShowroomDb" -ForegroundColor Yellow
}
