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
	private readonly IMessenger _messenger;
	private readonly GitHubService _githubService;
	private readonly GarrysModInstallService _garrysModInstallService;
	private readonly PackageInstallService _packageInstallService;
	private readonly GarrysModUpdateService _garrysModUpdateService;
	private readonly PatchingService _patchingService;

	// THE SCALABLE LIST OF PACKAGES
	public ObservableCollection<InstallablePackageViewModel> Packages { get; } = new();

	public AdvancedInstallViewModel(IMessenger messenger,
				GitHubService githubService,
				PackageInstallService packageInstallService,
				PatchingService patchingService,
				GarrysModInstallService installService,
				GarrysModUpdateService updateService)
	{
		Header = "Advanced Install";

		_messenger = messenger;
		_githubService = githubService;
		_packageInstallService = packageInstallService;
		_patchingService = patchingService;
		_garrysModInstallService = installService;
		_garrysModUpdateService = updateService;

		// To add a new package, you just add it to this list!
		Packages.Add(new RemixPackageViewModel(_githubService, _packageInstallService, _messenger));
		Packages.Add(new PatcherPackageViewModel(_patchingService, _messenger));
		Packages.Add(new FixesPackageViewModel(_githubService, _packageInstallService, _messenger));
		Packages.Add(new OptiScalerPackageViewModel(_githubService, _packageInstallService, _messenger));

		// Initialize all packages
		_ = InitializePackages();

		RefreshInstallInfo();
	}

	private void RefreshInstallInfo()
	{
		// Refresh the install info

		_vanillaInstallPath = GarrysModUtility.GetVanillaInstallFolder();
		_vanillaInstallType = GarrysModUtility.GetInstallType(_vanillaInstallPath);

		if (_vanillaInstallType == "unknown") _vanillaInstallType = "Not installed / not found";

		_rtxInstallPath = GarrysModUtility.GetThisInstallFolder();
		_rtxInstallType = GarrysModUtility.GetInstallType(_rtxInstallPath);

		if (_rtxInstallType == "unknown")
		{
			_rtxInstallType = "There's no install here, create one!";
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
			// TODO: Call your GarrysModUpdateService to show the update dialog
			// For example: await _updateService.ShowUpdateDialogAsync();

			// Simulate work for now
			await Task.Delay(2000);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task CreateInstall()
	{
		IsBusy = true;

		// Use the utility to get the paths
		var vanillaPath = GarrysModUtility.GetVanillaInstallFolder();
		var newInstallPath = GarrysModUtility.GetThisInstallFolder();

		if (string.IsNullOrEmpty(vanillaPath))
		{
			// TODO: Show an error dialog to the user
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
		catch (SymlinkFailedException ex)
		{
			// This is where you handle the specific error.
			// You would show a dialog asking the user if they want to retry as admin.
			//InstallProgressText = $"Error: {ex.Message}";

		}
		catch (Exception ex)
		{
			// Handle all other installation errors
			//InstallProgressText = $"An unexpected error occurred: {ex.Message}";
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

	public RemixPackageViewModel(GitHubService githubService, PackageInstallService installService, IMessenger messenger)
		: base(githubService)
	{
		Title = "NVIDIA RTX Remix";
		_installService = installService;
		_messenger = messenger;
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
		if (string.IsNullOrEmpty(SelectedSource) || !_remixSources.TryGetValue(SelectedSource, out var sourceInfo))
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

		IsBusy = true;
		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;


		try
		{
			var installDir = GarrysModUtility.GetThisInstallFolder();
			if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
			{
				throw new Exception("Could not find a valid RTX GMod installation directory.");
			}

			// Call the service with the appropriate configuration
			await _installService.InstallRemixPackageAsync(SelectedRelease, installDir, progress);
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

	private readonly Dictionary<string, (string Owner, string Repo, string FilePath)> _patchSources = new()
	{
		{ "BlueAmulet/SourceRTXTweaks", ("BlueAmulet", "SourceRTXTweaks", "applypatch.py") },
		{ "sambow23/SourceRTXTweaks", ("sambow23", "SourceRTXTweaks", "applypatch.py") },
        // ... other sources
    };

	public PatcherPackageViewModel(PatchingService patchingService, IMessenger messenger)
		: base(null) // It doesn't use GitHubService for releases, so we pass null.
	{
		Title = "Binary Patches";
		ButtonText = "Apply Patches";
		_patchingService = patchingService;
		_messenger = messenger;
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
			await _patchingService.ApplyPatchesAsync(sourceInfo.Owner, sourceInfo.Repo, sourceInfo.FilePath, installDir, progress);
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
	private readonly IMessenger _messenger;
	// --- 1. Add your sources dictionary ---
	private readonly Dictionary<string, (string Owner, string Repo, string InstallType)> _packageSources = new()
	{
		{ "Xenthio/gmod-rtx-fixes-2 (Any)", ("Xenthio", "gmod-rtx-fixes-2", "Any") },
		{ "Xenthio/RTXFixes (gmod_main)", ("Xenthio", "RTXFixes", "gmod_main") }
	};

	public FixesPackageViewModel(GitHubService githubService, PackageInstallService installService, IMessenger messenger)
		: base(githubService)
	{
		Title = "Fixes Package";
		_installService = installService;
		_messenger = messenger;
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
		if (string.IsNullOrEmpty(SelectedSource) || !_packageSources.TryGetValue(SelectedSource, out var sourceInfo))
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
		if (SelectedRelease == null) return;
		IsBusy = true;

		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;
		try
		{
			var installDir = GarrysModUtility.GetThisInstallFolder();
			await _installService.InstallStandardPackageAsync(SelectedRelease, installDir, PackageInstallService.DefaultIgnorePatterns, progress);
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

public partial class OptiScalerPackageViewModel : InstallablePackageViewModel
{
	private readonly PackageInstallService _installService;
	private readonly IMessenger _messenger;
	// --- 1. Add your sources dictionary for OptiScaler ---
	private readonly Dictionary<string, (string Owner, string Repo)> _optiScalerSources = new()
	{
		{ "sambow23/OptiScaler-Releases", ("sambow23", "OptiScaler-Releases") }
	};

	public OptiScalerPackageViewModel(GitHubService githubService, PackageInstallService installService, IMessenger messenger)
		: base(githubService)
	{
		Title = "AMD Support - OptiScaler";
		_installService = installService;
		_messenger = messenger;
	}


	// --- 2. Implement LoadSources to read from the dictionary ---
	protected override Task LoadSources()
	{
		Sources.Clear();
		foreach (var sourceName in _optiScalerSources.Keys)
		{
			Sources.Add(sourceName);
		}
		return Task.CompletedTask;
	}

	// --- 3. Implement LoadReleases to use the selected source ---
	protected override async Task LoadReleases()
	{
		if (string.IsNullOrEmpty(SelectedSource) || !_optiScalerSources.TryGetValue(SelectedSource, out var sourceInfo))
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
			System.Diagnostics.Debug.WriteLine($"[OptiScaler] Failed to load releases: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	protected override async Task Install()
	{
		if (SelectedRelease == null) return;

		IsBusy = true;

		var progressHandler = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progress = progressHandler;
		try
		{
			var installDir = GarrysModUtility.GetThisInstallFolder();
			if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
			{
				throw new Exception("Could not find a valid RTX GMod installation directory.");
			}

			// Call the special-case installer method for OptiScaler
			await _installService.InstallOptiScalerPackageAsync(SelectedRelease, installDir, progress);
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