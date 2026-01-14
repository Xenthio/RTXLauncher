// Services/QuickInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class QuickInstallService
{
	// Define the recommended sources as constants
	private const string RecommendedRemixSource = "sambow23/dxvk-remix-gmod";
	private const string RecommendedFixesSource = "Xenthio/garrys-mod-rtx-remixed (Any)";
	private const string RecommendedPatchesSourceX64 = "sambow23/SourceRTXTweaks";
	private const string RecommendedPatchesSourceX86 = "BlueAmulet/SourceRTXTweaks";

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

	/// <summary>
	/// Gets the available fixes package options for the user to choose from.
	/// </summary>
	public static List<FixesPackageInfo> GetAvailableFixesPackages()
	{
		return new List<FixesPackageInfo>
		{
			new FixesPackageInfo
			{
				Option = FixesPackageOption.Standard,
				DisplayName = "garrys-mod-rtx-remixed",
				Description = "The main project",
				Owner = "Xenthio",
				Repo = "garrys-mod-rtx-remixed",
				PatchOwner = "sambow23",
				PatchRepo = "SourceRTXTweaks",
				PatchBranch = "main",
				PatchFile = "applypatch.py",
				RequiresX64 = false,
				RequiresLegacyBuild = true,
				LegacyManifestId = "2195078592256565401" // May 1, 2025 - last known stable x86-64 build
			},
			new FixesPackageInfo
			{
				Option = FixesPackageOption.Performance,
				DisplayName = "garrys-mod-rtx-remixed-perf",
				Description = "Performance-focused version with custom rendering features (64-bit only)",
				Owner = "sambow23",
				Repo = "garrys-mod-rtx-remixed-perf",
				PatchOwner = "sambow23",
				PatchRepo = "SourceRTXTweaks",
				PatchBranch = "perf",
				PatchFile = "applypatch.py",
				RequiresX64 = true,
				RequiresLegacyBuild = false,
				LegacyManifestId = ""
			}
		};
	}

	public async Task PerformQuickInstallAsync(IProgress<InstallProgressReport> progress, FixesPackageOption fixesPackageOption = FixesPackageOption.Standard, string? manualVanillaPath = null, Func<Task<bool>>? legacyDowngradeCallback = null)
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

		// Get the selected fixes package info
		var fixesPackageInfo = GetAvailableFixesPackages().First(p => p.Option == fixesPackageOption);

		// Step 1: Check for existing installation
		progress.Report(new InstallProgressReport { Message = "Checking for existing RTX installation...", Percentage = 5 });
		var installDir = GarrysModUtility.GetThisInstallFolder();
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
		if (fixesPackageInfo.RequiresLegacyBuild && legacyDowngradeCallback != null)
		{
			progress.Report(new InstallProgressReport { Message = "Applying legacy build downgrade...", Percentage = 25 });
			bool downgradeCompleted = await legacyDowngradeCallback();
			if (!downgradeCompleted)
			{
				throw new OperationCanceledException("Legacy build downgrade was cancelled by user.");
			}
		}

		// Validate architecture compatibility
		bool isX64 = installType == "gmod_x86-64";
		if (fixesPackageInfo.RequiresX64 && !isX64)
		{
			throw new InvalidOperationException($"The '{fixesPackageInfo.DisplayName}' fixes package requires a 64-bit installation. Your current installation is 32-bit.");
		}

		// Step 3: Install latest RTX Remix
		progress.Report(new InstallProgressReport { Message = "Fetching latest RTX Remix...", Percentage = 30 });
		var (remixOwner, remixRepo) = ("sambow23", "dxvk-remix-gmod"); // From constant
		var remixReleases = await _githubService.FetchReleasesAsync(remixOwner, remixRepo);
		var latestRemix = remixReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault()
			?? throw new Exception("Could not find any RTX Remix releases.");

		await _packageInstallService.InstallRemixPackageAsync(latestRemix, installDir, CreateSubProgress(35, 25));

		// Save Remix version info
		await _installedPackagesService.SetRemixVersionAsync(
			$"{remixOwner}/{remixRepo}",
			latestRemix.TagName,
			latestRemix.Name ?? latestRemix.TagName);

		// Step 4: Apply recommended patches
		progress.Report(new InstallProgressReport { Message = $"Applying {fixesPackageInfo.DisplayName} patches...", Percentage = 60 });
		string patchOwner, patchRepo, patchFile, patchBranch;
		
		if (isX64)
		{
			patchOwner = fixesPackageInfo.PatchOwner;
			patchRepo = fixesPackageInfo.PatchRepo;
			patchFile = fixesPackageInfo.PatchFile;
			patchBranch = fixesPackageInfo.PatchBranch;
		}
		else
		{
			// 32-bit always uses BlueAmulet patches
			patchOwner = "BlueAmulet";
			patchRepo = "SourceRTXTweaks";
			patchFile = "applypatch.py";
			patchBranch = "master";
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

		// Step 5: Install recommended fixes package
		progress.Report(new InstallProgressReport { Message = $"Fetching {fixesPackageInfo.DisplayName} fixes package...", Percentage = 80 });
		var fixesReleases = await _githubService.FetchReleasesAsync(fixesPackageInfo.Owner, fixesPackageInfo.Repo);
		var latestFixes = fixesReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault()
			?? throw new Exception($"Could not find any releases for {fixesPackageInfo.Repo}.");

		await _packageInstallService.InstallStandardPackageAsync(latestFixes, installDir, PackageInstallService.DefaultIgnorePatterns, CreateSubProgress(85, 15));

		// Save Fixes version info
		await _installedPackagesService.SetFixesVersionAsync(
			$"{fixesPackageInfo.Owner}/{fixesPackageInfo.Repo}",
			latestFixes.TagName,
			latestFixes.Name ?? latestFixes.TagName);

		// The pre-install cleanup is handled automatically within InstallStandardPackageAsync
		// (removes outdated folders before extraction)

		// TODO: Process .launcherdependencies (can be added here later)

		progress.Report(new InstallProgressReport { Message = "Quick Install finished successfully!", Percentage = 100 });
	}
}