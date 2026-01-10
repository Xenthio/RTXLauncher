# RTXLauncher.Fazor

This is an experimental version of RTXLauncher using [Fazor](https://github.com/Xenthio/Fazor) - a desktop Blazor/Razor-like framework.

## What is Fazor?

Fazor is a desktop implementation of a Blazor/Razor system for building cross-platform desktop applications using C# and Razor syntax with SCSS styling support. It provides:

- **Razor-Only Development**: Write your entire UI in Razor - no AXAML, no XAML
- **SCSS Styling**: Full SCSS support with variables, nesting, and mixins
- **XGUI Theme System**: Windows-style themes (95, XP, 7, 10, 11, etc.)
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Building

This project requires:
- .NET 10.0 SDK
- The Fazor source code (referenced from /tmp/Fazor during development)

### Build Command

```bash
cd RTXLauncher.Fazor
dotnet build
```

### Run Command

```bash
cd RTXLauncher.Fazor
dotnet run
```

## Project Structure

- `MainWindow.razor` - Main application window with navigation
- `*Page.razor` - Individual page components (Settings, Install, Update, Mods)
- `MainWindow.scss` - Custom styling for the main window
- `Assets/` - Fazor assets including XGUI themes
- `Program.cs` - Application entry point

## Key Differences from RTXLauncher.Avalonia

1. **UI Framework**: Uses Fazor/Razor instead of Avalonia/AXAML
2. **Styling**: Uses SCSS instead of Avalonia styles
3. **Theming**: Uses XGUI themes (Windows 11 style by default)
4. **Component Model**: Uses Razor components instead of XAML controls

## Status

This is an experimental proof-of-concept. The basic window structure is in place, but full functionality from RTXLauncher.Avalonia is not yet implemented.

## Related Links

- [Fazor Repository](https://github.com/Xenthio/Fazor)
- [Fazor NuGet Usage Guide](https://github.com/Xenthio/Fazor/blob/main/docs/NuGet-Usage.md)
- [XGUI Themes Documentation](https://github.com/Xenthio/Fazor/blob/main/Assets/themes/XGUI/README.md)
