// Services/GarrysModUtility.cs
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RTXLauncher.Core.Utilities;

public static class GarrysModUtility
{
	/// <summary>
	/// Gets the directory where the current launcher executable is located.
	/// </summary>
	public static string GetThisInstallFolder()
	{
		// Get the full path of the process executable (e.g., C:\MyFolder\RTXLauncher.WinForms.exe)
		string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

		// Get the directory of that executable
		return Path.GetDirectoryName(exePath) ?? "N/A";
	}

	/// <summary>
	/// Finds the vanilla Garry's Mod installation folder using Steam libraries.
	/// </summary>
	/// <param name="manualPath">An optional, user-specified path to check first.</param>
	public static string? GetVanillaInstallFolder(string? manualPath = null)
	{
		return SteamLibraryUtility.GetGameInstallFolder("GarrysMod", manualPath);
	}

	/// <summary>
	/// Determines the type of Garry's Mod installation at a given path by reading Steam's ACF manifest.
	/// Falls back to file-based detection if ACF cannot be read.
	/// </summary>
	/// <param name="path">The root directory of the installation to check.</param>
	/// <returns>A string identifier like "gmod_x86-64", "gmod_i386", "gmod_main", or "unknown".</returns>
	public static string GetInstallType(string? path)
	{
		if (string.IsNullOrEmpty(path)) return "unknown";

		if (!Directory.Exists(Path.Combine(path, "garrysmod")))
		{
			return "unknown";
		}

		// Try to detect from Steam ACF manifest first
		var acfType = GetInstallTypeFromSteamAcf(path);
		if (acfType != null)
		{
			return acfType;
		}

		// Fallback to file-based detection
		if (File.Exists(Path.Combine(path, "bin", "win64", "gmod.exe"))) return "gmod_x86-64";
		if (File.Exists(Path.Combine(path, "bin", "gmod.exe"))) return "gmod_i386";
		if (File.Exists(Path.Combine(path, "gmod.exe"))) return "gmod_main";
		if (File.Exists(Path.Combine(path, "hl2.exe"))) return "gmod_main-legacy";
		return "gmod_unknown";
	}

	/// <summary>
	/// Reads the Steam ACF manifest to determine the game version based on the BetaKey.
	/// </summary>
	/// <param name="installPath">The game installation path (e.g., F:\Steam\steamapps\common\GarrysMod)</param>
	/// <returns>"gmod_x86-64" if 64-bit, "gmod_i386" if 32-bit, or null if cannot be determined</returns>
	private static string? GetInstallTypeFromSteamAcf(string installPath)
	{
		try
		{
			// Navigate up from installPath to find the steamapps directory
			// installPath is typically: .../steamapps/common/GarrysMod
			var installDir = new DirectoryInfo(installPath);
			if (installDir.Parent?.Name != "common") return null;
			if (installDir.Parent.Parent?.Name != "steamapps") return null;

			var steamappsDir = installDir.Parent.Parent.FullName;
			var acfPath = Path.Combine(steamappsDir, "appmanifest_4000.acf");

			if (!File.Exists(acfPath)) return null;

			var acfContent = File.ReadAllText(acfPath);

			// Look for BetaKey in UserConfig or MountedConfig sections
			// Pattern: "BetaKey"		"value"
			var betaKeyMatch = Regex.Match(acfContent, @"""BetaKey""\s+""([^""]+)""");
			if (!betaKeyMatch.Success) return null;

			var betaKey = betaKeyMatch.Groups[1].Value;

			return betaKey switch
			{
				"x86-64" => "gmod_x86-64",
				"public" => "gmod_i386",
				_ => null
			};
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error reading Steam ACF manifest: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Get the path to the game executable (gmod.exe or hl2.exe) within the current installation.
	/// </summary>
	/// <returns>The full path to the executable, or null if not found.</returns>
	public static string? FindGameExecutable()
	{
		// Get the launcher's current directory from the centralized utility.
		var execPath = GarrysModUtility.GetThisInstallFolder();
		var candidates = new[]
		{
			Path.Combine(execPath, "patcherlauncher.exe"),
			Path.Combine(execPath, "bin", "win64", "gmod.exe"),
			Path.Combine(execPath, "bin", "gmod.exe"),
			Path.Combine(execPath, "gmod.exe"),
			Path.Combine(execPath, "hl2.exe")
		};
		return candidates.FirstOrDefault(File.Exists);
	}
}