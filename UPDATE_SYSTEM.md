# RTXLauncher Unified Update System

This document describes the new unified update system that works with both the Avalonia and WinForms versions of RTXLauncher.

## Overview

The unified update system moves the core update logic into `RTXLauncher.Core.Services.UpdateService`, allowing both the Avalonia and WinForms applications to share the same update functionality while maintaining their platform-specific UI and installation mechanisms.

## Architecture

### Core Components

- **`RTXLauncher.Core.Services.UpdateService`** - Main service containing shared update logic
- **`RTXLauncher.Core.Models.UpdateSource`** - Model representing an update source (release or staging)
- **`RTXLauncher.Core.Utilities.VersionUtility`** - Version comparison and extraction utilities
- **`RTXLauncher.Core.Services.GitHubService`** - GitHub API integration for fetching releases and artifacts

### Platform-Specific Integrations

- **Avalonia**: `AboutViewModel` uses `UpdateService` directly with MVVM data binding
- **WinForms**: `Form1.AboutPage.Updater.cs` provides both existing functionality and new unified methods

## Key Features

### 1. Version Detection
```csharp
string currentVersion = updateService.GetCurrentVersion();
```
Automatically detects current version from assembly attributes, supporting both release versions (`v1.2.3`) and development builds (`dev-{hash}`).

### 2. Update Source Discovery
```csharp
var sources = await updateService.GetAvailableUpdateSourcesAsync();
```
Fetches available updates from multiple sources:
- **GitHub Releases**: Stable and pre-release versions
- **GitHub Actions Artifacts**: Latest development builds from CI

### 3. Update Checking
```csharp
var result = await updateService.CheckForUpdatesAsync();
if (result.UpdateAvailable) {
    // Handle update availability
}
```

### 4. Download with Progress
```csharp
var progress = new Progress<UpdateProgress>(p => {
    // Update UI with progress
});
string downloadPath = await updateService.DownloadUpdateAsync(source, progress);
```

### 5. Smart Executable Detection
The service automatically determines the correct executable to download:
- `RTXLauncher.Avalonia.exe` for Avalonia applications
- `RTXLauncher.WinForms.exe` for WinForms applications
- Falls back to generic `RTXLauncher.exe`

## Release Package Format

GitHub releases contain zip files with both executables:
```
RTXLauncher-v1.2.3.zip
├── RTXLauncher.Avalonia.exe
├── RTXLauncher.WinForms.exe
└── [other shared files]
```

The UpdateService automatically:
1. Downloads the zip file
2. Extracts it to a temporary directory
3. Identifies the correct executable for the current platform
4. Prepares it for installation

## Usage Examples

### Avalonia Integration
```csharp
public class AboutViewModel : PageViewModel
{
    private readonly UpdateService _updateService;
    
    public AboutViewModel(GitHubService gitHubService)
    {
        _updateService = new UpdateService(gitHubService);
        CurrentVersion = _updateService.GetCurrentVersion();
    }
    
    [RelayCommand]
    private async Task CheckForUpdates()
    {
        var result = await _updateService.CheckForUpdatesAsync(forceRefresh: true);
        // Update UI based on result
    }
}
```

### WinForms Integration
```csharp
public partial class Form1 : Form
{
    private static UpdateService _updateService;
    
    public Form1()
    {
        _githubService = new GitHubService();
        _updateService = new UpdateService(_githubService);
        // ... existing initialization
    }
    
    // Option 1: Use existing WinForms updater (no changes needed)
    private async void CheckForLauncherUpdatesButton_Click(object sender, EventArgs e)
    {
        await CheckForUpdatesAsync(true); // Existing method
    }
    
    // Option 2: Use unified UpdateService
    private async Task CheckForUpdatesUsingUnifiedServiceAsync()
    {
        var result = await _updateService.CheckForUpdatesAsync(forceRefresh: true);
        // Handle result
    }
}
```

## Benefits

1. **Shared Logic**: Core update functionality is implemented once and shared
2. **Consistency**: Both platforms behave identically for update operations
3. **Maintainability**: Bug fixes and improvements benefit both platforms
4. **Flexibility**: Each platform can customize UI and installation as needed
5. **Backward Compatibility**: Existing WinForms updater continues to work unchanged

## Error Handling

The UpdateService provides comprehensive error handling:
- Network connectivity issues
- GitHub API rate limiting
- File download failures
- Extraction errors
- Version parsing problems

All methods return structured results with error information that can be displayed to users appropriately for each platform.

## Future Enhancements

- **Update Installation**: Platform-specific installation mechanisms can be added
- **Update Scheduling**: Background update checks
- **Rollback Support**: Ability to revert problematic updates
- **Delta Updates**: Incremental updates for faster downloads
- **Update Notifications**: Push notifications for new releases