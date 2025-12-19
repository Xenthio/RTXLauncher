using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

public partial class AdvancedInstallViewModel : PageViewModel
{
	[ObservableProperty] private string _vanillaInstallPath = "Error Fetching Path";
	[ObservableProperty] private string _vanillaInstallType = "Error Fetching Install Type";
	[ObservableProperty] private string _rtxInstallPath = "Error Fetching Path";
	[ObservableProperty] private string _rtxInstallType = "Error Fetching Install Type";
	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private string? _manualVanillaPath;
	private readonly IMessenger _messenger;
	private readonly GitHubService _githubService;
	private readonly GarrysModInstallService _garrysModInstallService;
	private readonly PackageInstallService _packageInstallService;
	private readonly GarrysModUpdateService _garrysModUpdateService;
	private readonly PatchingService _patchingService;
	private readonly InstalledPackagesService _installedPackagesService;

	// THE SCALABLE LIST OF PACKAGES
	public ObservableCollection<InstallablePackageViewModel> Packages { get; } = new();

	public AdvancedInstallViewModel(IMessenger messenger,
				GitHubService githubService,
				PackageInstallService packageInstallService,
				PatchingService patchingService,
				GarrysModInstallService installService,
				GarrysModUpdateService updateService,
				InstalledPackagesService installedPackagesService)
	{
		Header = "Advanced Install";

		_messenger = messenger;
		_githubService = githubService;
		_packageInstallService = packageInstallService;
		_patchingService = patchingService;
		_garrysModInstallService = installService;
		_garrysModUpdateService = updateService;
		_installedPackagesService = installedPackagesService;

		// To add a new package, you just add it to this list!
		Packages.Add(new RemixPackageViewModel(_githubService, _packageInstallService, _messenger, _installedPackagesService));
		Packages.Add(new PatcherPackageViewModel(_patchingService, _messenger, _installedPackagesService));
		Packages.Add(new FixesPackageViewModel(_githubService, _packageInstallService, _messenger, _installedPackagesService, _patchingService, () => ManualVanillaPath));

		// Initialize all packages
		_ = InitializePackages();

		RefreshInstallInfo();
	}

	private void RefreshInstallInfo()
	{
		// Refresh the install info

		VanillaInstallPath = GarrysModUtility.GetVanillaInstallFolder(ManualVanillaPath) ?? "Not found";
		VanillaInstallType = GarrysModUtility.GetInstallType(VanillaInstallPath);

		if (VanillaInstallType == "unknown") VanillaInstallType = "Not installed / not found";

		RtxInstallPath = GarrysModUtility.GetThisInstallFolder();
		RtxInstallType = GarrysModUtility.GetInstallType(RtxInstallPath);

		if (RtxInstallType == "unknown")
		{
			RtxInstallType = "There's no install here, create one!";
			//CreateInstallButton.Enabled = true;
			//UpdateInstallButton.Enabled = false;
		}
		else
		{
			//CreateInstallButton.Enabled = false;
			//UpdateInstallButton.Enabled = true;
		}

		// Update visibility of the QuickInstallGroup
		//UpdateQuickInstallGroupVisibility();
	}

	private async Task InitializePackages()
	{
		foreach (var package in Packages)
		{
			await package.InitializeAsync();
		}
	}


	[RelayCommand]
	private async Task UpdateInstall()
	{
		IsBusy = true;
		try
		{
			// Get vanilla and RTX install paths
			var vanillaPath = GarrysModUtility.GetVanillaInstallFolder(ManualVanillaPath);
			var rtxInstallPath = GarrysModUtility.GetThisInstallFolder();

			if (string.IsNullOrEmpty(vanillaPath) || !Directory.Exists(vanillaPath))
			{
				await Utilities.DialogUtility.ShowMessageAsync("Vanilla Install Not Found",
					"Could not find vanilla Garry's Mod installation. Please specify the location manually using the Browse button.");
				return;
			}

			var rtxInstallType = GarrysModUtility.GetInstallType(rtxInstallPath);
			if (rtxInstallType == "unknown")
			{
				await Utilities.DialogUtility.ShowMessageAsync("No RTX Install",
					"There is no RTX installation at this location. Use Quick Install to create one first.");
				return;
			}

			// Check for updates
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
			{ 
				Message = "Checking for updates from vanilla installation...", 
				Percentage = 10 
			}));

