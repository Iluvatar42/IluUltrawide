# IluUltrawide - Tailor Simulator Ultrawide Fix

A BepInEx plugin that adds ultrawide (21:9, 32:9) and custom resolution support to Tailor Simulator.

## Features

- **Custom Resolution Support**: Play at any resolution including 3440×1440, 5120×1440, and more
- **UI Scaling**: Automatic and manual UI scaling to prevent stretched or tiny UI elements
- **In-Game Settings Menu**: Press F10 to open an overlay with live-adjustable settings
- **Black Bar Removal**: Removes letterboxing and pillarboxing
- **Aspect Ratio Fixes**: Properly scales UI elements for ultrawide displays

## Installation

### Prerequisites
1. **BepInEx 5.4.23** or later
   - Download from: https://github.com/BepInEx/BepInEx/releases
   - Install the `BepInEx_win_x64_5.4.23.5.zip` version
   - Extract to your Tailor Simulator game folder

### Plugin Installation
1. Download the latest release
2. Extract `IluUltrawide.dll` to:
   ```
   <Game Folder>/BepInEx/plugins/IluUltrawide/
   ```
3. Launch the game

## Usage

### First Launch
The plugin will automatically:
- Set your resolution to 3440×1440 (configurable)
- Apply UI scaling
- Remove black bars

### In-Game Settings (F10)
Press **F10** to open the settings overlay:

**Resolution:**
- Click preset buttons for common resolutions
- Use custom W/H fields for any resolution
- Click "Apply" to change instantly

**UI Scale:**
- Use the slider or type a value (0.25 - 2.5)
- 1.0 = automatic scaling for your aspect ratio
- Lower values = smaller UI
- Higher values = larger UI

**Force Apply:**
- Click to reapply all settings immediately

### Configuration File
Settings are saved to:
```
<Game Folder>/BepInEx/config/com.ultrawide.tailorsimulator.cfg
```

You can edit this file to change:
- Default resolution
- UI scale multiplier
- Keybind for settings menu (default: F10)
- Enable/disable features

## Troubleshooting

**UI is too small/large:**
- Press F10
- Adjust the UI Scale slider
- Try values between 0.7 - 1.5

**Resolution not applying:**
- Check BepInEx/LogOutput.log for errors
- Make sure your monitor supports the resolution
- Try using borderless window mode

**Settings overlay won't open:**
- Default key is F10
- Check config file to see if keybind was changed
- Make sure BepInEx is installed correctly

## Building from Source

See `Source/` folder for complete source code.

### Requirements
- .NET SDK 8.0+
- .NET Framework 4.6.2 Developer Pack
- Visual Studio Code (recommended) or Visual Studio

### Build Steps
1. Copy Unity and BepInEx DLLs to `Source/lib/`:
   - From `<Game>/Tailor Simulator_Data/Managed/`:
     - UnityEngine.dll
     - UnityEngine.CoreModule.dll
     - UnityEngine.UI.dll
     - UnityEngine.IMGUIModule.dll
     - UnityEngine.InputLegacyModule.dll
   - From `<Game>/BepInEx/core/`:
     - BepInEx.dll
     - 0Harmony.dll

2. Build:
   ```bash
   cd Source
   dotnet build -c Release
   ```

3. Output: `Source/bin/Release/netstandard2.1/IluUltrawide.dll`

## Credits

- Created by: Iluvatar
- BepInEx: https://github.com/BepInEx/BepInEx
- Harmony: https://github.com/pardeike/Harmony

## License

This project is provided as-is for personal use. Source code is included for educational purposes and modification.

## Changelog

### v1.0.0 (2026-02-15)
- Initial release
- Ultrawide resolution support (21:9, 32:9)
- Custom resolution input
- UI scaling with manual adjustment
- In-game settings overlay
- Black bar removal
- Aspect ratio fitter patches
