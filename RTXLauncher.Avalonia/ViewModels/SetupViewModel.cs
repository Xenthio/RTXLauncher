using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SetupViewModel : PageViewModel
{
	// --- Services ---
	private readonly QuickInstallService _quickInstallService;
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
	/// A simple boolean property for the View to bind to, indicating if there are any warnings.
	/// </summary>
	public bool ShowPreflightWarnings => PreflightWarnings.Count > 0;

	/// <summary>
	/// Available release channels (Stable, Nightly).
	/// </summary>
	public ReleaseChannel[] AvailableReleaseChannels { get; } = Enum.GetValues<ReleaseChannel>();

	/// <summary>
	/// The currently selected release channel.
	/// </summary>
	[ObservableProperty]
	private ReleaseChannel _selectedReleaseChannel = ReleaseChannel.Stable;

	// --- Local Zip Override Properties ---

	[ObservableProperty]
	private bool _useLocalRemixZip;

	[ObservableProperty]
	private string? _localRemixZipPath;

	[ObservableProperty]
	private bool _useLocalPatchesZip;

	[ObservableProperty]
	private string? _localPatchesZipPath;

	[ObservableProperty]
	private bool _useLocalFixesZip;

	[ObservableProperty]
	private string? _localFixesZipPath;

	public SetupViewModel(QuickInstallService quickInstallService, IMessenger messenger)
	{
		Header = "Setup";
		_quickInstallService = quickInstallService;
		_messenger = messenger;

		// Run the initial checks to determine which view to show.
		CheckInitialState();
		
		// Check for auto-detected vanilla installation
		CheckVanillaInstallation();
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
	private async Task BrowseLocalRemixZipAsync()
	{
		var zipFilter = new FilePickerFileType("Zip files") { Patterns = new[] { "*.zip" } };
		var result = await DialogUtility.ShowFilePickerAsync("Select RTX Remix zip package", zipFilter);
		if (!string.IsNullOrEmpty(result))
		{
			LocalRemixZipPath = result;
			UseLocalRemixZip = true;
		}
	}

	[RelayCommand]
	private async Task BrowseLocalPatchesZipAsync()
	{
		var zipFilter = new FilePickerFileType("Zip files") { Patterns = new[] { "*.zip" } };
		var result = await DialogUtility.ShowFilePickerAsync("Select binary patches zip (must contain applypatch.py)", zipFilter);
		if (!string.IsNullOrEmpty(result))
		{
			LocalPatchesZipPath = result;
			UseLocalPatchesZip = true;
		}
	}

	[RelayCommand]
	private async Task BrowseLocalFixesZipAsync()
	{
		var zipFilter = new FilePickerFileType("Zip files") { Patterns = new[] { "*.zip" } };
		var result = await DialogUtility.ShowFilePickerAsync("Select fixes package zip", zipFilter);
		if (!string.IsNullOrEmpty(result))
		{
			LocalFixesZipPath = result;
			UseLocalFixesZip = true;
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
		await RunInstallCoreAsync(SelectedReleaseChannel, wipeExistingInstall: false, promptForConfigBackup: true);
	}

	[RelayCommand(CanExecute = nameof(CanRunInstall))]
	private async Task ReRunQuickInstallAsync()
	{
		var confirmed = await DialogUtility.ShowConfirmationAsync(
			"Reinstall",
			"This will delete the current RTX installation data and reinstall the game.\n\nDo you want to continue?");

		if (!confirmed)
		{
			return;
		}

		await BackupRtxConfigIfRequestedAsync();

		var useNightly = await DialogUtility.ShowBinaryChoiceAsync(
			"Choose Release Channel",
			"Which branch do you want to use for this reinstall?",
			"Nightly",
			"Stable");

		if (useNightly == null)
		{
			return;
		}

		var releaseChannel = useNightly.Value ? ReleaseChannel.Nightly : ReleaseChannel.Stable;
		SelectedReleaseChannel = releaseChannel;

		await RunInstallCoreAsync(releaseChannel, wipeExistingInstall: true, promptForConfigBackup: false);
	}

	private async Task RunInstallCoreAsync(ReleaseChannel releaseChannel, bool wipeExistingInstall, bool promptForConfigBackup)
	{
		if (promptForConfigBackup)
		{
			await BackupRtxConfigIfRequestedAsync();
		}

		IsBusy = true;
		// Hide both panels; progress is shown globally in the main window's progress bar.
		IsWelcomeVisible = false;
		IsCompletedVisible = false;

		var _progressHandle = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progressHandle = _progressHandle;

		// Check the vanilla installation type to see if user has 32-bit or 64-bit
		var vanillaPath = GarrysModUtility.GetVanillaInstallFolder(ManualVanillaPath);
		var vanillaInstallType = GarrysModUtility.GetInstallType(vanillaPath);

		// Prompt 32-bit users to upgrade to 64-bit for better performance and features
		if (vanillaInstallType == "gmod_i386" || vanillaInstallType == "gmod_main")
		{
			var upgrade = await DialogUtility.ShowConfirmationAsync(
				"Switch to x64?",
				"You are currently using the 32-bit version of Garry's Mod.\n\n" +
				"The x64 version offers better performance and support for Remix API Features.\n" +
				"\n" +
				"To switch, you need to:\n" +
				"1. Open Steam\n" +
				"2. Right-click Garry's Mod → Properties → Betas\n" +
				"3. Select the 'x86-64' branch.\n" +
				"4. Restart the launcher after the update completes\n\n" +
				"Would you like to cancel this installation and switch to x64 now?");

			if (upgrade)
			{
				// User wants to switch to 64-bit - cancel installation and show instructions
				progressHandle.Report(new InstallProgressReport
				{
					Message = "Installation cancelled. Please switch to 64-bit in Steam and restart the launcher.",
					Percentage = 0
				});
				IsWelcomeVisible = true;
				IsBusy = false;
				return;
			}
			// User declined - continue with 32-bit installation
		}

		try
		{
			if (wipeExistingInstall)
			{
				await _quickInstallService.RemoveExistingInstallationAsync(progressHandle);
			}

			// No automatic downgrade prompt - users can manually downgrade via Advanced Install if needed
			// Build local zip overrides if any checkboxes are enabled
			LocalZipOverrides? localZipOverrides = null;
			if (UseLocalRemixZip || UseLocalPatchesZip || UseLocalFixesZip)
			{
				localZipOverrides = new LocalZipOverrides
				{
					RemixZipPath = UseLocalRemixZip ? LocalRemixZipPath : null,
					PatchesZipPath = UseLocalPatchesZip ? LocalPatchesZipPath : null,
					FixesZipPath = UseLocalFixesZip ? LocalFixesZipPath : null
				};
			}

			await _quickInstallService.PerformQuickInstallAsync(progressHandle, manualVanillaPath: ManualVanillaPath, legacyDowngradeCallback: null, localZipOverrides: localZipOverrides, releaseChannel: releaseChannel);
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

	private async Task BackupRtxConfigIfRequestedAsync()
	{
		var installDir = GarrysModUtility.GetThisInstallFolder();
		if (!RemixUtility.RtxConfigExists(installDir))
		{
			return;
		}

		var shouldBackup = await DialogUtility.ShowConfirmationAsync(
			"RTX Config Found",
			"An existing rtx.conf file was detected. Would you like to back it up before installing?\n\n" +
			"The backup will be saved as rtx.conf.backup_[timestamp]");

		if (!shouldBackup)
		{
			return;
		}

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