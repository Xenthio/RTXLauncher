using RTXLauncher.Core.Utilities;
using System.IO.Compression;

namespace RTXLauncher.Core.Services;

public class AddonInstallService
{
	/// <summary>
	/// Installs a mod/addon from a given file path (e.g., a downloaded zip file).
	/// </summary>
	/// <param name="filePath">The path to the addon file (likely a zip).</param>
	/// <param name="addonName">A descriptive name for the addon, used for creating folders.</param>
	/// <param name="confirmationProvider">A UI-agnostic function to ask for user confirmation.</param>
	/// <param name="progress">An optional progress reporter.</param>
	public async Task InstallAddonAsync(string filePath, string addonName, Func<string, Task<bool>> confirmationProvider, IProgress<string>? progress = null)
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException("Addon file not found.", filePath);

		var installFolder = GarrysModUtility.GetThisInstallFolder();
		var tempExtractPath = Path.Combine(Path.GetTempPath(), "rtx_addon_install", Guid.NewGuid().ToString());

		try
		{
			Directory.CreateDirectory(tempExtractPath);
			progress?.Report($"Extracting {Path.GetFileName(filePath)}...");

			if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
			{
				await Task.Run(() => ZipFile.ExtractToDirectory(filePath, tempExtractPath));
			}
			else
			{
				// If it's not a zip, copy the file to the temp directory to handle it uniformly
				File.Copy(filePath, Path.Combine(tempExtractPath, Path.GetFileName(filePath)), true);
			}

			progress?.Report("Analyzing addon contents...");
			await ProcessExtractedFiles(tempExtractPath, addonName, installFolder, confirmationProvider, progress);
		}
		finally
		{
			if (Directory.Exists(tempExtractPath))
			{
				Directory.Delete(tempExtractPath, true);
			}
		}
	}

	private async Task ProcessExtractedFiles(string extractedPath, string addonName, string installFolder, Func<string, Task<bool>> confirmationProvider, IProgress<string>? progress)
	{
		// Heuristic 1: Find Remix Mods (containing mod.usda)
		var modUsdaFiles = Directory.GetFiles(extractedPath, "mod.usda", SearchOption.AllDirectories);
		if (modUsdaFiles.Any())
		{
			var modUsdaFile = modUsdaFiles.First(); // Prioritize the first one found
			var modRoot = Path.GetDirectoryName(modUsdaFile);

			if (modRoot != null)
			{
				var targetModsFolder = Path.Combine(installFolder, "rtx-remix", "mods");
				Directory.CreateDirectory(targetModsFolder);

				string finalModFolderName;
				// If mod.usda is in the root of the zip, create a new folder for it.
				if (Path.GetFullPath(modRoot).Equals(Path.GetFullPath(extractedPath)))
				{
					finalModFolderName = addonName;
					var targetPath = Path.Combine(targetModsFolder, finalModFolderName);
					progress?.Report($"Installing Remix mod '{addonName}'...");
					CopyDirectory(modRoot, targetPath);
				}
				else // Otherwise, it's in a subdirectory, so copy that subdirectory.
				{
					finalModFolderName = new DirectoryInfo(modRoot).Name;
					var targetPath = Path.Combine(targetModsFolder, finalModFolderName);
					progress?.Report($"Installing Remix mod '{finalModFolderName}'...");
					CopyDirectory(modRoot, targetPath);
				}
				return; // Remix mod installed, primary job is done.
			}
		}

		// Heuristic 2: Find rtx.conf files
		var rtxConfFiles = Directory.GetFiles(extractedPath, "rtx.conf", SearchOption.AllDirectories);
		if (rtxConfFiles.Any())
		{
			var newConfPath = rtxConfFiles.First();
			var existingConfPath = Path.Combine(installFolder, "rtx.conf");

			progress?.Report("Found rtx.conf file.");
			bool shouldReplace = await confirmationProvider(
				"This addon includes an 'rtx.conf' file. Would you like to replace your current configuration? A backup of your existing file will be created."
			);

			if (shouldReplace)
			{
				progress?.Report("Backing up and replacing rtx.conf...");
				if (File.Exists(existingConfPath))
				{
					var backupPath = Path.Combine(installFolder, $"rtx.conf.backup.{DateTime.Now:yyyyMMddHHmmss}");
					File.Move(existingConfPath, backupPath, true);
					progress?.Report($"Backed up current config to {Path.GetFileName(backupPath)}");
				}
				File.Copy(newConfPath, existingConfPath, true);
				progress?.Report("rtx.conf has been replaced.");
			}
			else
			{
				progress?.Report("Skipped replacing rtx.conf.");
			}
		}
	}

	private static void CopyDirectory(string sourceDir, string destinationDir)
	{
		Directory.CreateDirectory(destinationDir);
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
			File.Copy(file, destFile, true);
		}
		foreach (var directory in Directory.GetDirectories(sourceDir))
		{
			var destDir = Path.Combine(destinationDir, Path.GetFileName(directory));
			CopyDirectory(directory, destDir);
		}
	}
}