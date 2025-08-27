using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
	// --- Services ---
	private readonly QuickInstallService _quickInstallService;
	private readonly IMessenger _messenger;

	// --- The Model ---
	// The ViewModel HOLDS a reference to the pure data Model.
	private readonly SettingsData _settingsData;

	// --- UI State Properties ---
	[ObservableProperty] private bool _isQuickInstallVisible;
	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private string _selectedResolution;

	public List<string> Resolutions { get; }

	public SettingsViewModel(
		SettingsData settingsData, // The loaded settings are passed in
		QuickInstallService quickInstallService,
		IMessenger messenger)
	{
		Header = "Settings";
		_settingsData = settingsData;
		_quickInstallService = quickInstallService;
		_messenger = messenger;

		Resolutions = new List<string> { "Native Resolution" };
		Resolutions.AddRange(LauncherUtility.CommonResolutions);

		if (settingsData.Width == 0 || settingsData.Height == 0)
		{
			SelectedResolution = "Native Resolution";
		}
		else
		{
			if (Resolutions.Contains($"{settingsData.Width}x{settingsData.Height}"))
			{
				SelectedResolution = $"{settingsData.Width}x{settingsData.Height}";
			}
			else
			{
				UseCustomResolution = true;
				SelectedResolution = $"{settingsData.Width}x{settingsData.Height}";
			}
		}

		CheckInstallationStatus();
	}

	partial void OnSelectedResolutionChanged(string value)
	{
		if (value == "Native Resolution")
		{
			Width = 0;
			Height = 0;
		}
		else
		{
			var parts = value.Split('x');
			if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
			{
				Width = width;
				Height = height;
			}
		}
	}

	// ===================================================================
	//      PROXIED PROPERTIES FROM THE SETTINGS DATA MODEL
	// ===================================================================
	// This pattern uses the powerful SetProperty overload from CommunityToolkit.Mvvm
	// to update the underlying model while still raising the correct PropertyChanged event
	// for the ViewModel's property, which updates the UI.

	public bool UseCustomResolution
	{
		get => _settingsData.UseCustomResolution;
		set => SetProperty(_settingsData.UseCustomResolution, value, _settingsData, (model, val) => model.UseCustomResolution = val);
	}

	public int Width
	{
		get => _settingsData.Width;
		set => SetProperty(_settingsData.Width, value, _settingsData, (model, val) => model.Width = val);
	}

	public int Height
	{
		get => _settingsData.Height;
		set => SetProperty(_settingsData.Height, value, _settingsData, (model, val) => model.Height = val);
	}

	public bool LoadWorkshopAddons
	{
		get => _settingsData.LoadWorkshopAddons;
		set => SetProperty(_settingsData.LoadWorkshopAddons, value, _settingsData, (model, val) => model.LoadWorkshopAddons = val);
	}

	public bool DisableChromium
	{
		get => _settingsData.DisableChromium;
		set => SetProperty(_settingsData.DisableChromium, value, _settingsData, (model, val) => model.DisableChromium = val);
	}

	public bool ConsoleEnabled
	{
		get => _settingsData.ConsoleEnabled;
		set => SetProperty(_settingsData.ConsoleEnabled, value, _settingsData, (model, val) => model.ConsoleEnabled = val);
	}

	public bool DeveloperMode
	{
		get => _settingsData.DeveloperMode;
		set => SetProperty(_settingsData.DeveloperMode, value, _settingsData, (model, val) => model.DeveloperMode = val);
	}

	public bool ToolsMode
	{
		get => _settingsData.ToolsMode;
		set => SetProperty(_settingsData.ToolsMode, value, _settingsData, (model, val) => model.ToolsMode = val);
	}

	public int DXLevel
	{
		get => _settingsData.DXLevel;
		set => SetProperty(_settingsData.DXLevel, value, _settingsData, (model, val) => model.DXLevel = val);
	}

	public string CustomLaunchOptions
	{
		get => _settingsData.CustomLaunchOptions;
		set => SetProperty(_settingsData.CustomLaunchOptions, value, _settingsData, (model, val) => model.CustomLaunchOptions = val);
	}

	public string ManuallySpecifiedInstallPath
	{
		get => _settingsData.ManuallySpecifiedInstallPath;
		set => SetProperty(_settingsData.ManuallySpecifiedInstallPath, value, _settingsData, (model, val) => model.ManuallySpecifiedInstallPath = val);
	}
	public bool RtxInstalled => RemixUtility.IsInstalled();

	public bool RtxEnabled
	{
		get => RemixUtility.IsEnabled();
		set
		{
			if (RemixUtility.IsEnabled() != value)
			{
				try
				{
					RemixUtility.SetEnabled(value);
				}
				catch (IOException ex)
				{
					// The ViewModel is responsible for showing the error!
					_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}" }));
				}

				// Notify the UI that the property's value has changed.
				OnPropertyChanged(nameof(RtxEnabled));
			}
		}
	}

	// ===================================================================
	//      QUICK INSTALL LOGIC
	// ===================================================================

	public void CheckInstallationStatus()
	{
		var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
		IsQuickInstallVisible = installType == "unknown";
	}

	[RelayCommand(CanExecute = nameof(CanRunQuickInstall))]
	private async Task RunQuickInstall()
	{
		var confirmed = await DialogUtility.ShowConfirmationAsync(
			"Quick Install Confirmation",
			"This will perform a complete installation with recommended settings.\n\n" +
			"• Create a new RTX installation (if needed)\n" +
			"• Install the latest recommended RTX Remix\n" +
			"• Apply recommended patches\n" +
			"• Install the latest recommended fixes package\n\n" +
			"Do you want to continue?");

		if (!confirmed) return;

		IsBusy = true;
		var progressHandle = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandle;

		try
		{
			await _quickInstallService.PerformQuickInstallAsync(progress);
			CheckInstallationStatus(); // Hide the panel on success
		}
		catch (Exception ex)
		{
			progress.Report(new InstallProgressReport { Message = $"FATAL ERROR: {ex.Message}", Percentage = 100 });
		}
		finally
		{
			IsBusy = false;
		}
	}
	private bool CanRunQuickInstall() => !IsBusy;
}