			var updates = await Task.Run(() => 
			{
				var allUpdates = new List<FileUpdateInfo>();
				
				// Check root directory (includes gmod.exe, hl2.exe, and bin folder)
				var rootUpdates = _garrysModUpdateService.CheckForUpdates(vanillaPath, rtxInstallPath);
				allUpdates.AddRange(rootUpdates);
				
				// Check garrysmod folder
				var vanillaGmodPath = Path.Combine(vanillaPath, "garrysmod");
				var rtxGmodPath = Path.Combine(rtxInstallPath, "garrysmod");
				var gmodUpdates = _garrysModUpdateService.CheckForUpdates(vanillaGmodPath, rtxGmodPath);
				allUpdates.AddRange(gmodUpdates);
				
				return allUpdates;
			});

			if (updates.Count == 0)
			{
				await Utilities.DialogUtility.ShowMessageAsync("No Updates Found",
					"Your RTX installation is already up to date with the vanilla installation.");
				return;
			}

			// Build summary message
			var newFiles = updates.Count(u => u.IsNew);
			// Check if any bin files or root executable are being updated (these require patch re-application)
			var binFilesUpdated = updates.Any(u => 
				u.RelativePath.StartsWith("bin", StringComparison.OrdinalIgnoreCase) ||
				u.RelativePath.Equals("gmod.exe", StringComparison.OrdinalIgnoreCase) ||
				u.RelativePath.Equals("hl2.exe", StringComparison.OrdinalIgnoreCase));

			// Show custom dialog with scrollable table
			var dialogViewModel = new UpdateConfirmationViewModel(updates, binFilesUpdated);
			var dialog = new Views.UpdateConfirmationWindow
			{
				DataContext = dialogViewModel
			};

			var mainWindow = App.GetMainWindow();
			if (mainWindow == null)
			{
				await Utilities.DialogUtility.ShowMessageAsync("Error",
					"Could not show update dialog: Main window not found.");
				return;
			}

			await dialog.ShowDialog(mainWindow);

