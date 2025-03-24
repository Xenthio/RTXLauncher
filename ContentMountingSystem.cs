namespace RTXLauncher
{
	public static class ContentMountingSystem
	{
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
		public static void MountGame(string gameFolder, string installFolder, string remixModFolder)
		{
			// Mount the content
			var installPath = SteamLibrary.GetGameInstallFolder(installFolder);
			if (installPath == null)
			{
				MessageBox.Show("Game not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
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
		public static void UnMountGame(string gameFolder, string installFolder, string remixModFolder)
		{
			// Unmount the content
			var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
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
		public static bool IsGameMounted(string gameFolder, string installFolder, string remixModFolder)
		{
			// Check if the content is mounted
			var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);
			return Directory.Exists(sourceContentMountPath) && Directory.Exists(remixModMountPath);
		}
	}
}
