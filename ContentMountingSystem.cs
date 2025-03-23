namespace RTXLauncher
{
	public static class ContentMountingSystem
	{
		/// <summary>
		/// Get the game folder, should be like D:\SteamLibrary\steamapps\common\GarrysMod, should be where this exe is.
		/// </summary>
		/// <returns></returns>
		private static string GetGmodInstallFolder()
		{

			return Path.GetDirectoryName(System.AppContext.BaseDirectory);//Assembly.GetExecutingAssembly().Location);
		}
		/// <summary>
		/// Checks all steam library paths for the install folder, should be like D:\SteamLibrary\steamapps\common\(installFolder), returns null if not installed/found
		/// </summary>
		/// <returns></returns>
		private static string GetInstallFolder(string installFolder)
		{
			// Get the steam library paths
			var steamLibraryPaths = GetSteamLibraryPaths();
			foreach (var path in steamLibraryPaths)
			{
				var installPath = Path.Combine(path, "steamapps", "common", installFolder);
				if (Directory.Exists(installPath))
				{
					return installPath;
				}
			}
			return null;
		}
		public static bool IsContentInstalled(string gameFolder, string installFolder, string remixModFolder)
		{
			// Check if the content is installed
			return GetInstallFolder(installFolder) != null;
		}
		private static List<string> GetSteamLibraryPaths()
		{
			var list = new List<string>();
			// Get the steam library paths from libraryfolders.vdf
			/*
				"libraryfolders"
				{
					"0"
					{
						"path"		"C:\\Program Files (x86)\\Steam"
						...
					}
					"1"
					{
					   "path"		"E:\\SteamLibrary"
						...
					}
				}
			 */
			var steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
			var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
			if (File.Exists(libraryFoldersPath))
			{
				var libraryFolders = File.ReadAllLines(libraryFoldersPath);
				foreach (var line in libraryFolders)
				{
					if (line.Contains("path"))
					{
						var path = line.Split('"')[3];
						list.Add(path);
					}
				}
			}
			return list;
		}

		// Mounting and unmounting content
		// the gameFolder is the folder name of the game, like "hl2rtx"
		// the installFolder is the folder name of the game in the steamapps\common folder, like "Half-Life 2: RTX", use GetInstallFolder to get the full path
		// the remixModFolder is the folder name of the mod in the installfolder\rtx-remix\mods folder, like "hl2rtx"

		// when mounting, these folders should be symlinked:

		// The source side content: (fullInstallPath)\(gameFolder) -> (garrysmodPath)\garrysmod\addons\mount-(gameFolder)
		// The remix mod: (fullInstallPath)\rtx-remix\mods\(remixModFolder) -> (garrysmodPath)\GarrysMod\rtx-remix\mods\mount-(gameFolder)-(remixModFolder)

		// examples:
		// The source side content: D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\hl2rtx -> D:\SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons\mount-hl2rtx
		// The source side content (for custom folder): D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\hl2rtx\custom\new_rtx_hands -> D:\SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons\mount-hl2rtx-new_rtx_hands
		// The remix mod: D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\rtx-remix\mods\hl2rtx -> D:\SteamLibrary\steamapps\common\GarrysMod\rtx-remix\mods\mount-hl2rtx-hl2rtx

		// However, for source side content, the folder itself shouldn't be linked, but the models, and maps folder should be linked instead, and for materials all folders inside should be linked except for the materials\vgui and materias\dev folders
		// do this for the folder itself, aswell as all folders inside the custom folder
		public static void MountContent(string gameFolder, string installFolder, string remixModFolder)
		{
			// Mount the content
			var installPath = GetInstallFolder(installFolder);
			if (installPath == null)
			{
				MessageBox.Show("Game not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			var gmodPath = GetGmodInstallFolder();
			var sourceContentPath = Path.Combine(installPath, gameFolder);
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
			var remixModPath = Path.Combine(installPath, "rtx-remix", "mods", remixModFolder);
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);

			// link the remix mod
			// check if it already exists
			if (!Directory.Exists(remixModMountPath))
			{
				CreateSymbolicLink(remixModMountPath, remixModPath);
			}

			// run LinkSourceContent on the sourceContentPath, aswell as all folders inside the custom folder
			LinkSourceContent(sourceContentPath, sourceContentMountPath);
			foreach (var folder in Directory.GetDirectories(Path.Combine(sourceContentPath, "custom")))
			{
				LinkSourceContent(folder, Path.Combine($"{sourceContentMountPath}-{Path.GetFileName(folder)}"));
			}
		}
		// Link the content of the source content/custom folder
		private static void LinkSourceContent(string path, string destinationMountPath)
		{
			// create path
			Directory.CreateDirectory(destinationMountPath);
			// link the models folder
			if (Directory.Exists(Path.Combine(path, "models")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "models")))
				{
					CreateSymbolicLink(Path.Combine(destinationMountPath, "models"), Path.Combine(path, "models"));
				}
			}
			// link the maps folder
			if (Directory.Exists(Path.Combine(path, "maps")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "maps")))
				{
					CreateSymbolicLink(Path.Combine(destinationMountPath, "maps"), Path.Combine(path, "maps"));
				}
			}
			// link the materials folder, note for materials all folders inside should be linked except for the materials\vgui and materias\dev folders
			if (Directory.Exists(Path.Combine(path, "materials")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "materials")))
				{
					Directory.CreateDirectory(Path.Combine(destinationMountPath, "materials"));
				}
				var dontLink = new List<string> { "vgui", "dev", "editor", "perftest", "tools" };
				foreach (var folder in Directory.GetDirectories(Path.Combine(path, "materials")))
				{
					var folderName = Path.GetFileName(folder);
					if (!dontLink.Contains(folderName))
					{
						CreateSymbolicLink(Path.Combine(destinationMountPath, "materials", folderName), folder);
					}
				}
			}
		}

		private static bool CreateSymbolicLink(string path, string pathToTarget)
		{
			// Create a symbolic link 
			Directory.CreateSymbolicLink(path, pathToTarget);
			return true;
		}

		private enum SymbolicLink
		{
			File = 0,
			Directory = 1
		}

		// Unmounting content
		// when unmounting, delete the folders
		// the source side content: (garrysmodPath)\garrysmod\addons\mount-(gameFolder)
		// the remix mod: (garrysmodPath)\GarrysMod\rtx-remix\mods\mount-(gameFolder)-(remixModFolder)
		// all custom source side content folders: (garrysmodPath)\garrysmod\addons\mount-(gameFolder)-*
		public static void UnMountContent(string gameFolder, string installFolder, string remixModFolder)
		{
			// Unmount the content
			var gmodPath = GetGmodInstallFolder();
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);
			// delete the remix mod
			// delete the remix mod
			if (Directory.Exists(remixModMountPath))
			{
				Directory.Delete(remixModMountPath, true);
			}

			// delete the source content
			if (Directory.Exists(sourceContentMountPath))
			{
				Directory.Delete(sourceContentMountPath, true);
			}

			// delete all custom source side content folders
			var customSourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons");
			foreach (var directory in Directory.GetDirectories(customSourceContentMountPath, "mount-" + gameFolder + "-*"))
			{
				Directory.Delete(directory, true);
			}
		}
		public static bool IsContentMounted(string gameFolder, string installFolder, string remixModFolder)
		{
			// Check if the content is mounted
			var gmodPath = GetGmodInstallFolder();
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);
			return Directory.Exists(sourceContentMountPath) && Directory.Exists(remixModMountPath);
		}
	}
}
