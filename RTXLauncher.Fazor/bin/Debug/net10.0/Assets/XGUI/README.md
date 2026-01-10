# XGUI Themes

This directory contains themes ported from [XGUI-3](https://github.com/Xenthio/XGUI-3) (MIT licensed).

## Structure

```
Assets/XGUI/
├── DefaultStyles/        # Complete theme definitions
│   ├── OliveGreen.scss  # Half-Life style theme
│   ├── Computer95.scss  # Windows 95 style
│   ├── ComputerXP.scss  # Windows XP style
│   ├── Computer7.scss   # Windows 7 style
│   ├── Computer11.scss  # Windows 11 style
│   ├── Derma.scss       # Garry's Mod style
│   ├── Vapour.scss      # Steam-like theme
│   └── ...
├── FunctionStyles/       # Base component styles
│   ├── FunctionStyles.scss  # Main import file
│   ├── Window.scss
│   ├── Button.scss
│   ├── Checkbox.scss
│   └── ...
└── Resources/            # Theme assets (icons, images)
```

## Using XGUI Themes

To use an XGUI theme in your component:

```razor
@attribute [StyleSheet("/XGUI/DefaultStyles/OliveGreen.scss")]
```

Or reference it relatively:

```razor
@attribute [StyleSheet("../../XGUI/DefaultStyles/ComputerXP.scss")]
```

## Available Themes

### Classic Themes
- **OliveGreen** - Half-Life 2 / Valve style interface
- **Computer95** - Windows 95 classic look
- **ComputerXP** - Windows XP Luna theme
- **Computer7** - Windows 7 Aero-inspired
- **Computer11** - Windows 11 modern design

### Gaming Themes
- **Derma** - Garry's Mod default UI
- **SboxDark** - s&box dark theme
- **Vapour** - Steam-inspired interface
- **IMGUI** - Dear ImGui style

### Minimal Themes
- **Simple** - Clean, minimal design
- **ThinGrey** - Lightweight grey theme
- **XGUI** - Default XGUI theme

## Theme Structure

Each theme follows this pattern:

1. **Variable Definitions** - Colors, fonts, sizes
2. **Import FunctionStyles** - Base component styles
3. **Import BaseStyles** - Platform-specific refinements

Example from OliveGreen.scss:
```scss
// Define theme variables
$base-colour: rgba(76,88,68,255);
$default-text-colour: #FFFFFF;
$window-title-icon: url("XGUI/Resources/icon_steam.png");

// Import base functionality
@import "/XGUI/FunctionStyles/FunctionStyles.scss";

// Import platform-specific base
@import "/XGUI/DefaultStyles/BaseStyles/VGUI.scss";
```

## Creating Custom Themes

To create your own XGUI-compatible theme:

1. Start with a template theme
2. Override SCSS variables for your colors/fonts
3. Import FunctionStyles and a BaseStyle
4. Optionally override specific component styles

Example:
```scss
// MyCustomTheme.scss
$base-colour: #your-color;
$default-text-colour: #your-text-color;

@import "/XGUI/FunctionStyles/FunctionStyles.scss";
@import "/XGUI/DefaultStyles/BaseStyles/VGUI.scss";
```

## Path Resolution

The SCSS compiler will resolve paths starting with `/` relative to the `Assets/` directory. This matches XGUI-3's path resolution, so themes from XGUI-3 work without modification.

## OS-Decorated Windows

When using OS-decorated windows (native window chrome), the `.osdecorated` class is automatically added to the window panel. This hides theme borders since the OS handles window decoration.

## License

These themes are from XGUI-3, licensed under MIT by Xenthio.
Original source: https://github.com/Xenthio/XGUI-3
