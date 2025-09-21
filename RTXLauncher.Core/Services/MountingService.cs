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
			if (Directory.Exists(remixModPath))
			{
				// Ensure a clean mount folder (remove old symlink or directory)
				if (Directory.Exists(remixModMountPath))
				{
					Directory.Delete(remixModMountPath, true);
				}
				Directory.CreateDirectory(remixModMountPath);

				// Copy only ROOT-level .usda files; symlink nested files (including nested .usda) // This is allows to overlay .usda fixes without replacing the orignal files. 
				MirrorRemixFolderWithUsdaCopies(remixModPath, remixModMountPath, true);
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
	/// Downloads the USDA fixes archive and overlays the HL2 RTX demo fixes into the
	/// mounted hl2rtx Remix mod folder (mount-hl2rtx-hl2rtx), overwriting copied .usda files.
	/// </summary>
	public async Task ApplyHl2UsdaFixesAsync(IProgress<InstallProgressReport> progress)
	{
		const string fixesUrl = "https://github.com/sambow23/rtx-usda-fixes/archive/refs/heads/main.zip";
		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncher_UsdFixes_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			progress.Report(new InstallProgressReport { Message = "Downloading USDA fixes...", Percentage = 5 });
			string zipPath = Path.Combine(tempDir, "usda_fixes.zip");
			using (var http = new HttpClient())
			{
				http.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher");
				using var resp = await http.GetAsync(fixesUrl, HttpCompletionOption.ResponseHeadersRead);
				resp.EnsureSuccessStatusCode();
				await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
				await resp.Content.CopyToAsync(fs);
			}

			progress.Report(new InstallProgressReport { Message = "Extracting fixes...", Percentage = 25 });
			string extractDir = Path.Combine(tempDir, "extracted");
			Directory.CreateDirectory(extractDir);
			System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

			// The archive structure: <root>/hl2rtxdemo/<files>
			string? hl2Root = Directory
				.GetDirectories(extractDir)
				.SelectMany(d => Directory.GetDirectories(d, "hl2rtxdemo", SearchOption.TopDirectoryOnly))
				.FirstOrDefault();
			if (string.IsNullOrEmpty(hl2Root) || !Directory.Exists(hl2Root))
			{
				throw new DirectoryNotFoundException("Could not find 'hl2rtxdemo' in the fixes archive.");
			}

			// Destination is the mounted hl2rtx Remix mod path we create during mount
			var gmodPath = GarrysModUtility.GetThisInstallFolder();
			string destMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-hl2rtx-hl2rtx");
			if (!Directory.Exists(destMountPath))
			{
				throw new DirectoryNotFoundException("Please mount 'Half-Life 2: RTX' first. The mount folder was not found.");
			}

			progress.Report(new InstallProgressReport { Message = "Applying fixes...", Percentage = 50 });
			await Task.Run(() => CopyDirectoryOverwrite(hl2Root, destMountPath));

			progress.Report(new InstallProgressReport { Message = "USDA fixes applied successfully.", Percentage = 100 });
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { /* ignore cleanup errors */ }
		}
	}

	private static void CopyDirectoryOverwrite(string sourceDir, string destDir)
	{
		Directory.CreateDirectory(destDir);
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var name = Path.GetFileName(file);
			var dest = Path.Combine(destDir, name);
			File.Copy(file, dest, true);
		}
		foreach (var dir in Directory.GetDirectories(sourceDir))
		{
			var name = Path.GetFileName(dir);
			CopyDirectoryOverwrite(dir, Path.Combine(destDir, name));
		}
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

	private void CreateFileSymbolicLink(string path, string pathToTarget)
	{
		try
		{
			File.CreateSymbolicLink(path, pathToTarget);
		}
		catch (IOException ex)
		{
			throw new SymlinkFailedException(
				"Failed to create file symbolic link. This requires Administrator privileges.",
				path,
				ex
			);
		}
	}

	/// <summary>
	/// Recursively mirrors a Remix mod folder into the mount folder. Copies ONLY root-level
	/// .usda files and creates file symlinks for everything else (including nested .usda files).
	/// Directories are created normally (not symlinked) to allow selective file-level handling.
	/// </summary>
	private void MirrorRemixFolderWithUsdaCopies(string sourceDir, string destDir, bool isRoot)
	{
		// Create destination directory if missing
		Directory.CreateDirectory(destDir);

		// First, process files in this directory
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var fileName = Path.GetFileName(file);
			var destFile = Path.Combine(destDir, fileName);
			var ext = Path.GetExtension(file).ToLowerInvariant();

			if (ext == ".usda" && isRoot)
			{
				// Copy text-based USD ASCII files so users can edit without touching original game
				File.Copy(file, destFile, true);
			}
			else
			{
				// Symlink all other files
				CreateFileSymbolicLink(destFile, file);
			}
		}

		// Then recurse into subdirectories
		foreach (var subdir in Directory.GetDirectories(sourceDir))
		{
			var name = Path.GetFileName(subdir);
			var destSubdir = Path.Combine(destDir, name);
			MirrorRemixFolderWithUsdaCopies(subdir, destSubdir, false);
		}
	}
}