# Building Delta Force Balance Tracker

This document provides detailed instructions for building the portable Windows executable.

## Prerequisites

1. **Operating System**: Windows 10 or Windows 11
2. **Development Tools**:
   - [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
   - Git (optional, for cloning the repository)
3. **Internet Connection**: Required for downloading Tesseract language data

## Automated Build (Recommended)

### Using the Build Script

The easiest way to build is using the included batch script:

```batch
build.bat
```

This script will:
1. Download Tesseract language data (English and Russian)
2. Restore NuGet dependencies
3. Build the project
4. Publish as a self-contained executable
5. Copy all necessary files to the `release` folder

### Using GitHub Actions

If you don't have a Windows machine, you can use GitHub Actions:

1. **Fork** this repository to your GitHub account
2. **Push** any change or manually trigger the workflow
3. Go to **Actions** tab in your repository
4. Select the **Build Windows Executable** workflow
5. Download the artifact from the completed run

The artifact will contain:
- `DeltaForceTracker.exe`
- `tessdata/` folder with language files

## Manual Build Instructions

### Step 1: Download Tesseract Language Data

Create a `tessdata` folder and download the required language files:

```batch
mkdir tessdata
cd tessdata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -o eng.traineddata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/rus.traineddata -o rus.traineddata
cd ..
```

**Alternative**: Download manually from [Tesseract GitHub](https://github.com/tesseract-ocr/tessdata) and place in `tessdata` folder.

### Step 2: Restore Dependencies

```batch
dotnet restore
```

### Step 3: Build in Debug Mode (Optional)

For testing:

```batch
dotnet build -c Debug
```

### Step 4: Publish Portable Executable

For a self-contained, single-file executable:

```batch
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o ./release
```

**Publish Parameters Explained**:
- `-c Release`: Release configuration (optimized)
- `-r win-x64`: Target Windows 64-bit
- `--self-contained true`: Include .NET runtime (no installation needed)
- `PublishSingleFile=true`: Bundle into single .exe
- `IncludeNativeLibrariesForSelfExtract=true`: Include native dependencies
- `EnableCompressionInSingleFile=true`: Compress to reduce size
- `DebugType=None`: No debug symbols
- `-o ./release`: Output to release folder

### Step 5: Copy Tesseract Data

```batch
xcopy /E /I /Y tessdata release\tessdata
```

### Step 6: Test the Build

```batch
cd release
DeltaForceTracker.exe
```

## Build Output

The `release` folder will contain:
- **DeltaForceTracker.exe** (~50-100 MB) - The main executable
- **tessdata/** folder with:
  - `eng.traineddata` - English OCR data
  - `rus.traineddata` - Russian OCR data

## Distribution

To distribute the application:

1. **Zip the release folder** including both the .exe and tessdata folder
2. Users should extract and run the .exe
3. The tessdata folder must remain in the same directory as the .exe

## Troubleshooting Build Issues

### "dotnet: command not found"

**Solution**: Install the .NET 6.0 SDK from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/6.0)

### NuGet Package Restore Fails

**Solution**: 
```batch
dotnet nuget locals all --clear
dotnet restore --force
```

### Tesseract Download Fails

**Solution**: Download manually from [GitHub](https://github.com/tesseract-ocr/tessdata):
- Get `eng.traineddata`
- Get `rus.traineddata`
- Place in `tessdata` folder

### Build Succeeds But Exe Crashes

**Common Issues**:
1. **Missing tessdata folder**: Ensure tessdata is next to the .exe
2. **Antivirus blocking**: Add exception for the executable
3. **Missing Visual C++ Runtime**: Install [VC++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)

## Advanced Build Options

### Debug Build with Symbols

```batch
dotnet publish -c Debug -r win-x64 --self-contained true -o ./debug
```

### Framework-Dependent Build (Smaller Size)

Requires .NET 6.0 to be installed on target machine:

```batch
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./release-fd
```

### Trimmed Build (Experimental)

**Warning**: May break Tesseract functionality.

```batch
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -o ./release-trimmed
```

## Continuous Integration

The GitHub Actions workflow (`.github/workflows/build.yml`) automatically builds on:
- Push to `main` or `master` branch
- Pull requests
- Manual trigger via workflow_dispatch

## Code Signing (Optional)

For production distribution, consider code signing:

```batch
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com release\DeltaForceTracker.exe
```

## Version Updates

Update version in `DeltaForceTracker.csproj`:

```xml
<Version>1.0.1</Version>
```

---

**Questions?** Open an issue on GitHub or contact the maintainer.
