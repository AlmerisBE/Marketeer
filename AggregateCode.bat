@echo off
setlocal enabledelayedexpansion

set OUTPUT_FILE=LLM_Context.txt
if exist %OUTPUT_FILE% del %OUTPUT_FILE%

echo ========================================= >> %OUTPUT_FILE%
echo PROJECT STRUCTURE >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%
:: Exclude hidden and generated folders from the tree view
tree /a /f | findstr /v /i "\.git \.vs \bin \obj \.github" >> %OUTPUT_FILE%

echo. >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%
echo FILE CONTENTS >> %OUTPUT_FILE%
echo ========================================= >> %OUTPUT_FILE%

:: Find relevant files and explicitly exclude paths containing \bin\, \obj\, \.vs\, and \.git\
for /f "delims=" %%i in ('dir /s /b *.cs *.csproj *.json *.yml *.sln ^| findstr /v /i "\\bin\\ \\obj\\ \\.vs\\ \\.git\\"') do (
    echo. >> %OUTPUT_FILE%
    echo --- FILE_START: %%~nxi --- >> %OUTPUT_FILE%
    echo --- PATH: %%i --- >> %OUTPUT_FILE%
    type "%%i" >> %OUTPUT_FILE%
    echo. >> %OUTPUT_FILE%
    echo --- FILE_END: %%~nxi --- >> %OUTPUT_FILE%
)

echo Extraction completed successfully in %OUTPUT_FILE%.
pause