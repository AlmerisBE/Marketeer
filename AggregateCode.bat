@echo off
setlocal enabledelayedexpansion

:: Define absolute path for the output file relative to the script location
set "OUTPUT_FILE=%~dp0LLM_Context.txt"
if exist "!OUTPUT_FILE!" del "!OUTPUT_FILE!"

echo =========================================
echo LLM Context Extraction Script
echo =========================================
echo 1. Consolidate Entire Solution
echo 2. Consolidate Main Project Only (Marketeer)
echo 3. Consolidate Tests Project Only (Marketeer.Tests)
echo 4. Consolidate Specific Feature / Folder
echo.
set /p TARGET_CHOICE="Select an option (1-4): "

set "TARGET_DIR=."
if "!TARGET_CHOICE!"=="2" set "TARGET_DIR=Marketeer"
if "!TARGET_CHOICE!"=="3" set "TARGET_DIR=Marketeer.Tests"
if "!TARGET_CHOICE!"=="4" (
    echo.
    set /p "TARGET_DIR=Enter the relative path (e.g., Marketeer\Core\MarketWatch): "
)

if not exist "!TARGET_DIR!" (
    echo.
    echo [ERROR] The directory '!TARGET_DIR!' does not exist.
    pause
    exit /b
)

echo.
echo Extracting context from: !TARGET_DIR! ...

echo ========================================= >> "!OUTPUT_FILE!"
echo PROJECT STRUCTURE (!TARGET_DIR!) >> "!OUTPUT_FILE!"
echo ========================================= >> "!OUTPUT_FILE!"

pushd "!TARGET_DIR!"
tree /a /f | findstr /v /i "\.git \.vs \bin \obj \.github" >> "!OUTPUT_FILE!"

echo. >> "!OUTPUT_FILE!"
echo ========================================= >> "!OUTPUT_FILE!"
echo FILE CONTENTS >> "!OUTPUT_FILE!"
echo ========================================= >> "!OUTPUT_FILE!"

:: Suppress stderr (2>nul) so missing file extensions don't clutter the console
for /f "delims=" %%i in ('dir /s /b *.cs *.csproj *.json *.yml *.sln 2^>nul ^| findstr /v /i "\\bin\\ \\obj\\ \\.vs\\ \\.git\\ \\.github\\"') do (
    echo. >> "!OUTPUT_FILE!"
    echo --- FILE_START: %%~nxi --- >> "!OUTPUT_FILE!"
    echo --- PATH: %%i --- >> "!OUTPUT_FILE!"
    type "%%i" >> "!OUTPUT_FILE!"
    echo. >> "!OUTPUT_FILE!"
    echo --- FILE_END: %%~nxi --- >> "!OUTPUT_FILE!"
)
popd

echo.
echo Extraction completed successfully in LLM_Context.txt.
pause