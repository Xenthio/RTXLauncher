using RTXLauncher.Core.Utilities;
using System.IO.Compression;

namespace RTXLauncher.Core.Services;

public class AddonInstallService
{
	/// <summary>
	/// Installs a mod/addon and returns a list of created file/directory paths.
	/// </summary>
	public async Task<List<string>> InstallAddonAsync(string filePath, string addonName, Func<string, Task<bool>> confirmationProvider, IProgress<string>? progress = null)
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException("Addon file not found.", filePath);

		var installFolder = GarrysModUtility.GetThisInstallFolder();
		var tempExtractPath = Path.Combine(Path.GetTempPath(), "rtx_addon_install", Guid.NewGuid().ToString());
		var installedPaths = new List<string>();

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
				File.Copy(filePath, Path.Combine(tempExtractPath, Path.GetFileName(filePath)), true);
			}

			progress?.Report("Analyzing addon contents...");
			installedPaths = await ProcessExtractedFiles(tempExtractPath, addonName, installFolder, confirmationProvider, progress);
		}
		finally
		{
			if (Directory.Exists(tempExtractPath))
			{
				Directory.Delete(tempExtractPath, true);
			}
		}
		return installedPaths;
	}

	/// <summary>
	/// Uninstalls an addon by deleting the files and directories at the specified paths.
	/// </summary>
	public Task UninstallAddonAsync(List<string> installedPaths, IProgress<string>? progress = null)
	{
		return Task.Run(() =>
		{
			var installFolder = GarrysModUtility.GetThisInstallFolder();
			foreach (var path in installedPaths.OrderByDescending(p => p.Length)) // Process deeper paths first
			{
				try
				{
					if (File.Exists(path))
					{
						progress?.Report($"Deleting file: {Path.GetFileName(path)}");
						File.Delete(path);

						// Special handling for rtx.conf: try to restore a backup
						if (Path.GetFileName(path).Equals("rtx.conf", StringComparison.OrdinalIgnoreCase))
						{
							var backup = Directory.GetFiles(installFolder, "rtx.conf.backup.*")
												  .OrderByDescending(f => f)
												  .FirstOrDefault();
							if (backup != null)
							{
								progress?.Report("Restoring rtx.conf from backup...");
								File.Move(backup, path);
							}
						}
					}
					else if (Directory.Exists(path))
					{
						progress?.Report($"Deleting directory: {Path.GetFileName(path)}");
						Directory.Delete(path, true);
					}
				}
				catch (Exception ex)
				{
					// Log error but continue trying to uninstall other parts
					progress?.Report($"ERROR: Could not remove '{path}'. {ex.Message}");
				}
			}
		});
	}

	private async Task<List<string>> ProcessExtractedFiles(string extractedPath, string addonName, string installFolder, Func<string, Task<bool>> confirmationProvider, IProgress<string>? progress)
	{
		var createdPaths = new List<string>();

		// Heuristic 1: Find Remix Mods (containing mod.usda)
		var modUsdaFiles = Directory.GetFiles(extractedPath, "mod.usda", SearchOption.AllDirectories);
		if (modUsdaFiles.Any())
		{
			var modUsdaFile = modUsdaFiles.First();
			var modRoot = Path.GetDirectoryName(modUsdaFile);

			if (modRoot != null)
			{
				var targetModsFolder = Path.Combine(installFolder, "rtx-remix", "mods");
				Directory.CreateDirectory(targetModsFolder);

				string finalModFolderName = new DirectoryInfo(modRoot).Name;
				if (Path.GetFullPath(modRoot).Equals(Path.GetFullPath(extractedPath)))
				{
					finalModFolderName = addonName;
				}

				var targetPath = Path.Combine(targetModsFolder, finalModFolderName);
				progress?.Report($"Installing Remix mod '{finalModFolderName}'...");
				CopyDirectory(modRoot, targetPath);
				createdPaths.Add(targetPath); // Track the created directory
				return createdPaths;
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
				createdPaths.Add(existingConfPath); // Track the replaced file
				progress?.Report("rtx.conf has been replaced.");
			}
			else
			{
				progress?.Report("Skipped replacing rtx.conf.");
			}
		}
		return createdPaths;
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