# Building IluUltrawide from Source

## Prerequisites

- .NET SDK 8.0 or later
- .NET Framework 4.6.2 Developer Pack (for netstandard2.1 support)
- Visual Studio Code with C# extension (recommended) OR Visual Studio 2022

## Setup

### 1. Copy Game DLLs to lib/ folder

Create a `lib/` folder in the Source directory and copy these files:

**From `<Game Install>/Tailor Simulator_Data/Managed/`:**
- UnityEngine.dll
- UnityEngine.CoreModule.dll
- UnityEngine.UI.dll
- UnityEngine.IMGUIModule.dll
- UnityEngine.InputLegacyModule.dll

**From `<Game Install>/BepInEx/core/`:**
- BepInEx.dll
- 0Harmony.dll

## Building

### Command Line
```bash
cd Source
dotnet build -c Release
```

Output: `bin/Release/netstandard2.1/IluUltrawide.dll`

## Project Structure

```
Source/
├── TailorSimulatorUltrawide.csproj   # Project file
├── src/
│   ├── Plugin.cs                      # Main plugin code
│   └── MyPluginInfo.cs                # Plugin metadata
└── lib/                               # Game DLLs (you provide)
    ├── UnityEngine*.dll
    ├── BepInEx.dll
    └── 0Harmony.dll
```
