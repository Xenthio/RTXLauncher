using Avalonia;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Collections.Generic;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class LauncherSettingsViewModel : PageViewModel
{
	private readonly SettingsData _settingsData;
	private readonly SettingsService _settingsService;
	private bool _isRevertingThemeChange = false;

	public LauncherSettingsViewModel(SettingsData settingsData, SettingsService settingsService)
	{
		Header = "Launcher Settings";
		_settingsData = settingsData;
		_settingsService = settingsService;
	}

	public List<string> Themes { get; } = new() { "Fluent", "Simple", "VGUI" };

	public bool CheckForUpdatesOnLaunch
	{
		get => _settingsData.CheckForUpdatesOnLaunch;
		set => SetProperty(_settingsData.CheckForUpdatesOnLaunch, value, _settingsData, (model, val) => model.CheckForUpdatesOnLaunch = val);
	}

	public bool ModDBDebugMode
	{
		get => ModDBModService.DebugMode;
		set
		{
			if (ModDBModService.DebugMode != value)
			{
				ModDBModService.DebugMode = value;
				ModDBModService.Headless = !value; // Show browser when debugging
				OnPropertyChanged();
			}
		}
	}

	public string Theme
	{
		get => _settingsData.Theme;
		set
		{
			if (_isRevertingThemeChange)
			{
				SetProperty(_settingsData.Theme, value, _settingsData, (model, val) => model.Theme = val);
				return;
			}

			var oldValue = _settingsData.Theme;
			if (SetProperty(oldValue, value, _settingsData, (model, val) => model.Theme = val))
			{
				ConfirmAndRestartAsync(oldValue);
			}
		}
	}

	private async void ConfirmAndRestartAsync(string oldTheme)
	{
		const string title = "Restart Required";
		const string message = "Changing the theme requires restarting the application. Please ensure no tasks are running before proceeding.\n\nContinue?";

		var confirmed = await DialogUtility.ShowConfirmationAsync(title, message);

		if (confirmed)
		{
			_settingsService.SaveSettings(_settingsData);
			if (Application.Current is App app)
			{
				app.RestartMainWindow();
			}
		}
		else
		{
			// If the user cancels, revert the theme selection in the UI.
			_isRevertingThemeChange = true;
			Theme = oldTheme;
			_isRevertingThemeChange = false;
		}
	}
}