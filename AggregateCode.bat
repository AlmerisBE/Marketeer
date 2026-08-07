@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo =======================================================
echo   LLM Snapshot Generator (Marketeer)
echo =======================================================

set /p INC_MAIN="Include main project (Marketeer)? (Y/N): "
set /p INC_TESTS="Include tests project (Marketeer.Tests)? (Y/N): "
set /p RECENT_ONLY="Only include files modified in the last 24 hours? (Y/N): "

set OUTPUT_FILE=LLM_Context.txt
if exist %OUTPUT_FILE% del %OUTPUT_FILE%

echo ========================================= >> %OUTPUT_FILE%
echo PROJECT STRUCTURE >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%
tree /a /f | findstr /v /i "\.git \.vs \bin \obj \.github" >> %OUTPUT_FILE%

echo. >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%
echo FILE CONTENTS >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%

echo Generating snapshot, please wait...

:: Create a temporary PowerShell script to handle dates and paths securely
set PS_SCRIPT=%TEMP%\GenerateSnapshot.ps1
> "%PS_SCRIPT%" (
    echo $incMain = '%INC_MAIN%' -match '^[yY]'
    echo $incTests = '%INC_TESTS%' -match '^[yY]'
    echo $recent = '%RECENT_ONLY%' -match '^[yY]'
    echo $limit = (Get-Date^).AddHours(-24^)
    echo $out = '%OUTPUT_FILE%'
    echo Get-ChildItem -Path . -Include '*.cs','*.csproj','*.json','*.yml','*.sln' -Recurse ^| Where-Object {
    echo     $p = $_.FullName
    echo     if ($p -match '\\bin\\' -or $p -match '\\obj\\' -or $p -match '\\.vs\\' -or $p -match '\\.git\\' -or $p -match '\\.github\\'^) { return $false }
    echo     $isTest = $p -match '\\Marketeer\.Tests\\'
    echo     $isMain = $p -match '\\Marketeer\\' -and -not $isTest
    echo     $isRoot = -not $isTest -and -not ($p -match '\\Marketeer\\'^)
    echo     if (-not $incMain -and ($isMain -or $isRoot^)^) { return $false }
    echo     if (-not $incTests -and $isTest^) { return $false }
    echo     if ($recent -and $_.LastWriteTime -lt $limit^) { return $false }
    echo     return $true
    echo } ^| ForEach-Object {
    echo     Add-Content $out "`n--- FILE_START: $($_.Name) ---"
    echo     Add-Content $out "--- PATH: $($_.FullName) ---"
    echo     Get-Content $_.FullName ^| Add-Content $out
    echo     Add-Content $out "`n--- FILE_END: $($_.Name) ---"
    echo }
)

powershell -ExecutionPolicy Bypass -NoProfile -File "%PS_SCRIPT%"
del "%PS_SCRIPT%"

echo Extraction completed successfully in %OUTPUT_FILE%.
pause