			if (!dialog.Result)
			{
				_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
				{ 
					Message = "Update cancelled by user", 
					Percentage = 0 
				}));
				return;
			}

			// Perform the update
			var progressHandler = new Progress<UpdateProgressReport>(report => 
				_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
				{ 
					Message = report.Message, 
					Percentage = report.Percentage 
				})));

			await _garrysModUpdateService.PerformUpdateAsync(updates, progressHandler, rtxInstallPath);

			// If bin files were updated, re-apply patches
			if (binFilesUpdated)
			{
				_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
				{ 
					Message = "Binary files updated. Re-applying patches...", 
					Percentage = 0 
				}));

				// Get the currently installed patches
				var installedPatches = await _installedPackagesService.GetPatchesVersionAsync();
				if (installedPatches != null)
				{
					// Parse the source to extract owner and repo
					// Source format examples: "BlueAmulet/SourceRTXTweaks", "sambow23/SourceRTXTweaks (for gmod-rtx-fixes-2)"
					var sourceBase = installedPatches.Source.Split('(')[0].Trim(); // Remove any suffix like "(for ...)"
					var parts = sourceBase.Split('/');
					
					if (parts.Length == 2 && !string.IsNullOrEmpty(installedPatches.Branch))
					{
						var owner = parts[0];
						var repo = parts[1];
						var branch = installedPatches.Branch; // Use the stored branch, not a hardcoded one
						
						var patchProgressHandler = new Progress<InstallProgressReport>(report => 
							_messenger.Send(new ProgressReportMessage(report)));

						await _patchingService.ApplyPatchesAsync(
							owner, 
							repo, 
							"applypatch.py", 
							rtxInstallPath, 
							patchProgressHandler, 
							branch);

						_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
						{ 
							Message = $"Patches re-applied successfully! (branch: {branch})", 
							Percentage = 100 
						}));
					}
					else
					{
						await Utilities.DialogUtility.ShowMessageAsync("Warning",
							$"Binary files were updated, but could not automatically re-apply patches.\n\n" +
							$"Invalid patch source format: {installedPatches.Source}\n\n" +
							$"Please manually re-apply patches from the Binary Patches section.");
					}
				}
				else
				{
					await Utilities.DialogUtility.ShowMessageAsync("Notice",
						"Binary files were updated. No patches were previously installed, so no patches were applied.\n\n" +
						"If you need patches, please apply them manually from the Binary Patches section.");
				}
			}

			var successMessage = $"Successfully updated {updates.Count} item(s) from vanilla installation.";
			if (binFilesUpdated)
			{
				successMessage += "\n\nBinary files were updated and patches were re-applied.";
			}

			await Utilities.DialogUtility.ShowMessageAsync("Update Complete", successMessage);
		}
		catch (Exception ex)
		{
			await Utilities.DialogUtility.ShowMessageAsync("Update Failed",
				$"An error occurred while updating:\n\n{ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task BrowseVanillaPathAsync()
	{
		var result = await Utilities.DialogUtility.ShowFolderPickerAsync("Select your vanilla Garry's Mod installation folder");
		if (!string.IsNullOrEmpty(result))
		{
			// Validate that it's actually a Garry's Mod installation
			var installType = GarrysModUtility.GetInstallType(result);
			if (installType != "unknown" && installType.StartsWith("gmod"))
			{
				ManualVanillaPath = result;
				RefreshInstallInfo(); // Refresh to show the new path
			}
			else
			{
				await Utilities.DialogUtility.ShowMessageAsync("Invalid Installation", 
					"The selected folder does not appear to be a valid Garry's Mod installation.\n\n" +
					"Please select the folder containing 'garrysmod' and 'gmod.exe' or 'hl2.exe'.");
			}
		}
	}

	[RelayCommand]
	private async Task CreateInstall()
	{
		IsBusy = true;

		// Use the utility to get the paths
		var vanillaPath = GarrysModUtility.GetVanillaInstallFolder(ManualVanillaPath);
		var newInstallPath = GarrysModUtility.GetThisInstallFolder();

		if (string.IsNullOrEmpty(vanillaPath))
		{
			await Utilities.DialogUtility.ShowMessageAsync("Vanilla Install Not Found", 
				"Could not find vanilla Garry's Mod installation. Please specify the location manually using the Browse button.");
			IsBusy = false;
			return;
		}

		// Set up progress reporting
		var progress = new Progress<InstallProgressReport>(report =>
		{
			_messenger.Send(new ProgressReportMessage(report));
		});

		try
		{
			await _garrysModInstallService.CreateNewGmodInstallAsync(vanillaPath, newInstallPath, progress);
			// TODO: Show a "Success!" dialog
		}
		catch (SymlinkFailedException)
		{
			// This is where you handle the specific error.
			// You would show a dialog asking the user if they want to retry as admin.
		}
		catch (Exception)
		{
			// Handle all other installation errors
		}
		finally
		{
			IsBusy = false;
		}
	}
}

// ===================================================================
//      EXAMPLE IMPLEMENTATIONS OF SPECIFIC PACKAGE VIEWMODELS
// ===================================================================

public partial class RemixPackageViewModel : InstallablePackageViewModel
{
	private readonly PackageInstallService _installService;
	private readonly IMessenger _messenger;

	// --- 1. Add your sources dictionary as a private field ---
	private readonly Dictionary<string, (string Owner, string Repo)> _remixSources = new()
	{
		{ "(OFFICIAL) NVIDIAGameWorks/rtx-remix", ("NVIDIAGameWorks", "rtx-remix") },
		{ "sambow23/dxvk-remix-gmod", ("sambow23", "dxvk-remix-gmod") },
	};

	public RemixPackageViewModel(GitHubService githubService, PackageInstallService installService, IMessenger messenger, InstalledPackagesService installedPackagesService)
		: base(githubService, installedPackagesService)
	{
		Title = "NVIDIA RTX Remix";
		_installService = installService;
		_messenger = messenger;
	}

	protected override async Task LoadInstalledVersion()
	{
		if (InstalledPackagesService == null) return;
		var version = await InstalledPackagesService.GetRemixVersionAsync();
		SetInstalledVersionDisplay(version);
	}

	/// <summary>
	/// Override to display release name instead of tag for Remix
	/// </summary>
	protected override void SetInstalledVersionDisplay(InstalledPackageVersion? version)
	{
		if (version != null)
		{
			InstalledVersion = version.ReleaseName; // Use release name instead of version tag
			InstalledSource = version.Source;
			HasInstalledVersion = true;
		}
		else
		{
			InstalledVersion = null;
			InstalledSource = null;
			HasInstalledVersion = false;
		}
	}

	// --- 2. Implement LoadSources to read from the dictionary ---
	protected override Task LoadSources()
	{
		Sources.Clear();
		foreach (var sourceName in _remixSources.Keys)
		{
			Sources.Add(sourceName);
		}
		return Task.CompletedTask;
	}

	// --- 3. Implement LoadReleases to use the selected source ---
	protected override async Task LoadReleases()
	{
		if (GitHubService == null || string.IsNullOrEmpty(SelectedSource) || !_remixSources.TryGetValue(SelectedSource, out var sourceInfo))
		{
			Releases.Clear();
			return;
		}

		IsBusy = true;
		Releases.Clear();
		try
		{
			var releases = await GitHubService.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);
			foreach (var release in releases.OrderByDescending(r => r.PublishedAt))
			{
				Releases.Add(release);
			}
			SelectedRelease = Releases.FirstOrDefault();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Remix] Failed to load releases: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	protected override async Task Install()
	{
		if (SelectedRelease == null) return;

		var installDir = GarrysModUtility.GetThisInstallFolder();
		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
			{ 
				Message = "ERROR: Could not find a valid RTX GMod installation directory.", 
				Percentage = 100 
			}));
			return;
		}

		// Check for existing rtx.conf and prompt for backup
		if (RemixUtility.RtxConfigExists(installDir))
		{
			var shouldBackup = await Utilities.DialogUtility.ShowConfirmationAsync(
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
						Percentage = 5 
					}));
				}
			}
		}

		IsBusy = true;
		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;

		try
		{
			// Call the service with the appropriate configuration
			await _installService.InstallRemixPackageAsync(SelectedRelease, installDir, progress);

			// Save installed version
			if (InstalledPackagesService != null && SelectedSource != null)
			{
				await InstalledPackagesService.SetRemixVersionAsync(
					SelectedSource,
					SelectedRelease.TagName,
					SelectedRelease.Name ?? SelectedRelease.TagName);
				await RefreshInstalledVersionAsync();
			}
		}
		catch (Exception ex)
		{
			progress.Report(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 });
		}
		finally
		{
			IsBusy = false;
		}
	}
}

