# Global Assets Folder

This folder contains built-in assets (themes, fonts, images) that are shared across all projects in the solution.

## Structure

```
Assets/
├── XGUI/            # XGUI theme system (compatible with XGUI-3)
│   ├── DefaultStyles/    # Complete theme definitions
│   ├── FunctionStyles/   # Base component styles  
│   └── Resources/        # Theme images and icons
├── themes/          # Fazor-specific styles
│   ├── Fazor.Defaults.scss
│   └── PanelInspector.scss
└── fonts/           # Font files (.ttf, .otf)
```

## Usage

Projects can access these global assets by importing the `Assets.props` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Import global assets -->
  <Import Project="..\..\Assets.props" />
  
  <!-- Your project configuration -->
  <PropertyGroup>
    <!-- ... -->
  </PropertyGroup>
</Project>
```

This automatically copies all global assets to the project's output directory.

## XGUI Theme Compatibility

The `/Assets/XGUI/` folder structure matches [XGUI-3](https://github.com/Xenthio/XGUI-3), allowing themes to be used without path modifications:

```razor
@attribute [StyleSheet("/XGUI/DefaultStyles/Computer95.scss")]
```

Image paths in themes like `url("XGUI/Resources/icon.png")` resolve correctly to `/Assets/XGUI/Resources/icon.png`.

## Global vs Project Assets

### Global Assets (`/Assets/`)
- **Purpose**: Built-in, shared resources used across multiple projects
- **Examples**: 
  - XGUI theme system
  - Default Fazor styles
  - Built-in fonts (Roboto, etc.)
  - Common icons and UI elements
- **Location**: Root of solution (`/Assets/`)
- **Usage**: Import via `Assets.props`

### Project Assets (`ProjectName/Assets/`)
- **Purpose**: Project-specific resources
- **Examples**:
  - Custom themes for a specific app
  - App-specific images and icons
  - Project branding assets
- **Location**: Inside project folder (`examples/SimpleDesktopApp/Assets/`)
- **Usage**: Configured in project's `.csproj` file

## How It Works

When `Assets.props` is imported:
1. All files in `/Assets/**/*` are included as Content items
2. Files are copied to build output directory (bin/Debug, bin/Release) under `Assets/`
3. Files are included in publish output and single-file executables
4. The assets remain under `Assets/` subdirectory in output

## File Resolution Priority

When loading stylesheets or images, the framework searches in this order:
1. **Assets subdirectory** - `{BaseDirectory}/Assets/{path}` (primary location)
2. **Direct path** - `{BaseDirectory}/{path}` (backward compatibility)
3. **Legacy paths** - `assets/`, `wwwroot/` subdirectories

This means:
- `[StyleSheet("/XGUI/DefaultStyles/MyTheme.scss")]` resolves to `Assets/XGUI/DefaultStyles/MyTheme.scss`
- `[StyleSheet("/themes/Fazor.Defaults.scss")]` resolves to `Assets/themes/Fazor.Defaults.scss`
- Project-specific assets in `ProjectName/Assets/` override global ones
- Old code referencing direct paths continues to work

## Adding Global Assets

### Adding a Built-in Theme

1. Place theme files in `/Assets/XGUI/` (XGUI-compatible) or `/Assets/themes/`:
   ```
   Assets/
   └── XGUI/
       └── DefaultStyles/
           └── MyTheme.scss
   ```

2. Projects automatically have access via StyleSheet attribute:
   ```csharp
   [StyleSheet("/XGUI/DefaultStyles/MyTheme.scss")]
   ```

### Adding Fonts

1. Place font files in `/Assets/fonts/`:
   ```
   Assets/
   └── fonts/
       ├── Roboto-Regular.ttf
       └── OpenSans-Bold.ttf
   ```

2. Fonts are automatically available to the renderer

### Adding Shared Images

1. Place images in appropriate location:
   ```
   Assets/
   └── XGUI/
       └── Resources/
           └── my-icon.png
   ```

2. Reference in themes:
   ```scss
   background-image: url("XGUI/Resources/my-icon.png");
   ```

## Best Practices

1. **Minimize global assets**: Only include truly shared resources
2. **Version control**: Commit all global assets to repository
3. **Optimize files**: Compress images and minify resources
4. **Document usage**: Add README files for complex asset structures
5. **Naming conventions**: Use lowercase, hyphen-separated names for cross-platform compatibility
6. **XGUI compatibility**: Keep XGUI themes in `/Assets/XGUI/` structure

## Troubleshooting

### Assets not appearing in output
1. Verify `Assets.props` is imported in your `.csproj`
2. Check import path is correct relative to project location
3. Run `dotnet clean` then `dotnet build`

### File path conflicts
If project assets conflict with global assets (same path), the project asset takes precedence during file copy. Consider renaming one to avoid confusion.

### Missing assets at runtime
Ensure you're running from the correct directory where assets were copied, or use absolute paths resolved at runtime.
