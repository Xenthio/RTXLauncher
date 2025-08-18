// Services/GarrysModInstallService.cs
using RTXLauncher.Core.Models; // Or a common folder for these types

namespace RTXLauncher.Core.Services;


public class GarrysModInstallService
{
	/// <summary>
	/// Creates a new Garry's Mod installation by copying and symlinking from the vanilla install.
	/// This is a long-running operation that should be run on a background thread.
	/// </summary>
	/// <param name="vanillaPath">The path to the source Garry's Mod installation.</param>
	/// <param name="newInstallPath">The path where the new RTX installation will be created.</param>
	/// <param name="progress">An IProgress object to report progress back to the UI.</param>
	public async Task CreateNewGmodInstallAsync(string vanillaPath, string newInstallPath, IProgress<InstallProgressReport> progress)
	{

		// Here's what installing clones from the vanilla folder.
		// We symlink VanillaGMOD/garrysmod/garrysmod_*.vpk, (garrysmod_000.vpk, garrysmod_001.vpk, garrysmod_dir.vpk etc.) files to the new install folder.
		// We symlink VanillaGMOD/garrysmod/fallbacks_*.vpk, (fallbacks_000.vpk, fallbacks_001.vpk, fallbacks_dir.vpk etc.) files to the new install folder.
		// We symlink VanillaGMOD/sourceengine entirely to the new install folder.
		// we copy over everything in VanillaGMOD/bin to the new install folder.
		// we copy over everything in VanillaGMOD/garrysmod to the new install folder, except for the addons, saves, settings, download, cache folder. (ignore filetypes like .dem and .log)
		// we create a blank RTXGMOD/garrysmod/addons folder.
		// We symlink VanillaGMOD/garrysmod/saves entirely to the new install folder.
		// We symlink VanillaGMOD/garrysmod/settings entirely to the new install folder.
		// This is enough to run the game! so it should be all we need!!!!
		await Task.Run(() =>
		{
			// Total number of discrete steps in the installation process.
			const int totalSteps = 12;
			int currentStep = 0;

			// Helper function to report progress.
			void Report(string message, int step)
			{
				progress.Report(new InstallProgressReport { Message = message, Percentage = (step * 100) / totalSteps });
			}

			try
			{
				Report($"Creating RTX install in: {newInstallPath}", currentStep++);

				// Step 1: Copy bin folder
				Report("Copying bin folder...", currentStep);
				CopyDirectory(Path.Combine(vanillaPath, "bin"), Path.Combine(newInstallPath, "bin"));
				currentStep++;

				// Step 2: Create main garrysmod folder
				var newGarrymodPath = Path.Combine(newInstallPath, "garrysmod");
				var vanillaGarrymodPath = Path.Combine(vanillaPath, "garrysmod");
				Directory.CreateDirectory(newGarrymodPath);
				Report("Created garrysmod folder", currentStep++);

				// Step 3: Copy game executable
				Report("Copying game executable...", currentStep);
				if (File.Exists(Path.Combine(vanillaPath, "gmod.exe")))
				{
					File.Copy(Path.Combine(vanillaPath, "gmod.exe"), Path.Combine(newInstallPath, "gmod.exe"), true);
				}
				else if (File.Exists(Path.Combine(vanillaPath, "hl2.exe")))
				{
					Report("gmod.exe not found, copying hl2.exe instead", currentStep);
					File.Copy(Path.Combine(vanillaPath, "hl2.exe"), Path.Combine(newInstallPath, "hl2.exe"), true);
				}
				currentStep++;

				// Step 4: Copy steam_appid.txt
				Report("Copying steam_appid.txt", currentStep);
				if (File.Exists(Path.Combine(vanillaPath, "steam_appid.txt")))
				{
					File.Copy(Path.Combine(vanillaPath, "steam_appid.txt"), Path.Combine(newInstallPath, "steam_appid.txt"), true);
				}
				currentStep++;

				// Keep track of files we symlink so we don't copy them later
				var symlinkedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				// Step 5: Symlink VPK files
				Report("Symlinking VPK files...", currentStep);
				foreach (var vpkFile in Directory.GetFiles(vanillaGarrymodPath, "*.vpk"))
				{
					var fileName = Path.GetFileName(vpkFile);
					var targetPath = Path.Combine(newGarrymodPath, fileName);
					if (!File.Exists(targetPath))
					{
						CreateFileSymbolicLink(targetPath, vpkFile);
						symlinkedFiles.Add(fileName);
					}
				}
				currentStep++;

				// Step 6: Symlink external engine folders
				var externalFoldersToSymlink = new List<string> { "sourceengine", "platform" };
				Report("Symlinking engine folders...", currentStep);
				foreach (var folderName in externalFoldersToSymlink)
				{
					var vanillaFolderPath = Path.Combine(vanillaPath, folderName);
					var newFolderPath = Path.Combine(newInstallPath, folderName);
					if (Directory.Exists(vanillaFolderPath) && !Directory.Exists(newFolderPath))
					{
						CreateDirectorySymbolicLink(newFolderPath, vanillaFolderPath);
					}
				}
				currentStep++;

				// Define folders to exclude from copying (they will be symlinked instead)
				var garrymodFoldersToSymlink = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
					"saves", "dupes", "demos", "settings", "cache",
					"materials", "models", "maps", "screenshots", "videos", "download"
				};
				var excludedFolders = new HashSet<string>(garrymodFoldersToSymlink);
				excludedFolders.Add("addons"); // We create a blank addons folder

				var excludedExtensions = new HashSet<string> { ".dem", ".log" };

				// Step 7: Copy root files in garrysmod folder
				Report("Copying root garrysmod files...", currentStep);
				foreach (var file in Directory.GetFiles(vanillaGarrymodPath, "*", SearchOption.TopDirectoryOnly))
				{
					var fileName = Path.GetFileName(file);
					var fileExt = Path.GetExtension(file).ToLowerInvariant();
					if (!symlinkedFiles.Contains(fileName) && !excludedExtensions.Contains(fileExt))
					{
						File.Copy(file, Path.Combine(newGarrymodPath, fileName), true);
					}
				}
				currentStep++;

				// Step 8: Copy non-excluded subdirectories in garrysmod folder
				Report("Copying garrysmod subdirectories...", currentStep);
				foreach (var dir in Directory.GetDirectories(vanillaGarrymodPath, "*", SearchOption.TopDirectoryOnly))
				{
					var dirName = Path.GetFileName(dir);
					if (!excludedFolders.Contains(dirName))
					{
						CopyDirectory(dir, Path.Combine(newGarrymodPath, dirName));
					}
				}
				currentStep++;

				// Step 9: Create a blank addons folder
				Report("Creating blank addons folder...", currentStep);
				Directory.CreateDirectory(Path.Combine(newGarrymodPath, "addons"));
				currentStep++;

				// Step 10 & 11 (combined): Symlink all the excluded garrysmod folders
				Report("Symlinking content folders...", currentStep);
				foreach (var folderName in garrymodFoldersToSymlink)
				{
					var vanillaFolderPath = Path.Combine(vanillaGarrymodPath, folderName);
					var newFolderPath = Path.Combine(newGarrymodPath, folderName);
					if (Directory.Exists(vanillaFolderPath) && !Directory.Exists(newFolderPath))
					{
						CreateDirectorySymbolicLink(newFolderPath, vanillaFolderPath);
					}
				}
				currentStep++;

				Report("Installation Complete!", totalSteps);
			}
			catch (Exception)
			{
				// Let the exception bubble up to the caller (the ViewModel)
				// which will be responsible for showing an error message.
				throw;
			}
		});
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
				"Failed to create file symbolic link. This often requires Administrator privileges.",
				path,
				ex
			);
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

	/// <summary>
	/// Helper method to copy directories recursively
	/// </summary>
	private static void CopyDirectory(string sourceDir, string destinationDir)
	{
		// Create the destination directory if it doesn't exist
		Directory.CreateDirectory(destinationDir);

		// Copy files
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var fileName = Path.GetFileName(file);
			var destFile = Path.Combine(destinationDir, fileName);
			File.Copy(file, destFile, true);
		}

		// Copy subdirectories recursively
		foreach (var directory in Directory.GetDirectories(sourceDir))
		{
			var dirName = Path.GetFileName(directory);
			var destDir = Path.Combine(destinationDir, dirName);
			CopyDirectory(directory, destDir);
		}
	}
}