// Services/GarrysModUtility.cs
using System.Diagnostics;

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
	public static string? GetVanillaInstallFolder()
	{
		return SteamLibraryUtility.GetGameInstallFolder("GarrysMod");
	}

	/// <summary>
	/// Determines the type of Garry's Mod installation at a given path.
	/// </summary>
	/// <param name="path">The root directory of the installation to check.</param>
	/// <returns>A string identifier like "gmod_x86-64", "gmod_main", or "unknown".</returns>
	public static string GetInstallType(string? path)
	{
		if (string.IsNullOrEmpty(path)) return "unknown";

		if (Directory.Exists(Path.Combine(path, "garrysmod")))
		{
			if (File.Exists(Path.Combine(path, "bin", "win64", "gmod.exe"))) return "gmod_x86-64";
			if (File.Exists(Path.Combine(path, "bin", "gmod.exe"))) return "gmod_i386";
			if (File.Exists(Path.Combine(path, "gmod.exe"))) return "gmod_main";
			if (File.Exists(Path.Combine(path, "hl2.exe"))) return "gmod_main-legacy";
			return "gmod_unknown";
		}

		return "unknown";
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