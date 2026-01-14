using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SetupViewModel : PageViewModel
{
	// --- Services ---
	private readonly QuickInstallService _quickInstallService;
	private readonly DepotDowngradeService _depotDowngradeService;
	private readonly IMessenger _messenger;

	// --- UI State Properties ---
	[ObservableProperty]
	private bool _isWelcomeVisible;

	[ObservableProperty]
	private bool _isCompletedVisible;

	[ObservableProperty]
	private bool _isBusy;

	/// <summary>
	/// Manual path to vanilla Garry's Mod installation, if specified by user.
	/// </summary>
	[ObservableProperty]
	private string? _manualVanillaPath;

	/// <summary>
	/// Auto-detected vanilla installation path for display purposes.
	/// </summary>
	[ObservableProperty]
	private string _autoDetectedPath = "Checking...";

	/// <summary>
	/// A collection of warnings to show the user before they install.
	/// </summary>
	public ObservableCollection<string> PreflightWarnings { get; } = new();

	/// <summary>
	/// Available fixes packages for the user to choose from.
	/// </summary>
	public List<FixesPackageInfo> AvailableFixesPackages { get; }

	/// <summary>
	/// The currently selected fixes package.
	/// </summary>
	[ObservableProperty]
	private FixesPackageInfo _selectedFixesPackage;

	/// <summary>
	/// A simple boolean property for the View to bind to, indicating if there are any warnings.
	/// </summary>
	public bool ShowPreflightWarnings => PreflightWarnings.Count > 0;

	/// <summary>
	/// Indicates whether the selected fixes package requires the legacy x86-64 build.
	/// </summary>
	[ObservableProperty]
	private bool _requiresLegacyDowngrade;

	/// <summary>
	/// Indicates whether the user has already downgraded or skipped the downgrade.
	/// </summary>
	[ObservableProperty]
	private bool _hasHandledDowngrade;

	public SetupViewModel(QuickInstallService quickInstallService, DepotDowngradeService depotDowngradeService, IMessenger messenger)
	{
		Header = "Setup";
		_quickInstallService = quickInstallService;
		_depotDowngradeService = depotDowngradeService;
		_messenger = messenger;

		// Initialize fixes packages
		AvailableFixesPackages = QuickInstallService.GetAvailableFixesPackages();
		_selectedFixesPackage = AvailableFixesPackages.First(p => p.Option == FixesPackageOption.Standard); // Default to Standard

		// Run the initial checks to determine which view to show.
		CheckInitialState();
		
		// Check for auto-detected vanilla installation
		CheckVanillaInstallation();

		// Manually trigger the package changed handler for initial selection
		OnSelectedFixesPackageChanged(_selectedFixesPackage);
	}

	/// <summary>
	/// Checks the current environment and installation status to decide what to show the user.
	/// </summary>
	private void CheckInitialState()
	{
		// First, check if an installation already exists.
		var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
		if (installType != "unknown")
		{
			IsCompletedVisible = true;
			IsWelcomeVisible = false;
			return; // Nothing more to do.
		}

		// If no installation exists, show the welcome screen.
		IsWelcomeVisible = true;
		IsCompletedVisible = false;

		// Now, perform pre-flight checks for potential issues.
		PreflightWarnings.Clear();
		var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

		// Check if running from Downloads folder.
		if (currentDirectory.Contains(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), StringComparison.OrdinalIgnoreCase))
		{
			PreflightWarnings.Add("Warning: It looks like you are running the launcher from your Downloads folder. It is strongly recommended to move it to a dedicated folder (e.g., C:\\Games\\GModRTX) before installing.");
		}

		// A placeholder for the symlink check. A real check is complex and OS-dependent.
		// For now, we can add a general warning.
		// PreflightWarnings.Add("Note: If installation fails, you may need to run this launcher as an Administrator to allow it to create file links (symlinks).");

		OnPropertyChanged(nameof(ShowPreflightWarnings));
	}

	/// <summary>
	/// Checks if vanilla Garry's Mod can be auto-detected.
	/// </summary>
	private void CheckVanillaInstallation()
	{
		var vanillaPath = GarrysModUtility.GetVanillaInstallFolder();
		if (!string.IsNullOrEmpty(vanillaPath))
		{
			AutoDetectedPath = vanillaPath;
		}
		else
		{
			AutoDetectedPath = "Not found - please specify manually";
			// Add a warning if we can't find it
			PreflightWarnings.Insert(0, "Warning: Could not auto-detect vanilla Garry's Mod installation. Please specify the location manually using the button below.");
			OnPropertyChanged(nameof(ShowPreflightWarnings));
		}
	}

	[RelayCommand]
	private async Task BrowseVanillaPathAsync()
	{
		var result = await DialogUtility.ShowFolderPickerAsync("Select your vanilla Garry's Mod installation folder");
		if (!string.IsNullOrEmpty(result))
		{
			// Validate that it's actually a Garry's Mod installation
			var installType = GarrysModUtility.GetInstallType(result);
			if (installType != "unknown" && installType.StartsWith("gmod"))
			{
				ManualVanillaPath = result;
				// Remove the auto-detection warning if it exists
				var warning = PreflightWarnings.FirstOrDefault(w => w.Contains("Could not auto-detect"));
				if (warning != null)
				{
					PreflightWarnings.Remove(warning);
					OnPropertyChanged(nameof(ShowPreflightWarnings));
				}
			}
			else
			{
				await DialogUtility.ShowMessageAsync("Invalid Installation", 
					"The selected folder does not appear to be a valid Garry's Mod installation.\n\n" +
					"Please select the folder containing 'garrysmod' and 'gmod.exe' or 'hl2.exe'.");
			}
		}
	}

	[RelayCommand(CanExecute = nameof(CanRunInstall))]
	private async Task RunInstallAsync()
	{
		var installDir = GarrysModUtility.GetThisInstallFolder();

		// Check for existing rtx.conf and prompt for backup before starting install
		if (RemixUtility.RtxConfigExists(installDir))
		{
			var shouldBackup = await DialogUtility.ShowConfirmationAsync(
				"RTX Config Found",
				"An existing rtx.conf file was detected. Would you like to back it up before installing?\n\n" +
				"The backup will be saved as rtx.conf.backup_[timestamp]");

			if (shouldBackup)
			{
				var backupPath = RemixUtility.BackupRtxConfig(installDir);
				if (backupPath != null)
				{
					_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
					{ 
						Message = $"Backed up rtx.conf to {Path.GetFileName(backupPath)}", 
						Percentage = 0 
					}));
				}
			}
		}

		IsBusy = true;
		// Hide both panels; progress is shown globally in the main window's progress bar.
		IsWelcomeVisible = false;
		IsCompletedVisible = false;

		var _progressHandle = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progressHandle = _progressHandle;

		// Create downgrade callback if package requires legacy build
		Func<Task<bool>>? downgradeCallback = null;
		if (SelectedFixesPackage.RequiresLegacyBuild && !HasHandledDowngrade)
		{
			downgradeCallback = async () =>
			{
				var result = await DialogUtility.ShowConfirmationAsync(
					"Downgrade?",
					"The selected fixes package works best with the legacy Garry's Mod x86-64 build (May 1, 2025).\n\n" +
					"The downgrade will be applied to your RTX installation after the vanilla files are copied.\n\n" +
					"You will need to provide your Steam credentials.\n\n" +
					"This can be done later in the Advanced Install tab.\n\n" +
					"Would you like to downgrade now?");

				if (result)
				{
					// User wants to downgrade - run the downgrade process
					await DowngradeLegacyBuildAsync();
					// If downgrade failed, HasHandledDowngrade will be false
					// But we still return true to continue installation
				}
				else
				{
					// User doesn't want to downgrade - mark as handled and continue
					HasHandledDowngrade = true;
				}

				// Always return true to continue installation (with or without downgrade)
				return true;
			};
		}

		try
		{
			await _quickInstallService.PerformQuickInstallAsync(progressHandle, SelectedFixesPackage.Option, ManualVanillaPath, downgradeCallback);
			// After installation, re-run the check. It should now show the 'Completed' view.
			CheckInitialState();
			// Notify that packages were installed so Advanced Install tab can refresh
			_messenger.Send(new PackagesUpdatedMessage());
		}
		catch (OperationCanceledException)
		{
			progressHandle.Report(new InstallProgressReport { Message = "Installation cancelled by user.", Percentage = 100 });
			// If it fails, show the welcome screen again so they can retry.
			IsWelcomeVisible = true;
		}
		catch (Exception ex)
		{
			progressHandle.Report(new InstallProgressReport { Message = $"FATAL ERROR: {ex.Message}", Percentage = 100 });
			// If it fails, show the welcome screen again so they can retry.
			IsWelcomeVisible = true;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private bool CanRunInstall() => !IsBusy;

	/// <summary>
	/// Called when the selected fixes package changes to validate compatibility.
	/// </summary>
	partial void OnSelectedFixesPackageChanged(FixesPackageInfo value)
	{
		if (value == null) return;

		// Clear existing warnings related to package compatibility
		var compatWarnings = PreflightWarnings.Where(w => w.Contains("Performance") || w.Contains("64-bit") || w.Contains("legacy") || w.Contains("downgrade")).ToList();
		foreach (var warning in compatWarnings)
		{
			PreflightWarnings.Remove(warning);
		}

		// If the selected package requires x64, add a warning if we can detect it's x32
		if (value.RequiresX64)
		{
			var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
			if (installType != "unknown" && installType != "gmod_x86-64")
			{
				PreflightWarnings.Add($"Warning: The '{value.DisplayName}' package requires a 64-bit installation. Your current installation appears to be 32-bit. The installation will fail if you proceed with this option.");
			}
			else if (installType == "unknown")
			{
				// We don't know yet, but we should still inform the user
				PreflightWarnings.Add($"Note: The '{value.DisplayName}' package requires a 64-bit Garry's Mod installation. Make sure your Steam copy of Garry's Mod is the 64-bit version.");
			}
		}

		OnPropertyChanged(nameof(ShowPreflightWarnings));
	}

	[RelayCommand]
	private async Task DowngradeLegacyBuildAsync()
	{
		// Show credentials dialog
		var credentialsViewModel = new SteamCredentialsViewModel
		{
			ManifestId = SelectedFixesPackage.LegacyManifestId
		};
		var credentialsDialog = new Views.SteamCredentialsWindow
		{
			DataContext = credentialsViewModel
		};

		var mainWindow = App.GetMainWindow();
		if (mainWindow == null) return;

		await credentialsDialog.ShowDialog(mainWindow);

		if (!credentialsDialog.Result) return;

		IsBusy = true;
		var _progressHandle = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progressHandle = _progressHandle;

		try
		{
			var rtxInstallPath = GarrysModUtility.GetThisInstallFolder();

			var authUi = new Services.DepotDownloaderAuthUi(_messenger);
			var downloadRequest = new DepotDownloadRequest
			{
				ManifestId = credentialsViewModel.ManifestId,
				Username = credentialsViewModel.Username,
				Password = credentialsViewModel.Password,
				UseQrCode = credentialsViewModel.UseQrCode,
				RememberPassword = credentialsViewModel.RememberPassword,
				SkipAppConfirmation = credentialsViewModel.SkipAppConfirmation
			};

			progressHandle.Report(new InstallProgressReport
			{
				Message = "Downloading legacy depot...",
				Percentage = 0
			});

			string depotPath = await _depotDowngradeService.DownloadLegacyDepotAsync(
				downloadRequest,
				progressHandle,
				authUi);

			progressHandle.Report(new InstallProgressReport
			{
				Message = "Applying depot to RTX installation...",
				Percentage = 90
			});

			await _depotDowngradeService.ApplyDepotToInstallationAsync(
				depotPath,
				rtxInstallPath,
				progressHandle);

			HasHandledDowngrade = true;
			RequiresLegacyDowngrade = false;

			// Remove the downgrade warning
			var downgradeWarning = PreflightWarnings.FirstOrDefault(w => w.Contains("legacy x86-64 beta build"));
			if (downgradeWarning != null)
			{
				PreflightWarnings.Remove(downgradeWarning);
			}
			PreflightWarnings.Add("✓ Legacy build has been applied. Installation will continue automatically.");
			OnPropertyChanged(nameof(ShowPreflightWarnings));

			progressHandle.Report(new InstallProgressReport
			{
				Message = "Downgrade completed successfully. Continuing with installation...",
				Percentage = 100
			});
		}
		catch (Exception ex)
		{
			progressHandle.Report(new InstallProgressReport
			{
				Message = $"Downgrade failed: {ex.Message}",
				Percentage = 100
			});

			await DialogUtility.ShowMessageAsync("Downgrade Failed",
				$"Failed to downgrade the game:\n\n{ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

}