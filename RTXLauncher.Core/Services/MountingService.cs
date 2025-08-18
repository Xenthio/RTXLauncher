using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class MountingService
{
	// We can reuse the SymlinkFailedException from the InstallService
	// or create a new, more specific one if desired.

	/// <summary>
	/// Creates all necessary symbolic links to mount a game's content.
	/// </summary>
	public async Task MountGameAsync(string gameName, string gameFolder, string installFolder, string remixModFolder, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			var installPath = SteamLibraryUtility.GetGameInstallFolder(installFolder);
			if (string.IsNullOrEmpty(installPath))
			{
				throw new DirectoryNotFoundException($"Could not find installation for {gameName}. Please ensure it is installed via Steam.");
			}

			var gmodPath = GarrysModUtility.GetThisInstallFolder();
			var sourceContentPath = Path.Combine(installPath, gameFolder);
			var remixModPath = Path.Combine(installPath, "rtx-remix", "mods", remixModFolder);

			if (!Directory.Exists(sourceContentPath))
			{
				throw new DirectoryNotFoundException($"Source content folder not found at: {sourceContentPath}");
			}

			// --- Create Mount Points ---
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", $"mount-{gameFolder}");
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", $"mount-{gameFolder}-{remixModFolder}");
			Directory.CreateDirectory(Path.Combine(gmodPath, "rtx-remix", "mods"));

			// --- Link Remix Mod ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} Remix content...", Percentage = 25 });
			if (Directory.Exists(remixModPath) && !Directory.Exists(remixModMountPath))
			{
				CreateDirectorySymbolicLink(remixModMountPath, remixModPath);
			}

			// --- Link Source Content ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} Source content...", Percentage = 50 });
			LinkSourceContent(sourceContentPath, sourceContentMountPath);

			// --- Link Custom Content ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} custom content...", Percentage = 75 });
			var customPath = Path.Combine(sourceContentPath, "custom");
			if (Directory.Exists(customPath))
			{
				foreach (var folder in Directory.GetDirectories(customPath))
				{
					LinkSourceContent(folder, Path.Combine($"{sourceContentMountPath}-{Path.GetFileName(folder)}"));
				}
			}

			progress.Report(new InstallProgressReport { Message = $"{gameName} mounted successfully.", Percentage = 100 });
		});
	}

	/// <summary>
	/// Removes all symbolic links for a mounted game.
	/// </summary>
	public void UnmountGame(string gameFolder, string remixModFolder)
	{
		var gmodPath = GarrysModUtility.GetThisInstallFolder();
		var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", $"mount-{gameFolder}");
		var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", $"mount-{gameFolder}-{remixModFolder}");

		if (Directory.Exists(remixModMountPath)) Directory.Delete(remixModMountPath, true);
		if (Directory.Exists(sourceContentMountPath)) Directory.Delete(sourceContentMountPath, true);

		// Unmount custom folders
		var customAddonsPath = Path.Combine(gmodPath, "garrysmod", "addons");
		if (Directory.Exists(customAddonsPath))
		{
			foreach (var directory in Directory.GetDirectories(customAddonsPath, $"mount-{gameFolder}-*"))
			{
				Directory.Delete(directory, true);
			}
		}
	}

	// --- Private Helper Methods (Ported directly from your old code) ---

	private void LinkSourceContent(string path, string destinationMountPath)
	{
		Directory.CreateDirectory(destinationMountPath);

		// Link models folder
		var modelsPath = Path.Combine(path, "models");
		if (Directory.Exists(modelsPath)) CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "models"), modelsPath);

		// Link maps folder
		var mapsPath = Path.Combine(path, "maps");
		if (Directory.Exists(mapsPath)) CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "maps"), mapsPath);

		// Link materials subfolders
		var materialsPath = Path.Combine(path, "materials");
		if (Directory.Exists(materialsPath))
		{
			Directory.CreateDirectory(Path.Combine(destinationMountPath, "materials"));
			var dontLink = new[] { "vgui", "dev", "editor", "perftest", "tools" };
			foreach (var folder in Directory.GetDirectories(materialsPath))
			{
				if (!dontLink.Contains(Path.GetFileName(folder)))
				{
					CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "materials", Path.GetFileName(folder)), folder);
				}
			}
		}
	}

	private void CreateDirectorySymbolicLink(string path, string pathToTarget)
	{
		try
		{
			Directory.CreateSymbolicLink(path, pathToTarget);
		}
		catch (IOException ex)
		{
			throw new SymlinkFailedException(
				"Failed to create directory symbolic link. This often requires Administrator privileges.",
				path,
				ex
			);
		}
	}
}