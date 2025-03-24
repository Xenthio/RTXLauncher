using Microsoft.Win32;

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

			// Try default location first
			var defaultSteamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");

			// If default location doesn't exist, check registry
			if (!Directory.Exists(defaultSteamPath))
			{
				defaultSteamPath = GetSteamInstallPathFromRegistry();

				// If still not found, return empty list
				if (defaultSteamPath == null)
				{
					return list;
				}
			}

			// Add default Steam path to list
			list.Add(defaultSteamPath);

			// Get additional library folders from libraryfolders.vdf
			var libraryFoldersPath = Path.Combine(defaultSteamPath, "steamapps", "libraryfolders.vdf");

			if (File.Exists(libraryFoldersPath))
			{
				try
				{
					var libraryFolders = File.ReadAllLines(libraryFoldersPath);
					foreach (var line in libraryFolders)
					{
						if (line.Contains("\"path\""))
						{
							// Extract path between quotes
							var startQuote = line.IndexOf('"', line.IndexOf("path") + 4);
							var endQuote = line.IndexOf('"', startQuote + 1);

							if (startQuote >= 0 && endQuote > startQuote)
							{
								var path = line.Substring(startQuote + 1, endQuote - startQuote - 1);

								// Convert forward slashes to backslashes if needed
								path = path.Replace('/', '\\');

								// Don't add duplicate paths
								if (Directory.Exists(path) && !list.Contains(path))
								{
									list.Add(path);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error reading Steam library folders: {ex.Message}");
				}
			}

			return list;
		}
		/// <summary>
		/// Gets the Steam installation directory from the registry
		/// </summary>
		/// <returns>The Steam installation directory or null if not found</returns>
		private static string GetSteamInstallPathFromRegistry()
		{
			try
			{
				// Try 64-bit registry first
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
				{
					if (key != null)
					{
						string installPath = key.GetValue("InstallPath") as string;
						if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
						{
							return installPath;
						}
					}
				}

				// Try 32-bit registry next
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
				{
					if (key != null)
					{
						string installPath = key.GetValue("InstallPath") as string;
						if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
						{
							return installPath;
						}
					}
				}

				// If not found in HKLM, try HKCU as a last resort
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
				{
					if (key != null)
					{
						string installPath = key.GetValue("SteamPath") as string;
						if (!string.IsNullOrEmpty(installPath))
						{
							// Convert forward slashes to backslashes if needed
							installPath = installPath.Replace('/', '\\');
							if (Directory.Exists(installPath))
							{
								return installPath;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading Steam registry: {ex.Message}");
			}

			return null;
		}
	}
}
