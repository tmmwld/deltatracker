@echo off
echo ======================================
echo Delta Force Balance Tracker Builder
echo ======================================
echo.

REM Check if .NET 6 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 6.0 SDK is not installed.
    echo Please download and install from: https://dotnet.microsoft.com/download/dotnet/6.0
    pause
    exit /b 1
)

echo [1/4] Downloading Tesseract language data...
if not exist tessdata mkdir tessdata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -o tessdata/eng.traineddata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/rus.traineddata -o tessdata/rus.traineddata

echo.
echo [2/4] Restoring dependencies...
dotnet restore

echo.
echo [3/4] Building application...
dotnet build -c Release

echo.
echo [4/4] Publishing portable executable...
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o ./release

echo.
echo Copying Tesseract data...
xcopy /E /I /Y tessdata release\tessdata

echo.
echo ======================================
echo Build Complete!
echo ======================================
echo.
echo The portable executable is located at:
echo %cd%\release\DeltaForceTracker.exe
echo.
echo You can now distribute this folder.
echo.
pause
