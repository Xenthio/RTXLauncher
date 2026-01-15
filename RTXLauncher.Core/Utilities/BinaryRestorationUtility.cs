// Utilities/BinaryRestorationUtility.cs

using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Utilities;

/// <summary>
/// Utility for restoring original game binaries from the vanilla installation before re-patching
/// </summary>
public static class BinaryRestorationUtility
{
	/// <summary>
	/// Restores original game binaries from vanilla installation to RTX installation.
	/// This must be called before applying patches if binaries were previously patched.
	/// </summary>
	/// <param name="vanillaPath">Path to the vanilla Garry's Mod installation</param>
	/// <param name="rtxInstallPath">Path to the RTX installation</param>
	/// <param name="progress">Progress reporter</param>
	/// <param name="manualVanillaPath">Optional manual vanilla path override</param>
	public static void RestoreOriginalBinaries(string rtxInstallPath, IProgress<InstallProgressReport>? progress = null, string? manualVanillaPath = null)
	{
		progress?.Report(new InstallProgressReport { Message = "Restoring original binaries from vanilla installation...", Percentage = 0 });

		// Get vanilla installation path
		var vanillaPath = GarrysModUtility.GetVanillaInstallFolder(manualVanillaPath);
		if (string.IsNullOrEmpty(vanillaPath) || !Directory.Exists(vanillaPath))
		{
			throw new DirectoryNotFoundException("Could not find vanilla Garry's Mod installation. Cannot restore original binaries.");
		}

	// Determine install type to know which binaries to restore
	var installType = GarrysModUtility.GetInstallType(rtxInstallPath);
	
	if (installType == "gmod_x86-64")
	{
		// 64-bit installation - restore bin/win64 binaries
		RestoreBinFolder(vanillaPath, rtxInstallPath, "bin/win64", progress);
	}
	else if (installType == "gmod_main" || installType == "gmod_i386")
	{
		// 32-bit installation - restore bin binaries (engine) and garrysmod/bin binaries (game DLLs)
		RestoreBinFolder(vanillaPath, rtxInstallPath, "bin", progress);
		RestoreBinFolder(vanillaPath, rtxInstallPath, "garrysmod/bin", progress);
	}
	else
	{
		throw new InvalidOperationException($"Cannot restore binaries for installation type: {installType}");
	}

		// Also restore the main game executable if it exists
		RestoreGameExecutable(vanillaPath, rtxInstallPath, progress);

		progress?.Report(new InstallProgressReport { Message = "Original binaries restored successfully", Percentage = 100 });
	}

	/// <summary>
	/// Restores binaries from a specific bin folder
	/// </summary>
	private static void RestoreBinFolder(string vanillaPath, string rtxInstallPath, string binSubPath, IProgress<InstallProgressReport>? progress)
	{
		var vanillaBinPath = Path.Combine(vanillaPath, binSubPath);
		var rtxBinPath = Path.Combine(rtxInstallPath, binSubPath);

		if (!Directory.Exists(vanillaBinPath))
		{
			throw new DirectoryNotFoundException($"Vanilla bin folder not found: {vanillaBinPath}");
		}

		if (!Directory.Exists(rtxBinPath))
		{
			Directory.CreateDirectory(rtxBinPath);
		}

		progress?.Report(new InstallProgressReport { Message = $"Copying binaries from {binSubPath}...", Percentage = 25 });

		// Get all files that might be patched (executables and DLLs)
		var filesToRestore = new[] { "*.exe", "*.dll", "*.bin" };
		var restoredCount = 0;

		foreach (var pattern in filesToRestore)
		{
			foreach (var vanillaFile in Directory.GetFiles(vanillaBinPath, pattern, SearchOption.TopDirectoryOnly))
			{
				var fileName = Path.GetFileName(vanillaFile);
				var rtxFile = Path.Combine(rtxBinPath, fileName);

				// Copy the original file, overwriting any patched version
				File.Copy(vanillaFile, rtxFile, overwrite: true);
				restoredCount++;
			}
		}

		progress?.Report(new InstallProgressReport { Message = $"Restored {restoredCount} binary files from {binSubPath}", Percentage = 75 });
	}

	/// <summary>
	/// Restores the main game executable (gmod.exe or hl2.exe)
	/// </summary>
	private static void RestoreGameExecutable(string vanillaPath, string rtxInstallPath, IProgress<InstallProgressReport>? progress)
	{
		// Check for gmod.exe first
		var vanillaExe = Path.Combine(vanillaPath, "gmod.exe");
		var rtxExe = Path.Combine(rtxInstallPath, "gmod.exe");

		if (File.Exists(vanillaExe))
		{
			progress?.Report(new InstallProgressReport { Message = "Restoring gmod.exe...", Percentage = 80 });
			File.Copy(vanillaExe, rtxExe, overwrite: true);
			return;
		}

		// Fallback to hl2.exe for older installations
		vanillaExe = Path.Combine(vanillaPath, "hl2.exe");
		rtxExe = Path.Combine(rtxInstallPath, "hl2.exe");

		if (File.Exists(vanillaExe))
		{
			progress?.Report(new InstallProgressReport { Message = "Restoring hl2.exe...", Percentage = 80 });
			File.Copy(vanillaExe, rtxExe, overwrite: true);
		}
	}
}