// In a file like ViewModels/Packages/PatcherPackageViewModel.cs

public partial class PatcherPackageViewModel : InstallablePackageViewModel
{
	private readonly PatchingService _patchingService;
	private readonly IMessenger _messenger;

	private readonly Dictionary<string, (string Owner, string Repo, string FilePath, string Branch)> _patchSources = new()
	{
		{ "BlueAmulet/SourceRTXTweaks", ("BlueAmulet", "SourceRTXTweaks", "applypatch.py", "master") },
		{ "sambow23/SourceRTXTweaks (for gmod-rtx-fixes-2)", ("sambow23", "SourceRTXTweaks", "applypatch.py", "main") },
		{ "sambow23/SourceRTXTweaks (for garrys-mod-rtx-remixed-perf)", ("sambow23", "SourceRTXTweaks", "applypatch.py", "perf") },
    };

	public PatcherPackageViewModel(PatchingService patchingService, IMessenger messenger, InstalledPackagesService installedPackagesService)
		: base(null, installedPackagesService) // It doesn't use GitHubService for releases, so we pass null.
	{
		Title = "Binary Patches";
		ButtonText = "Apply Patches";
		_patchingService = patchingService;
		_messenger = messenger;
	}

	protected override async Task LoadInstalledVersion()
	{
		if (InstalledPackagesService == null) return;
		var version = await InstalledPackagesService.GetPatchesVersionAsync();
		SetInstalledVersionDisplay(version);
	}

	protected override Task LoadSources()
	{
		Sources.Clear();
		foreach (var sourceName in _patchSources.Keys)
		{
			Sources.Add(sourceName);
		}
		return Task.CompletedTask;
	}

	protected override Task LoadReleases()
	{
		Releases.Clear();
		return Task.CompletedTask; // Patches don't have releases.
	}

