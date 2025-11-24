# Delta Force Balance Tracker 🎮

A portable Windows desktop application that tracks your in-game balance in **Delta Force** using OCR technology. Monitor your profits and losses, view historical data, and analyze your performance over time.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)

## ✨ Features

- **📸 OCR Balance Scanning**: Automatically reads your Total Assets from the game screen
- **⌨️ Hotkey Support**: Default F8 hotkey (customizable)
- **📊 P&L Tracking**: Daily profit/loss calculations with color-coded display
- **📈 Analytics Dashboard**: Charts and statistics showing balance history
- **🌍 Multi-Language**: Supports both English and Russian game localization
- **💾 Local Storage**: All data stored locally in SQLite database
- **🎨 Modern UI**: Clean, minimal dark theme interface
- **📦 Portable**: Single executable, no installation required

## 🚀 Quick Start

### For End Users

1. **Download** the latest release from the [Releases](../../releases) page or GitHub Actions artifacts
2. **Extract** the folder to any location
3. **Run** `DeltaForceTracker.exe`
4. **First Time Setup**:
   - Click "Select OCR Region"
   - Drag to select the area containing "Total Assets" and the balance value
   - Press F8 while in-game to scan your balance

### System Requirements

- Windows 10 or Windows 11
- .NET 6.0 Runtime (included in self-contained build)
- Screen resolution: 1920x1080 or higher recommended

## 📖 How to Use

### Scanning Your Balance

1. Launch the Delta Force game
2. Navigate to a screen showing "Total Assets" or "Общие активы"
3. Press **F8** (or your custom hotkey) to scan
4. The app will automatically detect and record your balance

### Understanding the Dashboard

- **Current Balance**: Your latest scanned balance
- **Daily P&L**: Profit/Loss since the first scan of the day
  - 🟢 Green = Profit
  - 🔴 Red = Loss
- **Status**: Current scan status and messages

### Analytics Tab

View comprehensive statistics including:
- **Balance History Chart**: Visual representation of balance changes
- **Best Day**: Your highest profit day
- **Worst Day**: Your biggest loss day
- **Highest Balance**: All-time peak balance
- **Total Scans**: Number of scans performed
- **Scan History Table**: Detailed log of all scans

### Customization

- **Change Hotkey**: Click "Change Hotkey" and press your desired key
- **Update OCR Region**: Click "Select OCR Region" to redefine the scan area

## 🔧 Building from Source

### Prerequisites

- Windows 10/11
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Git (optional)

### Build Steps

1. **Clone or download** this repository
2. **Run** the build script:
   ```batch
   build.bat
   ```
3. The portable executable will be in the `release` folder

### Manual Build

```batch
# Download Tesseract language data
mkdir tessdata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -o tessdata/eng.traineddata
curl -L https://github.com/tesseract-ocr/tessdata/raw/main/rus.traineddata -o tessdata/rus.traineddata

# Restore and publish
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./release

# Copy Tesseract data
xcopy /E /I /Y tessdata release\tessdata
```

## 📁 Data Storage

All data is stored locally in:
```
%AppData%\DeltaForceTracker\balances.db
```

The database contains:
- All balance scans with timestamps
- Application settings (hotkey, OCR region)

## 🎯 Supported Balance Formats

The OCR engine can read:
- **English**: `902.4M`, `614.5K`, `120M`
- **Russian**: `902,4М`, `614,5К` (comma decimal separator)

Suffixes:
- `K` / `К` = ×1,000
- `M` / `М` = ×1,000,000

## 🐛 Troubleshooting

### OCR Not Working

1. **Ensure tessdata folder exists** alongside the .exe with `eng.traineddata` and `rus.traineddata`
2. **Select a larger region** that clearly contains the label and value
3. **Check game resolution** - higher resolution = better OCR accuracy
4. **Verify label visibility** - ensure "Total Assets" or "Общие активы" is visible

### Hotkey Not Responding

1. **Check for conflicts** - another app might be using the same hotkey
2. **Run as Administrator** (some games require elevated permissions)
3. **Try a different key** using "Change Hotkey"

### Error on Startup

If you see "tessdata folder not found":
1. Make sure the `tessdata` folder is in the same directory as `DeltaForceTracker.exe`
2. Download language files manually from [Tesseract GitHub](https://github.com/tesseract-ocr/tessdata)

## 🛠️ Technology Stack

- **Framework**: .NET 6.0 WPF
- **OCR**: Tesseract 5.2.0
- **Database**: SQLite
- **Charts**: LiveCharts2
- **UI**: XAML with modern dark theme

## 📋 Architecture

```
DeltaForceTracker/
├── Database/           # SQLite data access layer
├── Models/            # Data models (BalanceScan, DailyStats)
├── OCR/               # Tesseract OCR engine & screen capture
├── Hotkeys/           # Global hotkey registration
├── Views/             # UI dialogs (region selector, hotkey dialog)
├── Utils/             # Value parsing and formatting utilities
├── Resources/         # XAML styles and themes
└── MainWindow.xaml    # Main application window
```

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) - Open-source OCR engine
- [LiveCharts2](https://github.com/beto-rodriguez/LiveCharts2) - Charting library
- Delta Force game by TiMi Studio Group

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ⚠️ Disclaimer

This application is not affiliated with or endorsed by Delta Force or TiMi Studio Group. It is a fan-made tool for personal use only. Use at your own risk.

---

**Happy Tracking! 🎮📊**
