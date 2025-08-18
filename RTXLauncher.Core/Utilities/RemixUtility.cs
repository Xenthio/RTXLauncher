// Services/RemixUtility.cs

namespace RTXLauncher.Core.Utilities;

public static class RemixUtility
{
	private static string GetInstallFolder()
	{
		string installPath = GarrysModUtility.GetThisInstallFolder();
		string installType = GarrysModUtility.GetInstallType(installPath);

		if (installType == "gmod_x86-64")
		{
			return Path.Combine(installPath, "bin", "win64");
		}
		// All other valid types (gmod_main, gmod_i386) use the root bin folder.
		// This also serves as a safe fallback.
		return Path.Combine(installPath, "bin");
	}

	public static bool IsInstalled()
	{
		var installFolder = GetInstallFolder();
		if (!Directory.Exists(installFolder)) return false;

		return File.Exists(Path.Combine(installFolder, "d3d9.dll")) ||
			   File.Exists(Path.Combine(installFolder, "d3d9.dll.disabled"));
	}

	public static bool IsEnabled()
	{
		var installFolder = GetInstallFolder();
		if (!Directory.Exists(installFolder)) return false;

		return File.Exists(Path.Combine(installFolder, "d3d9.dll"));
	}

	public static void SetEnabled(bool enabled)
	{
		if (!IsInstalled()) return;

		var installFolder = GetInstallFolder();
		var enabledPath = Path.Combine(installFolder, "d3d9.dll");
		var disabledPath = Path.Combine(installFolder, "d3d9.dll.disabled");

		try
		{
			if (enabled && !IsEnabled())
			{
				File.Move(disabledPath, enabledPath);
			}
			else if (!enabled && IsEnabled())
			{
				File.Move(enabledPath, disabledPath);
			}
		}
		catch (Exception ex)
		{
			// The utility throws an exception that the ViewModel must catch and display.
			throw new IOException($"Failed to change Remix state. Please check file permissions.", ex);
		}
	}
}