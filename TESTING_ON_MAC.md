# Testing on macOS

Since the Delta Force Balance Tracker is a Windows-only application (WPF/C#), you have several options for testing on macOS:

## Option 1: Virtual Machine (Recommended)

**Best for:** Full testing with real Windows environment

### Using Parallels Desktop (Commercial, ~$100/year)
1. **Install Parallels Desktop**: https://www.parallels.com/
2. **Create Windows 11 VM**:
   - Parallels can download Windows automatically
   - Allocate 4GB+ RAM, 2+ CPU cores
3. **Transfer files**:
   - Use shared folders between macOS and Windows VM
   - Copy the release folder or clone the Git repo
4. **Build in VM**:
   - Install .NET 6.0 SDK in Windows VM
   - Run `build.bat`
5. **Test the application**

**Pros**:
- Native Windows environment
- Full .NET support
- Can test everything including OCR and hotkeys
- Fast and smooth on Apple Silicon Macs

**Cons**:
- Costs money ($99/year)
- Requires disk space (~30GB for Windows)

### Using VMware Fusion (Free for personal use)
1. **Download VMware Fusion Player**: https://www.vmware.com/products/fusion.html
2. **Download Windows 11 ISO**: https://www.microsoft.com/software-download/windows11
3. **Create VM** and install Windows
4. Same as Parallels above

**Pros**:
- Free for personal use
- Good performance

**Cons**:
- Slower than Parallels
- More manual setup

### Using UTM (Free, Open Source)
1. **Install UTM**: https://mac.getutm.app/
2. **Download Windows 11 ARM ISO** (for Apple Silicon) or x64 (for Intel)
3. **Create VM**
4. Build and test

**Pros**:
- Completely free
- Open-source

**Cons**:
- Slower performance
- May have compatibility issues

## Option 2: GitHub Actions (No macOS Testing)

**Best for:** Building without testing

You can use GitHub Actions to build the Windows executable, but you won't be able to test it on macOS.

**Process**:
1. Push code to GitHub
2. GitHub Actions builds on Windows runner
3. Download artifact
4. Ask someone with Windows to test
5. Or test yourself when you have access to Windows

**Pros**:
- No cost
- No VM needed
- Automated builds

**Cons**:
- Cannot test on macOS
- Need access to Windows for testing

## Option 3: Wine/CrossOver (Limited Success)

**Best for:** Quick tests, but unreliable for WPF apps

### Using Wine
1. **Install Homebrew**: https://brew.sh/
2. **Install Wine**:
   ```bash
   brew install --cask wine-stable
   ```
3. **Try to run the .exe**:
   ```bash
   wine DeltaForceTracker.exe
   ```

**Expected Result**: ❌ **Will likely NOT work**
- WPF applications require many Windows-specific libraries
- .NET 6.0 WPF is not fully supported by Wine
- OCR and hotkeys definitely won't work

### Using CrossOver (Commercial, ~$60)
Similar to Wine but with better compatibility.

**Expected Result**: ⚠️ **Might work partially**
- Better than Wine, but WPF support is still limited
- Not recommended for serious testing

## Option 4: Cloud Windows VM (Pay per use)

**Best for:** Occasional testing without buying VM software

### Using Azure Virtual Machines
1. **Sign up** for Azure: https://azure.microsoft.com/
2. **Create Windows 11 VM** (B2s size is enough)
3. **Connect via RDP** (Remote Desktop)
4. **Build and test**
5. **Delete VM** when done

**Cost**: ~$0.05-0.10/hour (delete when not using)

### Using AWS WorkSpaces
Similar to Azure, pay per hour.

**Pros**:
- No local VM needed
- Pay only when using
- Can access from anywhere

**Cons**:
- Costs money
- Requires internet connection
- Remote Desktop may be slow

## Option 5: Ask Someone with Windows

**Best for:** If you have friends/colleagues with Windows

1. Build using GitHub Actions
2. Send them the artifact
3. Ask them to test
4. Get feedback

## Recommended Approach for You

Based on your situation (no Windows PC), here's what I recommend:

### For Development & Testing:
**Use GitHub Actions + Parallels/VMware VM**

1. **For quick builds**: Use GitHub Actions
   - Push to GitHub
   - Download artifact
   - No need to build on macOS

2. **For testing**: Get a VM (one-time setup)
   - **If you have budget**: Parallels Desktop ($99/year)
   - **If you want free**: VMware Fusion Player or UTM
   - Install Windows 11
   - Test the application properly

### Minimal Testing (Without VM):

If you don't want to set up a VM right now:

1. **Push to GitHub** and use GitHub Actions to build
2. **Ask the user** (whoever will use this app) to test it
3. **Iterate** based on their feedback
4. **Later**, when needed, set up a VM for detailed debugging

## Quick VM Setup Guide (Parallels)

If you decide to go with Parallels:

```bash
# 1. Download Parallels Desktop trial (14 days free)
# https://www.parallels.com/

# 2. Install Parallels

# 3. Create Windows 11 VM (Parallels will guide you)

# 4. In Windows VM, open PowerShell and run:
# Install .NET 6.0 SDK
winget install Microsoft.DotNet.SDK.6

# 5. Clone your repo in Windows VM
git clone https://github.com/YOUR_USERNAME/DeltaForceTracker.git
cd DeltaForceTracker

# 6. Build
.\build.bat

# 7. Test
cd release
.\DeltaForceTracker.exe
```

## What Can You Test on macOS?

**Without Windows VM:**
- ✅ Code quality (syntax, structure)
- ✅ Git operations
- ✅ GitHub Actions workflow
- ❌ Running the application
- ❌ OCR functionality
- ❌ UI appearance
- ❌ Hotkeys
- ❌ Database operations

**With Windows VM:**
- ✅ Everything above
- ✅ Running the application
- ✅ OCR functionality (with test screenshots)
- ✅ UI appearance
- ✅ Hotkeys
- ✅ Database operations
- ✅ Full end-to-end testing

## Summary

| Option | Cost | Setup Time | Testing Quality | Recommended? |
|--------|------|------------|-----------------|--------------|
| **Parallels VM** | $99/year | 30 min | ⭐⭐⭐⭐⭐ | ✅ **Yes** |
| **VMware Fusion** | Free | 1 hour | ⭐⭐⭐⭐ | ✅ **Yes** |
| **UTM** | Free | 1-2 hours | ⭐⭐⭐ | ⚠️ OK |
| **GitHub Actions** | Free | 5 min | ⭐ (no testing) | ✅ For builds |
| **Wine/CrossOver** | Free/$60 | 15 min | ⭐ (won't work) | ❌ No |
| **Cloud VM** | ~$0.10/hr | 20 min | ⭐⭐⭐⭐ | ⚠️ For occasional use |
| **Ask someone** | Free | 0 min | ⭐⭐⭐⭐⭐ | ✅ If available |

## My Recommendation

**For immediate use:**
- Use **GitHub Actions** to build
- Download and give to the end user to test

**For future development:**
- Get **Parallels Desktop** (or free VMware Fusion)
- It's a one-time setup that will help for any Windows development
- Worth it if you plan to maintain this project

---

Need help setting up any of these? Let me know!
