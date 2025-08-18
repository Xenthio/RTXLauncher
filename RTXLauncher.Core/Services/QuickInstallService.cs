// Services/QuickInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class QuickInstallService
{
	// Define the recommended sources as constants
	private const string RecommendedRemixSource = "sambow23/dxvk-remix-gmod";
	private const string RecommendedFixesSource = "Xenthio/gmod-rtx-fixes-2 (Any)";
	private const string RecommendedPatchesSourceX64 = "sambow23/SourceRTXTweaks";
	private const string RecommendedPatchesSourceX86 = "BlueAmulet/SourceRTXTweaks";

	// Dependencies on all our other services
	private readonly GarrysModInstallService _installService;
	private readonly GitHubService _githubService;
	private readonly PackageInstallService _packageInstallService;
	private readonly PatchingService _patchingService;

	public QuickInstallService(GarrysModInstallService installService, GitHubService githubService, PackageInstallService packageInstallService, PatchingService patchingService)
	{
		_installService = installService;
		_githubService = githubService;
		_packageInstallService = packageInstallService;
		_patchingService = patchingService;
	}

	public async Task PerformQuickInstallAsync(IProgress<InstallProgressReport> progress)
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

		// Step 1: Check for existing installation
		progress.Report(new InstallProgressReport { Message = "Checking for existing RTX installation...", Percentage = 5 });
		var installDir = GarrysModUtility.GetThisInstallFolder();
		var installType = GarrysModUtility.GetInstallType(installDir);

		if (installType == "unknown")
		{
			// Step 2: Create new installation if needed
			var vanillaDir = GarrysModUtility.GetVanillaInstallFolder();
			if (string.IsNullOrEmpty(vanillaDir)) throw new DirectoryNotFoundException("Could not find vanilla Garry's Mod installation.");

			await _installService.CreateNewGmodInstallAsync(vanillaDir, installDir, CreateSubProgress(10, 20));
			installType = GarrysModUtility.GetInstallType(installDir); // Re-check type
		}

		// Step 3: Install latest RTX Remix
		progress.Report(new InstallProgressReport { Message = "Fetching latest RTX Remix...", Percentage = 30 });
		var (remixOwner, remixRepo) = ("sambow23", "dxvk-remix-gmod"); // From constant
		var remixReleases = await _githubService.FetchReleasesAsync(remixOwner, remixRepo);
		var latestRemix = remixReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault()
			?? throw new Exception("Could not find any RTX Remix releases.");

		await _packageInstallService.InstallRemixPackageAsync(latestRemix, installDir, CreateSubProgress(35, 25));

		// Step 4: Apply recommended patches
		progress.Report(new InstallProgressReport { Message = "Applying recommended patches...", Percentage = 60 });
		bool isX64 = installType == "gmod_x86-64";
		var (patchOwner, patchRepo, patchFile) = isX64
			? ("sambow23", "SourceRTXTweaks", "applypatch.py")
			: ("BlueAmulet", "SourceRTXTweaks", "applypatch.py");

		await _patchingService.ApplyPatchesAsync(patchOwner, patchRepo, patchFile, installDir, CreateSubProgress(65, 15));

		// Step 5: Install recommended fixes package
		progress.Report(new InstallProgressReport { Message = "Fetching recommended fixes package...", Percentage = 80 });
		var (fixesOwner, fixesRepo) = ("Xenthio", "gmod-rtx-fixes-2"); // From constant
		var fixesReleases = await _githubService.FetchReleasesAsync(fixesOwner, fixesRepo);
		var latestFixes = fixesReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault()
			?? throw new Exception("Could not find any fixes packages.");

		await _packageInstallService.InstallStandardPackageAsync(latestFixes, installDir, PackageInstallService.DefaultIgnorePatterns, CreateSubProgress(85, 15));

		// Step 6: TODO - Process .launcherdependencies (can be added here later)

		progress.Report(new InstallProgressReport { Message = "Quick Install finished successfully!", Percentage = 100 });
	}
}