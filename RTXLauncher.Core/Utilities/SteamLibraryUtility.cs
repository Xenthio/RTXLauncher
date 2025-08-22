using Microsoft.Win32;
using System.Runtime.InteropServices; // <-- Required for OS platform detection

namespace RTXLauncher.Core.Utilities;

public static class SteamLibraryUtility
{
	/// <summary>
	/// Finds a game's installation folder by searching all Steam libraries.
	/// This method is cross-platform.
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
			// On Linux, the main library path is the steam root, so we need to add steamapps to it.
			// For other libraries from libraryfolders.vdf, the path already includes steamapps.
			// A simple check handles both cases.
			var steamappsPath = path.EndsWith("steamapps") ? path : Path.Combine(path, "steamapps");
			var installPath = Path.Combine(steamappsPath, "common", gameFolderName);
			if (Directory.Exists(installPath))
			{
				return installPath;
			}
		}
		return null;
	}

	/// <summary>
	/// Gets all Steam library paths for the current operating system.
	/// </summary>
	public static List<string> GetSteamLibraryPaths()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return GetWindowsSteamLibraryPaths();
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return GetLinuxSteamLibraryPaths();
		}

		// Return empty list for unsupported OS (e.g., macOS, in theory that would be ~/library/application support/steam but what mac has an rtx card, lol)
		return new List<string>();
	}

	// ==========================================================
	//                  LINUX IMPLEMENTATION
	// ==========================================================

	private static List<string> GetLinuxSteamLibraryPaths()
	{
		var paths = new List<string>();
		var steamRoot = GetLinuxSteamRoot();

		if (string.IsNullOrEmpty(steamRoot) || !Directory.Exists(steamRoot))
		{
			return paths;
		}

		// The main library is inside the Steam root directory.
		// We add the root path itself, as GetGameInstallFolder will append "steamapps".
		paths.Add(steamRoot);

		// Additional libraries are listed in libraryfolders.vdf
		var libraryFoldersVdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");

		if (File.Exists(libraryFoldersVdfPath))
		{
			try
			{
				var vdfLines = File.ReadAllLines(libraryFoldersVdfPath);
				// The parsing logic is the same as on Windows
				foreach (var line in vdfLines)
				{
					var trimmedLine = line.Trim();
					if (trimmedLine.StartsWith("\"path\""))
					{
						var parts = trimmedLine.Split('"', StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length > 1 && parts[0] == "path")
						{
							var path = parts[1];
							if (Directory.Exists(path) && !paths.Contains(path))
							{
								paths.Add(path);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error reading Linux libraryfolders.vdf: {ex.Message}");
			}
		}

		return paths;
	}

	/// <summary>
	/// Finds the root Steam directory on Linux by checking common locations.
	/// </summary>
	private static string? GetLinuxSteamRoot()
	{
		var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (string.IsNullOrEmpty(homePath)) return null;

		// List of common relative paths for Steam on Linux
		var potentialPaths = new[]
		{
			Path.Combine(homePath, ".local", "share", "Steam"),
			Path.Combine(homePath, ".steam", "steam"),
			Path.Combine(homePath, ".var", "app", "com.valvesoftware.Steam", "data", "Steam") // Flatpak path
        };

		// Return the first path that exists
		return potentialPaths.FirstOrDefault(Directory.Exists);
	}

	// ==========================================================
	//                  WINDOWS IMPLEMENTATION
	// ==========================================================

	private static List<string> GetWindowsSteamLibraryPaths()
	{
		var list = new List<string>();
		var steamPath = GetWindowsSteamInstallPathFromRegistry();

		if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
		{
			return list;
		}

		list.Add(steamPath);

		var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
		if (File.Exists(libraryFoldersPath))
		{
			try
			{
				var vdfLines = File.ReadAllLines(libraryFoldersPath);
				foreach (var line in vdfLines)
				{
					// Using a more robust regex to extract the path
					var match = System.Text.RegularExpressions.Regex.Match(line, @"^\s*""path""\s*""(.+)""\s*$");
					if (match.Success)
					{
						var path = match.Groups[1].Value.Replace("\\\\", "\\");
						if (Directory.Exists(path) && !list.Contains(path))
						{
							list.Add(path);
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error reading Windows libraryfolders.vdf: {ex.Message}");
			}
		}

		return list;
	}

	/// <summary>
	/// Gets the Steam installation directory from the Windows Registry.
	/// </summary>
	private static string? GetWindowsSteamInstallPathFromRegistry()
	{
		// This method should only be called on Windows.
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;

		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
						 ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");

			if (key?.GetValue("InstallPath") is string installPath && !string.IsNullOrEmpty(installPath))
			{
				return installPath;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error reading Steam registry: {ex.Message}");
		}

		return null;
	}
}