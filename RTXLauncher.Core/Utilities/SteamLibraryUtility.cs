using Microsoft.Win32;

namespace RTXLauncher.Core.Utilities;

public static class SteamLibraryUtility
{
	/// <summary>
	/// Finds a game's installation folder by searching all Steam libraries.
	/// </summary>
	/// <param name="gameFolderName">The folder name of the game in steamapps/common (e.g., "GarrysMod").</param>
	/// <param name="manualPath">An optional, user-specified path to check first.</param>
	/// <returns>The full path to the game's installation folder, or null if not found.</returns>
	public static string? GetGameInstallFolder(string gameFolderName, string? manualPath = null)
	{
		// Check the manually specified path first if it's valid.
		if (!string.IsNullOrWhiteSpace(manualPath) && Directory.Exists(manualPath))
		{
			return manualPath;
		}

		var steamLibraryPaths = GetSteamLibraryPaths();
		foreach (var path in steamLibraryPaths)
		{
			var installPath = Path.Combine(path, "steamapps", "common", gameFolderName);
			if (Directory.Exists(installPath))
			{
				return installPath;
			}
		}
		return null;
	}

	// This logic is mostly unchanged, just made non-static.
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
				//System.Diagnostics.Debug.WriteLine($"Found libraryfolders.vdf: {libraryFoldersPath}");
				var libraryFolders = File.ReadAllLines(libraryFoldersPath);
				foreach (var line in libraryFolders)
				{
					if (line.Contains("\"path\""))
					{
						//System.Diagnostics.Debug.WriteLine($"Found line in libraryfolders.vdf: {line}");
						var cleanline = line.Replace("\"path\"", "");
						// Extract path between quotes
						var startQuote = cleanline.IndexOf('"');
						var endQuote = cleanline.IndexOf('"', startQuote + 1);

						if (startQuote >= 0 && endQuote > startQuote)
						{
							var path = cleanline.Substring(startQuote + 1, endQuote - startQuote - 1);

							// Convert forward slashes to backslashes if needed
							path = path.Replace('/', '\\');


							path = path.Replace("\\\\", "\\");

							// Don't add duplicate paths
							if (Directory.Exists(path) && !list.Contains(path))
							{
								//System.Diagnostics.Debug.WriteLine($"Found path in libraryfolders.vdf: {path}");
								list.Add(path);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error reading Steam library folders: {ex.Message}");
			}
		}

		return list;
	}

	/// <summary>
	/// Gets the Steam installation directory from the registry
	/// </summary>
	/// <returns>The Steam installation directory or null if not found</returns>
	public static string GetSteamInstallPathFromRegistry()
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
			System.Diagnostics.Debug.WriteLine($"Error reading Steam registry: {ex.Message}");
		}

		return null;
	}
}
