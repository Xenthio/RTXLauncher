namespace RTXLauncher
{
	internal class SteamLibrary
	{

		/// <summary>
		/// Checks all steam library paths for the install folder, should be like D:\SteamLibrary\steamapps\common\(installFolder), returns null if not installed/found
		/// </summary>
		/// <returns></returns>
		public static string GetGameInstallFolder(string installFolder)
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
		public static bool IsGameInstalled(string gameFolder, string installFolder, string remixModFolder)
		{
			// Check if the content is installed
			return GetGameInstallFolder(installFolder) != null;
		}
		public static List<string> GetSteamLibraryPaths()
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
	}
}
