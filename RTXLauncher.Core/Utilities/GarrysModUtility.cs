// Services/GarrysModUtility.cs
namespace RTXLauncher.Core.Utilities;

public static class GarrysModUtility
{
	/// <summary>
	/// Gets the directory where the current launcher executable is located.
	/// </summary>
	public static string GetThisInstallFolder()
	{
		return Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? "N/A";
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
}