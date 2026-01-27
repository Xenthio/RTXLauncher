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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
	// --- Services ---
	private readonly IMessenger _messenger;

	// --- The Model ---
	// The ViewModel HOLDS a reference to the pure data Model.
	private readonly SettingsData _settingsData;

	// --- UI State Properties ---
	[ObservableProperty] private string _selectedResolution;
	[ObservableProperty] private List<(string Label, string Path)> _availableProtonBuilds = new();
	[ObservableProperty] private List<string> _protonBuildLabels = new();
	[ObservableProperty] private string _selectedProtonBuild = "";
	[ObservableProperty] private bool _isCustomProtonPathVisible = false;

	public List<string> Resolutions { get; }
	[ObservableProperty] private List<string> _vulkanDriverOptions = new();
	
	// Platform detection
	public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

	public SettingsViewModel(
		SettingsData settingsData, // The loaded settings are passed in
		IMessenger messenger)
	{
		Header = "Settings";
		_settingsData = settingsData;
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
		
		// Initialize Linux-specific settings if on Linux
		if (IsLinux)
		{
			LoadProtonBuilds();
			LoadVulkanDrivers();
		}
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
	
	// ===================================================================
	//      LINUX-SPECIFIC SETTINGS (Only visible on Linux)
	// ===================================================================
	
	public string LinuxProtonPath
	{
		get => _settingsData.LinuxProtonPath;
		set => SetProperty(_settingsData.LinuxProtonPath, value, _settingsData, (model, val) => model.LinuxProtonPath = val);
	}
	
	public string LinuxSteamRootOverride
	{
		get => _settingsData.LinuxSteamRootOverride;
		set => SetProperty(_settingsData.LinuxSteamRootOverride, value, _settingsData, (model, val) => model.LinuxSteamRootOverride = val);
	}
	
	public bool LinuxEnableProtonLog
	{
		get => _settingsData.LinuxEnableProtonLog;
		set => SetProperty(_settingsData.LinuxEnableProtonLog, value, _settingsData, (model, val) => model.LinuxEnableProtonLog = val);
	}
	
	public string LinuxSelectedProtonLabel
	{
		get => _settingsData.LinuxSelectedProtonLabel;
		set => SetProperty(_settingsData.LinuxSelectedProtonLabel, value, _settingsData, (model, val) => model.LinuxSelectedProtonLabel = val);
	}
	
	public string LinuxVulkanDriver
	{
		get => _settingsData.LinuxVulkanDriver;
		set => SetProperty(_settingsData.LinuxVulkanDriver, value, _settingsData, (model, val) => model.LinuxVulkanDriver = val);
	}
	
	public string LinuxCustomEnvironmentVariables
	{
		get => _settingsData.LinuxCustomEnvironmentVariables;
		set => SetProperty(_settingsData.LinuxCustomEnvironmentVariables, value, _settingsData, (model, val) => model.LinuxCustomEnvironmentVariables = val);
	}
	
	public string LinuxLaunchCommandPrefix
	{
		get => _settingsData.LinuxLaunchCommandPrefix;
		set => SetProperty(_settingsData.LinuxLaunchCommandPrefix, value, _settingsData, (model, val) => model.LinuxLaunchCommandPrefix = val);
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
	//      LINUX PROTON MANAGEMENT
	// ===================================================================
	
	private void LoadProtonBuilds()
	{
		try
		{
			AvailableProtonBuilds = LauncherUtility.ListProtonBuilds(_settingsData);
			ProtonBuildLabels = AvailableProtonBuilds.Select(p => p.Label).ToList();
			
			// Add "Custom" option at the end
			ProtonBuildLabels.Add("Custom");
			
			// Set the selected build based on saved label or first available
			if (!string.IsNullOrEmpty(LinuxSelectedProtonLabel))
			{
				if (LinuxSelectedProtonLabel == "Custom" || !string.IsNullOrEmpty(LinuxProtonPath))
				{
					SelectedProtonBuild = "Custom";
					IsCustomProtonPathVisible = true;
					return;
				}
				
				var matching = AvailableProtonBuilds.FirstOrDefault(p => p.Label == LinuxSelectedProtonLabel);
				if (matching != default)
				{
					SelectedProtonBuild = matching.Label;
					IsCustomProtonPathVisible = false;
					return;
				}
			}
			
			// Default to first available Proton build
			if (AvailableProtonBuilds.Count > 0)
			{
				SelectedProtonBuild = AvailableProtonBuilds[0].Label;
				LinuxSelectedProtonLabel = SelectedProtonBuild;
				IsCustomProtonPathVisible = false;
			}
		}
		catch (Exception ex)
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
			{ 
				Message = $"Warning: Could not load Proton builds: {ex.Message}" 
			}));
		}
	}
	
	partial void OnSelectedProtonBuildChanged(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			LinuxSelectedProtonLabel = value;
			
			// Show/hide custom path based on selection
			IsCustomProtonPathVisible = (value == "Custom");
			
			// If switching away from custom, clear the custom path
			if (value != "Custom" && !string.IsNullOrEmpty(LinuxProtonPath))
			{
				LinuxProtonPath = "";
			}
		}
	}
	
	[RelayCommand]
	private void RefreshProtonBuilds()
	{
		if (IsLinux)
		{
			LoadProtonBuilds();
			LoadVulkanDrivers();
		}
	}
	
	[RelayCommand]
	private void ClearProtonPath()
	{
		LinuxProtonPath = "";
		// Switch back to first available Proton build
		if (AvailableProtonBuilds.Count > 0)
		{
			SelectedProtonBuild = AvailableProtonBuilds[0].Label;
		}
		_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
		{ 
			Message = "Switched back to auto-detected Proton version." 
		}));
	}
	
	private void LoadVulkanDrivers()
	{
		var availableDrivers = new List<string> { "Auto" };
		
		try
		{
			// Check for AMD drivers
			if (File.Exists("/usr/share/vulkan/icd.d/amd_icd64.json"))
			{
				availableDrivers.Add("AMDVLK");
			}
			if (File.Exists("/usr/share/vulkan/icd.d/radeon_icd.x86_64.json"))
			{
				availableDrivers.Add("RADV");
			}
			
			// Check for Intel driver
			if (File.Exists("/usr/share/vulkan/icd.d/intel_icd.x86_64.json"))
			{
				availableDrivers.Add("Intel ANV");
			}
			
			// Check for NVIDIA driver
			if (File.Exists("/usr/share/vulkan/icd.d/nvidia_icd.json"))
			{
				availableDrivers.Add("NVIDIA");
			}
			
			// Also check in /usr/local/share/vulkan/icd.d/ for custom installations
			var localIcdDir = "/usr/local/share/vulkan/icd.d/";
			if (Directory.Exists(localIcdDir))
			{
				if (File.Exists(Path.Combine(localIcdDir, "amd_icd64.json")) && !availableDrivers.Contains("AMDVLK"))
				{
					availableDrivers.Add("AMDVLK");
				}
				if (File.Exists(Path.Combine(localIcdDir, "radeon_icd.x86_64.json")) && !availableDrivers.Contains("RADV"))
				{
					availableDrivers.Add("RADV");
				}
				if (File.Exists(Path.Combine(localIcdDir, "intel_icd.x86_64.json")) && !availableDrivers.Contains("Intel ANV"))
				{
					availableDrivers.Add("Intel ANV");
				}
				if (File.Exists(Path.Combine(localIcdDir, "nvidia_icd.json")) && !availableDrivers.Contains("NVIDIA"))
				{
					availableDrivers.Add("NVIDIA");
				}
			}
		}
		catch (Exception ex)
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
			{ 
				Message = $"Warning: Could not detect Vulkan drivers: {ex.Message}" 
			}));
		}
		
		VulkanDriverOptions = availableDrivers;
		
		// Ensure current selection is valid, default to Auto if not
		if (!VulkanDriverOptions.Contains(LinuxVulkanDriver))
		{
			LinuxVulkanDriver = "Auto";
		}
	}
}