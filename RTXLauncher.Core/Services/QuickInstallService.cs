// Services/QuickInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class QuickInstallService
{
	private const string FixesDisplayName = "garrys-mod-rtx-remixed";
	private const string FixesOwner = "Xenthio";
	private const string FixesRepo = "garrys-mod-rtx-remixed";
	private const string PatchOwnerX64 = "sambow23";
	private const string PatchRepoX64 = "SourceRTXTweaks";
	private const string PatchBranchX64 = "main";
	private const string PatchOwnerX86 = "BlueAmulet";
	private const string PatchRepoX86 = "SourceRTXTweaks";
	private const string PatchBranchX86 = "master";
	private const string PatchFile = "applypatch.py";

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

		bool isX64 = installType == "gmod_x86-64";

		// Step 3: Install RTX Remix (from local zip or GitHub)
		if (localZipOverrides?.RemixZipPath != null)
		{
			progress.Report(new InstallProgressReport { Message = "Installing RTX Remix from local zip...", Percentage = 30 });
			await _packageInstallService.InstallRemixFromLocalZipAsync(localZipOverrides.RemixZipPath, installDir, CreateSubProgress(35, 25));

			await _installedPackagesService.SetRemixVersionAsync(
				"Local zip",
				Path.GetFileName(localZipOverrides.RemixZipPath),
				Path.GetFileName(localZipOverrides.RemixZipPath));
		}
		else
		{
			progress.Report(new InstallProgressReport { Message = $"Fetching {channelLabel} RTX Remix...", Percentage = 30 });
			var (remixOwner, remixRepo) = ("sambow23", "dxvk-remix-gmod"); // From constant
			var remixReleases = await _githubService.FetchReleasesAsync(remixOwner, remixRepo);
			var latestRemix = releaseChannel == ReleaseChannel.Nightly
				? remixReleases.FirstOrDefault(r => r.TagName == "nightly")
					?? throw new Exception("Could not find a nightly RTX Remix release.")
				: remixReleases
					.Where(r => !r.Prerelease || r.TagName != "nightly")
					.OrderByDescending(r => r.PublishedAt)
					.FirstOrDefault()
					?? throw new Exception("Could not find any stable RTX Remix releases.");

			await _packageInstallService.InstallRemixPackageAsync(latestRemix, installDir, CreateSubProgress(35, 25));

			// Save Remix version info
			await _installedPackagesService.SetRemixVersionAsync(
				$"{remixOwner}/{remixRepo}",
				latestRemix.TagName,
				latestRemix.Name ?? latestRemix.TagName);
		}

		// Step 4: Apply patches (from local zip or GitHub)
		progress.Report(new InstallProgressReport { Message = $"Applying {FixesDisplayName} patches...", Percentage = 60 });

		if (localZipOverrides?.PatchesZipPath != null)
		{
			progress.Report(new InstallProgressReport { Message = "Applying binary patches from local zip...", Percentage = 60 });
			await _patchingService.ApplyPatchesFromLocalZipAsync(localZipOverrides.PatchesZipPath, installDir, CreateSubProgress(65, 15));

			await _installedPackagesService.SetPatchesVersionAsync(
				"Local zip",
				Path.GetFileName(localZipOverrides.PatchesZipPath));
		}
		else
		{
			string patchOwner, patchRepo, patchFile, patchBranch;
			
			if (isX64)
			{
				patchOwner = PatchOwnerX64;
				patchRepo = PatchRepoX64;
				patchFile = PatchFile;
				patchBranch = PatchBranchX64;
			}
			else
			{
				patchOwner = PatchOwnerX86;
				patchRepo = PatchRepoX86;
				patchFile = PatchFile;
				patchBranch = PatchBranchX86;
			}

			// Use branch-specific patching if needed
			if (!string.IsNullOrEmpty(patchBranch) && patchBranch != "master")
			{
				await _patchingService.ApplyPatchesAsync(patchOwner, patchRepo, patchFile, installDir, CreateSubProgress(65, 15), patchBranch);
			}
			else
			{
				await _patchingService.ApplyPatchesAsync(patchOwner, patchRepo, patchFile, installDir, CreateSubProgress(65, 15));
			}

			// Save Patches version info
			await _installedPackagesService.SetPatchesVersionAsync(
				$"{patchOwner}/{patchRepo}",
				patchBranch);
		}

		// Step 5: Install fixes package (from local zip or GitHub)
		if (localZipOverrides?.FixesZipPath != null)
		{
			progress.Report(new InstallProgressReport { Message = "Installing fixes package from local zip...", Percentage = 80 });
			await _packageInstallService.InstallStandardFromLocalZipAsync(localZipOverrides.FixesZipPath, installDir, PackageInstallService.DefaultIgnorePatterns, CreateSubProgress(85, 15));

			await _installedPackagesService.SetFixesVersionAsync(
				"Local zip",
				Path.GetFileName(localZipOverrides.FixesZipPath),
				Path.GetFileName(localZipOverrides.FixesZipPath));
		}
		else
		{
			progress.Report(new InstallProgressReport { Message = $"Fetching {channelLabel} {FixesDisplayName} fixes package...", Percentage = 80 });
			var fixesReleases = await _githubService.FetchReleasesAsync(FixesOwner, FixesRepo);
			var latestFixes = releaseChannel == ReleaseChannel.Nightly
				? fixesReleases.FirstOrDefault(r => r.TagName == "nightly")
					?? throw new Exception($"Could not find a nightly release for {FixesRepo}.")
				: fixesReleases
					.Where(r => !r.Prerelease || r.TagName != "nightly")
					.OrderByDescending(r => r.PublishedAt)
					.FirstOrDefault()
					?? throw new Exception($"Could not find any stable releases for {FixesRepo}.");

			await _packageInstallService.InstallStandardPackageAsync(latestFixes, installDir, PackageInstallService.DefaultIgnorePatterns, CreateSubProgress(85, 15));

			// Save Fixes version info
			await _installedPackagesService.SetFixesVersionAsync(
				$"{FixesOwner}/{FixesRepo}",
				latestFixes.TagName,
				GetFixesReleaseDisplayName(latestFixes));
		}

		// The pre-install cleanup is handled automatically within InstallStandardPackageAsync
		// (removes outdated folders before extraction)

		// TODO: Process .launcherdependencies (can be added here later)

		progress.Report(new InstallProgressReport { Message = "Quick Install finished successfully!", Percentage = 100 });
	}

	private static string GetFixesReleaseDisplayName(GitHubRelease release)
	{
		if (release.Prerelease || string.Equals(release.TagName, "nightly", StringComparison.OrdinalIgnoreCase))
		{
			string[] patterns = { "-gmod.zip", "-release.zip", ".zip" };
			foreach (var pattern in patterns)
			{
				var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) && !a.Name.Contains("-symbols"));
				if (asset != null)
				{
					return Path.GetFileNameWithoutExtension(asset.Name);
				}
			}
		}

		return release.Name ?? release.TagName;
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