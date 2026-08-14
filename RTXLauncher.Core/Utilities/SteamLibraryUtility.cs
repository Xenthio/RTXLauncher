using Microsoft.Win32;
using System.Runtime.InteropServices; // <-- Required for OS platform detection
using System.Text;
using System.Text.RegularExpressions;

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
	public static string? GetGameInstallFolder(
		string gameFolderName,
		string? manualPath = null,
		string? steamRootOverride = null)
	{
		// Check the manually specified path first if it's valid.
		if (!string.IsNullOrWhiteSpace(manualPath) && Directory.Exists(manualPath))
		{
			return manualPath;
		}

		var steamLibraryPaths = GetSteamLibraryPaths(steamRootOverride);
		foreach (var path in steamLibraryPaths)
		{
			var libraryRoot = NormalizeLibraryRoot(path);
			var steamappsPath = Path.Combine(libraryRoot, "steamapps");

			// The directory name is normally the same as the requested Steam folder,
			// but Steam's app manifest is the authoritative source when it differs or
			// when a stale directory with the expected name is still present.
			var manifestInstallPath = FindGameInstallFromManifests(steamappsPath, gameFolderName);
			if (manifestInstallPath != null)
			{
				return manifestInstallPath;
			}

			// Fall back to the conventional folder name for older installations that
			// do not have a readable app manifest.
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
	public static List<string> GetSteamLibraryPaths(string? steamRootOverride = null)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return GetWindowsSteamLibraryPaths(steamRootOverride);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return GetLinuxSteamLibraryPaths(steamRootOverride);
		}

		// Return empty list for unsupported OS (e.g., macOS, in theory that would be ~/library/application support/steam but what mac has an rtx card, lol)
		return new List<string>();
	}

	// ==========================================================
	//                  LINUX IMPLEMENTATION
	// ==========================================================

	private static List<string> GetLinuxSteamLibraryPaths(string? steamRootOverride = null)
	{
		var paths = new List<string>();
		foreach (var steamRoot in GetLinuxSteamRoots(steamRootOverride))
		{
			AddUniquePath(paths, steamRoot);

			// Additional libraries are listed in libraryfolders.vdf.
			var libraryFoldersVdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
			if (!File.Exists(libraryFoldersVdfPath))
			{
				continue;
			}

			try
			{
				var content = File.ReadAllText(libraryFoldersVdfPath);
				foreach (var path in ParseLibraryFolderPaths(content))
				{
					if (Directory.Exists(path))
					{
						AddUniquePath(paths, path);
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

	public static string? GetSteamRoot(string? steamRootOverride = null)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return GetWindowsSteamInstallPathFromRegistry(steamRootOverride);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return GetLinuxSteamRoot(steamRootOverride);
		}
		return null;
	}

	/// <summary>
	/// Finds the root Steam directory on Linux by checking common locations.
	/// </summary>
	private static string? GetLinuxSteamRoot(string? steamRootOverride = null)
	{
		return GetLinuxSteamRoots(steamRootOverride).FirstOrDefault();
	}

	private static IEnumerable<string> GetLinuxSteamRoots(string? steamRootOverride)
	{
		var candidates = new List<string>();
		if (!string.IsNullOrWhiteSpace(steamRootOverride))
		{
			candidates.Add(steamRootOverride);
		}

		var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(homePath))
		{
			var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
			if (!string.IsNullOrWhiteSpace(xdgDataHome))
			{
				candidates.Add(Path.Combine(xdgDataHome, "Steam"));
			}

			candidates.Add(Path.Combine(homePath, ".local", "share", "Steam"));
			candidates.Add(Path.Combine(homePath, ".steam", "steam"));
			candidates.Add(Path.Combine(homePath, ".steam", "root"));
			candidates.Add(Path.Combine(homePath, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"));
		}

		candidates.Add("/usr/lib/steam");

		var roots = new List<string>();
		foreach (var candidate in candidates)
		{
			if (!Directory.Exists(candidate))
			{
				continue;
			}

			AddUniquePath(roots, NormalizeLibraryRoot(candidate));
		}

		return roots;
	}

	// ==========================================================
	//                  WINDOWS IMPLEMENTATION
	// ==========================================================

	private static List<string> GetWindowsSteamLibraryPaths(string? steamRootOverride = null)
	{
		var list = new List<string>();
		var steamPath = GetWindowsSteamInstallPathFromRegistry(steamRootOverride);

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
				var vdfContent = File.ReadAllText(libraryFoldersPath);
				foreach (var path in ParseLibraryFolderPaths(vdfContent))
				{
					var normalizedPath = path.Replace("\\\\", "\\");
					if (Directory.Exists(normalizedPath))
					{
						AddUniquePath(list, normalizedPath);
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
	private static string? GetWindowsSteamInstallPathFromRegistry(string? steamRootOverride = null)
	{
		if (!string.IsNullOrWhiteSpace(steamRootOverride) && Directory.Exists(steamRootOverride))
		{
			return NormalizeLibraryRoot(steamRootOverride);
		}

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

	private static string NormalizeLibraryRoot(string path)
	{
		var fullPath = Path.GetFullPath(path);
		var directory = new DirectoryInfo(fullPath);
		return directory.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase)
			? directory.Parent?.FullName ?? fullPath
			: fullPath;
	}

	private static void AddUniquePath(List<string> paths, string path)
	{
		var normalizedPath = NormalizeLibraryRoot(path);
		var comparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		if (!paths.Any(existing => string.Equals(existing, normalizedPath, comparison)))
		{
			paths.Add(normalizedPath);
		}
	}

	private static string? FindGameInstallFromManifests(string steamappsPath, string gameFolderName)
	{
		if (!Directory.Exists(steamappsPath))
		{
			return null;
		}

		try
		{
			foreach (var manifestPath in Directory.EnumerateFiles(
				steamappsPath, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
			{
				try
				{
					var manifest = File.ReadAllText(manifestPath);
					var installDirectory = ReadVdfValue(manifest, "installdir");
					if (string.IsNullOrWhiteSpace(installDirectory))
					{
						continue;
					}

					var displayName = ReadVdfValue(manifest, "name");
					if (!GameNamesMatch(gameFolderName, installDirectory) &&
						!GameNamesMatch(gameFolderName, displayName))
					{
						continue;
					}

					var installPath = Path.Combine(steamappsPath, "common", installDirectory);
					if (Directory.Exists(installPath))
					{
						return installPath;
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(
						$"Error reading Steam app manifest '{manifestPath}': {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(
				$"Error enumerating Steam app manifests in '{steamappsPath}': {ex.Message}");
		}

		return null;
	}

	internal static IReadOnlyList<string> ParseLibraryFolderPaths(string content)
	{
		var paths = new List<string>();
		var matches = Regex.Matches(
			content,
			@"""path""\s+""(?<value>(?:\\.|[^""\\])*)""",
			RegexOptions.CultureInvariant);

		foreach (Match match in matches)
		{
			var path = UnescapeVdfValue(match.Groups["value"].Value);
			if (!string.IsNullOrWhiteSpace(path))
			{
				paths.Add(path);
			}
		}

		return paths;
	}

	private static string? ReadVdfValue(string content, string key)
	{
		var pattern = $@"""{Regex.Escape(key)}""\s+""(?<value>(?:\\.|[^""\\])*)""";
		var match = Regex.Match(content, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		return match.Success
			? UnescapeVdfValue(match.Groups["value"].Value)
			: null;
	}

	private static string UnescapeVdfValue(string value)
	{
		var builder = new StringBuilder(value.Length);
		for (var index = 0; index < value.Length; index++)
		{
			if (value[index] != '\\' || index + 1 >= value.Length)
			{
				builder.Append(value[index]);
				continue;
			}

			var escaped = value[++index];
			builder.Append(escaped switch
			{
				'n' => '\n',
				'r' => '\r',
				't' => '\t',
				_ => escaped
			});
		}

		return builder.ToString();
	}

	private static bool GameNamesMatch(string expected, string? actual)
	{
		if (string.IsNullOrWhiteSpace(actual))
		{
			return false;
		}

		return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase) ||
			NormalizeGameName(expected) == NormalizeGameName(actual);
	}

	private static string NormalizeGameName(string value) =>
		new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