	protected override async Task Install()
	{
		if (string.IsNullOrEmpty(SelectedSource))
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = "ERROR: No patch source selected." }));
			return;
		}

		IsBusy = true;
		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;

		try
		{
			var installDir = GarrysModUtility.GetThisInstallFolder();
			if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
			{
				throw new DirectoryNotFoundException("Could not find a valid RTX GMod installation directory.");
			}

			var sourceInfo = _patchSources[SelectedSource];
			await _patchingService.ApplyPatchesAsync(sourceInfo.Owner, sourceInfo.Repo, sourceInfo.FilePath, installDir, progress, sourceInfo.Branch);

			// Save installed version
			if (InstalledPackagesService != null)
			{
				await InstalledPackagesService.SetPatchesVersionAsync(SelectedSource, sourceInfo.Branch);
				await RefreshInstalledVersionAsync();
			}
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
}

public partial class FixesPackageViewModel : InstallablePackageViewModel
{
	private readonly PackageInstallService _installService;
	private readonly PatchingService _patchingService;
	private readonly IMessenger _messenger;
	private readonly Func<string?> _getManualVanillaPath;
	
	// --- 1. Add your sources dictionary ---
	private readonly Dictionary<string, (string Owner, string Repo, string InstallType)> _packageSources = new()
	{
		{ "Xenthio/gmod-rtx-fixes-2 (Any)", ("Xenthio", "gmod-rtx-fixes-2", "Any") },
		{ "sambow23/garrys-mod-rtx-remixed-perf (Any)", ("sambow23", "garrys-mod-rtx-remixed-perf", "main") }
	};

	// Mapping of fixes package sources to their required binary patches
	private readonly Dictionary<string, (string PatchSource, string Owner, string Repo, string FilePath, string Branch)> _requiredPatches = new()
	{
		{ "Xenthio/gmod-rtx-fixes-2 (Any)", ("sambow23/SourceRTXTweaks (for gmod-rtx-fixes-2)", "sambow23", "SourceRTXTweaks", "applypatch.py", "main") },
		{ "sambow23/garrys-mod-rtx-remixed-perf (Any)", ("sambow23/SourceRTXTweaks (for garrys-mod-rtx-remixed-perf)", "sambow23", "SourceRTXTweaks", "applypatch.py", "perf") }
	};

	public FixesPackageViewModel(GitHubService githubService, PackageInstallService installService, IMessenger messenger, InstalledPackagesService installedPackagesService, PatchingService patchingService, Func<string?> getManualVanillaPath)
		: base(githubService, installedPackagesService)
	{
		Title = "Fixes Package";
		_installService = installService;
		_messenger = messenger;
		_patchingService = patchingService;
		_getManualVanillaPath = getManualVanillaPath;
	}

	protected override async Task LoadInstalledVersion()
	{
		if (InstalledPackagesService == null) return;
		var version = await InstalledPackagesService.GetFixesVersionAsync();
		SetInstalledVersionDisplay(version);
	}

	// --- 2. Implement LoadSources ---
	protected override Task LoadSources()
	{
		Sources.Clear();
		foreach (var sourceName in _packageSources.Keys)
		{
			Sources.Add(sourceName);
		}
		return Task.CompletedTask;
	}

