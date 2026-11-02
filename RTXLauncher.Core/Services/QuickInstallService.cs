// Services/QuickInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class QuickInstallService
{
	// Quick install puts OpenRemix and its shader API proxy into the 64-bit game
	// (PackageInstallService.OpenRemix*, Proxy*); the binary patches and the
	// fixes package of RTX Remix installs are not part of it.

	// Dependencies on all our other services
	private readonly GarrysModInstallService _installService;
	private readonly GitHubService _githubService;
	private readonly PackageInstallService _packageInstallService;
	private readonly PatchingService _patchingService;
	private readonly InstalledPackagesService _installedPackagesService;

	public QuickInstallService(GarrysModInstallService installService, GitHubService githubService, PackageInstallService packageInstallService, PatchingService patchingService, InstalledPackagesService installedPackagesService)
	{
		_installService = installService;
		_githubService = githubService;
		_packageInstallService = packageInstallService;
		_patchingService = patchingService;
		_installedPackagesService = installedPackagesService;
	}

	public async Task RemoveExistingInstallationAsync(IProgress<InstallProgressReport> progress)
	{
		var installDir = GarrysModUtility.GetThisInstallFolder();
		EnsureSupportedInstallPath(installDir);
		progress.Report(new InstallProgressReport { Message = "Removing existing installation data...", Percentage = 0 });

		await Task.Run(() =>
		{
			if (!Directory.Exists(installDir))
			{
				return;
			}

			if (!GarrysModUtility.UseLocalInstallPath)
			{
				Directory.Delete(installDir, recursive: true);
				return;
			}

			DeleteLocalInstallContents(installDir);
		});

		_installedPackagesService.ClearCache();
		progress.Report(new InstallProgressReport { Message = "Existing installation data removed.", Percentage = 100 });
	}

	public async Task PerformQuickInstallAsync(IProgress<InstallProgressReport> progress, string? manualVanillaPath = null, Func<Task<bool>>? legacyDowngradeCallback = null, LocalZipOverrides? localZipOverrides = null, ReleaseChannel releaseChannel = ReleaseChannel.Stable)
	{
		// Helper to remap progress for sub-tasks
		IProgress<InstallProgressReport> CreateSubProgress(int basePercent, int range)
		{
			return new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport
				{
					Message = report.Message,
					Percentage = basePercent + (int)(report.Percentage * (range / 100.0))
				});
			});
		}

		var channelLabel = releaseChannel == ReleaseChannel.Nightly ? "nightly" : "latest stable";

		// Step 1: Check for existing installation
		progress.Report(new InstallProgressReport { Message = "Checking for existing RTX installation...", Percentage = 5 });
		var installDir = GarrysModUtility.GetThisInstallFolder();
		EnsureSupportedInstallPath(installDir);
		var installType = GarrysModUtility.GetInstallType(installDir);

		if (installType == "unknown")
		{
			// Step 2: Create new installation if needed
			var vanillaDir = GarrysModUtility.GetVanillaInstallFolder(manualVanillaPath);
			if (string.IsNullOrEmpty(vanillaDir)) throw new DirectoryNotFoundException("Could not find vanilla Garry's Mod installation. Please specify the location manually.");

			await _installService.CreateNewGmodInstallAsync(vanillaDir, installDir, CreateSubProgress(10, 20));
			installType = GarrysModUtility.GetInstallType(installDir); // Re-check type
		}

		// Step 2.5: Apply legacy downgrade if requested (must happen after vanilla install, before patches)
		if (legacyDowngradeCallback != null)
		{
			progress.Report(new InstallProgressReport { Message = "Applying legacy build downgrade...", Percentage = 25 });
			bool downgradeCompleted = await legacyDowngradeCallback();
			if (!downgradeCompleted)
			{
				throw new OperationCanceledException("Legacy build downgrade was cancelled by user.");
			}
		}

		// OpenRemix runs in the 64-bit game, through its shader API proxy: the
		// main branch ships the 64-bit binaries beside its 32-bit ones, as the
		// x86-64 branch does.
		if (!GarrysModUtility.Has64BitBinaries(installDir))
		{
			throw new InvalidOperationException(
				"This Garry's Mod installation has no 64-bit binaries (gmod_win64.exe and bin/win64), which OpenRemix needs. " +
				"Update Garry's Mod through Steam and run the setup again.");
		}

		// Step 3: Install the OpenRemix runtime (from local zip or GitHub)
		if (localZipOverrides?.RemixZipPath != null)
		{
			progress.Report(new InstallProgressReport { Message = "Installing OpenRemix from local zip...", Percentage = 30 });
			await _packageInstallService.InstallOpenRemixFromLocalZipAsync(localZipOverrides.RemixZipPath, installDir, CreateSubProgress(35, 40));
			await _installedPackagesService.SetRemixVersionAsync(
				"Local zip",
				Path.GetFileName(localZipOverrides.RemixZipPath),
				Path.GetFileName(localZipOverrides.RemixZipPath));
		}
		else
		{
			progress.Report(new InstallProgressReport { Message = $"Fetching {channelLabel} OpenRemix...", Percentage = 30 });
			var release = await FetchReleaseAsync(PackageInstallService.OpenRemixOwner, PackageInstallService.OpenRemixRepo, releaseChannel);
			await _packageInstallService.InstallOpenRemixPackageAsync(release, installDir, CreateSubProgress(35, 40));
			await _installedPackagesService.SetRemixVersionAsync(
				$"{PackageInstallService.OpenRemixOwner}/{PackageInstallService.OpenRemixRepo}",
				release.TagName,
				release.Name ?? release.TagName);
		}

		// Step 4: Put the shader API proxy in place (from local zip or GitHub)
		if (localZipOverrides?.ProxyZipPath != null)
		{
			progress.Report(new InstallProgressReport { Message = "Installing the shader API proxy from local zip...", Percentage = 75 });
			await _packageInstallService.InstallProxyFromLocalZipAsync(localZipOverrides.ProxyZipPath, installDir, CreateSubProgress(80, 20));
			await _installedPackagesService.SetProxyVersionAsync(
				"Local zip",
				Path.GetFileName(localZipOverrides.ProxyZipPath),
				Path.GetFileName(localZipOverrides.ProxyZipPath));
		}
		else
		{
			progress.Report(new InstallProgressReport { Message = $"Fetching {channelLabel} shader API proxy...", Percentage = 75 });
			var release = await FetchReleaseAsync(PackageInstallService.ProxyOwner, PackageInstallService.ProxyRepo, releaseChannel);
			await _packageInstallService.InstallProxyPackageAsync(release, installDir, CreateSubProgress(80, 20));
			await _installedPackagesService.SetProxyVersionAsync(
				$"{PackageInstallService.ProxyOwner}/{PackageInstallService.ProxyRepo}",
				release.TagName,
				release.Name ?? release.TagName);
		}

		progress.Report(new InstallProgressReport { Message = "Quick Install finished successfully!", Percentage = 100 });
	}

	private async Task<GitHubRelease> FetchReleaseAsync(string owner, string repo, ReleaseChannel channel)
	{
		var releases = await _githubService.FetchReleasesAsync(owner, repo);
		return PackageInstallService.SelectRelease(releases, channel)
			?? throw new Exception(channel == ReleaseChannel.Nightly
				? $"Could not find a nightly release of {owner}/{repo}."
				: $"Could not find any releases of {owner}/{repo}.");
	}

	private static void EnsureSupportedInstallPath(string installDir)
	{
		if (GarrysModUtility.IsPathUnderOneDrive(installDir, out var oneDriveRoot))
		{
			throw new InvalidOperationException(
				$"The selected RTX install folder is inside OneDrive ({oneDriveRoot}). " +
				"Please choose a local folder outside OneDrive, such as C:\\Games\\GModRTX.");
		}
	}

	private static void DeleteLocalInstallContents(string installDir)
	{
		var directoriesToDelete = new[]
		{
			"bin",
			"garrysmod",
			"platform",
			"rtx-remix",
			"sourceengine"
		};

		foreach (var directoryName in directoriesToDelete)
		{
			var directoryPath = Path.Combine(installDir, directoryName);
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, recursive: true);
			}
		}

		var filesToDelete = new[]
		{
			"gmod.exe",
			"hl2.exe",
			"installed_packages.json",
			"patcherlauncher.exe",
			"rtx.conf",
			"steam_appid.txt"
		};

		foreach (var fileName in filesToDelete)
		{
			var filePath = Path.Combine(installDir, fileName);
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		foreach (var backupPath in Directory.EnumerateFiles(installDir, "rtx.conf.backup_*", SearchOption.TopDirectoryOnly))
		{
			File.Delete(backupPath);
		}
	}
}