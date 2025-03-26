namespace RTXLauncher;

public class RemixSystem
{
	/// <summary>
	/// Find where our rtx remix is installed to (bin/ for gmod_main and bin/win64/ for gmod_win64)
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public static string GetInstallFolder()
	{
		string installPath = GarrysModInstallSystem.GetThisInstallFolder();
		string installType = GarrysModInstallSystem.GetInstallType(installPath);
		if (installType == "gmod_main")
		{
			return Path.Combine(installPath, "bin");
		}
		else if (installType == "gmod_win64")
		{
			return Path.Combine(installPath, "bin/win64");
		}
		else
		{
			return Path.Combine(installPath, "bin");
		}
	}

	public static bool Enabled
	{
		get => GetIsRemixEnabled();
		set
		{
			if (value)
			{
				EnableRemix();
			}
			else
			{
				DisableRemix();
			}
		}
	}

	public static bool Installed
	{
		get => GetIsRemixInstalled();
	}

	/// <summary>
	/// Check if RTX Remix is installed by checking if the d3d9.dll file exists
	/// </summary>
	/// <returns></returns>
	public static bool GetIsRemixInstalled()
	{
		string installPath = GetInstallFolder();
		// Check if the d3d9.dll file exists or d3d9.dll.disabled exists
		return File.Exists(Path.Combine(installPath, "d3d9.dll")) || File.Exists(Path.Combine(installPath, "d3d9.dll.disabled"));
	}

	public static bool GetIsRemixEnabled()
	{
		string installPath = GetInstallFolder();
		return File.Exists(Path.Combine(installPath, "d3d9.dll"));
	}

	/// <summary>
	/// Disables RTX Remix by renaming the d3d9.dll file to d3d9.dll.disabled
	/// </summary>
	public static void DisableRemix()
	{
		if (!GetIsRemixInstalled()) return;
		string installPath = GetInstallFolder();
		File.Move(Path.Combine(installPath, "d3d9.dll"), Path.Combine(installPath, "d3d9.dll.disabled"));
	}

	/// <summary>
	/// Enables RTX Remix by renaming the d3d9.dll.disabled file to d3d9.dll
	/// </summary>
	public static void EnableRemix()
	{
		if (!GetIsRemixInstalled()) return;
		string installPath = GetInstallFolder();
		File.Move(Path.Combine(installPath, "d3d9.dll.disabled"), Path.Combine(installPath, "d3d9.dll"));
	}
}