	// --- 3. Implement LoadReleases ---
	protected override async Task LoadReleases()
	{
		if (GitHubService == null || string.IsNullOrEmpty(SelectedSource) || !_packageSources.TryGetValue(SelectedSource, out var sourceInfo))
		{
			Releases.Clear();
			return;
		}

		IsBusy = true;
		Releases.Clear();
		try
		{
			var releases = await GitHubService.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);
			foreach (var release in releases.OrderByDescending(r => r.PublishedAt))
			{
				Releases.Add(release);
			}
			SelectedRelease = Releases.FirstOrDefault();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Fixes] Failed to load releases: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	protected override async Task Install()
	{
		if (SelectedRelease == null || SelectedSource == null) return;

		var installDir = GarrysModUtility.GetThisInstallFolder();
		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport 
			{ 
				Message = "ERROR: Could not find a valid RTX GMod installation directory.", 
				Percentage = 100 
			}));
			return;
		}

		// Check if we're switching from a different fixes package
		string? previousFixesSource = null;
		string? previousPatchBranch = null;
		bool needsPatches = false;
		bool needsBinaryRestore = false;
		
		if (InstalledPackagesService != null)
		{
			var previousVersion = await InstalledPackagesService.GetFixesVersionAsync();
			var previousPatchesVersion = await InstalledPackagesService.GetPatchesVersionAsync();
			
			if (previousVersion != null)
			{
				previousFixesSource = previousVersion.Source;
				previousPatchBranch = previousPatchesVersion?.Branch;
				
				// Check if we're switching to a different fixes package that needs different patches
				if (previousFixesSource != SelectedSource && _requiredPatches.ContainsKey(SelectedSource))
				{
					needsPatches = true;
					
					// Check if we need to restore binaries (switching to different patch branch)
					if (_requiredPatches.TryGetValue(SelectedSource, out var newPatchInfo) && 
					    !string.IsNullOrEmpty(previousPatchBranch) && 
					    previousPatchBranch != newPatchInfo.Branch)
					{
						needsBinaryRestore = true;
					}
				}
			}
			else if (_requiredPatches.ContainsKey(SelectedSource))
			{
				// First time installing fixes - patches will be needed but no restore needed
				needsPatches = true;
			}
		}

		// Check for existing rtx.conf and prompt for backup
		if (RemixUtility.RtxConfigExists(installDir))
		{
			var shouldBackup = await Utilities.DialogUtility.ShowConfirmationAsync(
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
						Percentage = 5 
					}));
				}
			}
		}

		IsBusy = true;

		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;
		try
		{
			// Install fixes package (takes 0-80%)
			var fixesProgress = new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport 
				{ 
					Message = report.Message, 
					Percentage = (int)(report.Percentage * 0.8) 
				});
			});
			await _installService.InstallStandardPackageAsync(SelectedRelease, installDir, PackageInstallService.DefaultIgnorePatterns, fixesProgress);

			// Save installed version
			if (InstalledPackagesService != null)
			{
				await InstalledPackagesService.SetFixesVersionAsync(
					SelectedSource,
					SelectedRelease.TagName,
					SelectedRelease.Name ?? SelectedRelease.TagName);
				await RefreshInstalledVersionAsync();
			}

			// Auto-apply required patches if needed (takes 80-100%)
			if (needsPatches && _requiredPatches.TryGetValue(SelectedSource, out var patchInfo))
			{
				// Restore original binaries before patching if switching between different patch branches
				if (needsBinaryRestore)
				{
					progress.Report(new InstallProgressReport 
					{ 
						Message = "Restoring original binaries before applying new patches...", 
						Percentage = 80 
					});

					await Task.Run(() =>
					{
						var restoreProgress = new Progress<InstallProgressReport>(report =>
						{
							progress.Report(new InstallProgressReport 
							{ 
								Message = report.Message, 
								Percentage = 80 + (int)(report.Percentage * 0.05) 
							});
						});

						BinaryRestorationUtility.RestoreOriginalBinaries(installDir, restoreProgress, _getManualVanillaPath());
					});
				}

				progress.Report(new InstallProgressReport 
				{ 
					Message = $"Applying required binary patches for {SelectedSource}...", 
					Percentage = needsBinaryRestore ? 85 : 80
				});

				var patchProgress = new Progress<InstallProgressReport>(report =>
				{
					int basePercentage = needsBinaryRestore ? 85 : 80;
					int range = needsBinaryRestore ? 15 : 20;
					progress.Report(new InstallProgressReport 
					{ 
						Message = report.Message, 
						Percentage = basePercentage + (int)(report.Percentage * (range / 100.0)) 
					});
				});

				await _patchingService.ApplyPatchesAsync(
					patchInfo.Owner, 
					patchInfo.Repo, 
					patchInfo.FilePath, 
					installDir, 
					patchProgress, 
					patchInfo.Branch);

				// Save patches version
				if (InstalledPackagesService != null)
				{
					await InstalledPackagesService.SetPatchesVersionAsync(patchInfo.PatchSource, patchInfo.Branch);
				}

				progress.Report(new InstallProgressReport 
				{ 
					Message = "Fixes package and patches installed successfully!", 
					Percentage = 100 
				});
			}
			else
			{
				progress.Report(new InstallProgressReport 
				{ 
					Message = "Fixes package installed successfully!", 
					Percentage = 100 
				});
			}
		}
		catch (Exception ex)
		{
			progress.Report(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 });
		}
		finally
		{
			IsBusy = false;
		}
	}
}