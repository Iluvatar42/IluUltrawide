# Building IluUltrawide from Source

## Prerequisites

- **.NET SDK 8.0** or later - https://dotnet.microsoft.com/download
- **Visual Studio Code** (recommended) or Visual Studio 2022
- **Tailor Simulator** installed (for game DLLs)
- **BepInEx** installed in game (for BepInEx DLLs)

## Setup

### 1. Copy Required DLLs to `lib/` folder

You need to manually copy game DLLs from your Tailor Simulator installation:

**From:** `<Game Install>/Tailor Simulator_Data/Managed/`

Copy these files:
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.UI.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll`

**From:** `<Game Install>/BepInEx/core/`

Copy these files:
- `BepInEx.dll`
- `0Harmony.dll`

**Place all DLLs in:** `Source/lib/`

> ⚠️ These DLLs are not included in the distribution due to copyright.

### 2. Verify Project Structure

Your `Source/` folder should look like:

```
Source/
├── Plugin.cs
├── MyPluginInfo.cs
├── IluUltrawide.csproj
├── BUILD.md (this file)
└── lib/
    ├── README.txt
    ├── UnityEngine.dll
    ├── UnityEngine.CoreModule.dll
    ├── UnityEngine.UI.dll
    ├── UnityEngine.IMGUIModule.dll
    ├── UnityEngine.InputLegacyModule.dll
    ├── BepInEx.dll
    └── 0Harmony.dll
```

## Building

### Command Line (Windows/Linux/Mac)

```bash
cd Source
dotnet build -c Release
```

**Output:** `bin/Release/netstandard2.1/IluUltrawide.dll`

### Visual Studio Code

1. Open the `Source/` folder in VS Code
2. Press **Ctrl+Shift+B** (Windows/Linux) or **Cmd+Shift+B** (Mac)
3. Select `dotnet: build` task
4. Output in `bin/Release/netstandard2.1/`

### Visual Studio 2022

1. Open `IluUltrawide.csproj`
2. Set configuration to **Release**
3. Build → Build Solution
4. Output in `bin/Release/netstandard2.1/`

## Testing

### Manual Installation

Copy the built DLL to:
```
<Game Folder>/BepInEx/plugins/IluUltrawide/IluUltrawide.dll
```

### Verify Installation

1. Launch Tailor Simulator
2. Check `BepInEx/LogOutput.log` for:
   ```
   [Info :IluUltrawide] IluUltrawide v1.0.0 loaded.
   ```
3. Press **F10** to open settings overlay

## Modifying the Code

### Key Files

**Plugin.cs** - Main plugin logic
- Resolution enforcement
- UI scaling
- Settings overlay
- Harmony patches

**MyPluginInfo.cs** - Plugin metadata
- GUID, Name, Version
- Change version here for releases

**IluUltrawide.csproj** - Project configuration
- Target framework: netstandard2.1
- DLL references
- Build settings

### Making Changes

1. Edit source files
2. Rebuild: `dotnet build -c Release`
3. Copy new DLL to game
4. Test in-game

### Common Modifications

**Change default resolution:**
```csharp
// In Plugin.cs, BindConfig() method
TargetWidth = Config.Bind("1 - Resolution", "TargetWidth", 3440, ...
TargetHeight = Config.Bind("1 - Resolution", "TargetHeight", 1440, ...
```

**Change default UI scale:**
```csharp
// In Plugin.cs, BindConfig() method
UiScaleMultiplier = Config.Bind("2 - UI Scale", "UiScaleMultiplier", 1.0f, ...
```

**Change F10 keybind:**
```csharp
// In Plugin.cs, BindConfig() method
GuiToggleKey = Config.Bind("4 - GUI", "ToggleKey", KeyCode.F10, ...
```

## Troubleshooting

### "Could not find UnityEngine.dll"

Check that all DLLs are in `lib/` folder and match the names in `.csproj`.

### "netstandard version conflict" warnings

These are suppressed in the project file with `<NoWarn>`. They're safe to ignore.

### OmniSharp errors in VS Code

If you see errors but the build works:
1. Press **Ctrl+Shift+P**
2. Run: "OmniSharp: Restart OmniSharp"

### Build succeeds but mod doesn't load

1. Check `BepInEx/LogOutput.log` for errors
2. Verify DLL is in correct location
3. Make sure BepInEx is installed and working
4. Check file isn't blocked (Right-click → Properties → Unblock)

## Creating a Release

1. Build in **Release** configuration
2. Copy `bin/Release/netstandard2.1/IluUltrawide.dll`
3. Place in `BepInEx/plugins/IluUltrawide/` folder
4. Archive with source code and documentation
5. Distribute!

## Advanced: Auto-Deploy on Build

Add this to `.csproj` to auto-copy on build:

```xml
<Target Name="CopyToGame" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Copy
    SourceFiles="$(OutputPath)$(AssemblyName).dll"
    DestinationFolder="C:\Path\To\Game\BepInEx\plugins\IluUltrawide\"
    OverwriteReadOnlyFiles="true" />
</Target>
```

Change the path to match your game installation.
