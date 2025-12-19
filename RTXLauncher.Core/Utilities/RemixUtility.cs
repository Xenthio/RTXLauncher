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

	/// <summary>
	/// Checks if rtx.conf exists in the installation directory.
	/// </summary>
	/// <param name="installPath">The installation directory path</param>
	/// <returns>True if rtx.conf exists, false otherwise</returns>
	public static bool RtxConfigExists(string installPath)
	{
		if (string.IsNullOrEmpty(installPath)) return false;
		return File.Exists(Path.Combine(installPath, "rtx.conf"));
	}

	/// <summary>
	/// Backs up rtx.conf to rtx.conf.backup_[timestamp]
	/// </summary>
	/// <param name="installPath">The installation directory path</param>
	/// <returns>The path to the backup file if successful, null otherwise</returns>
	public static string? BackupRtxConfig(string installPath)
	{
		if (string.IsNullOrEmpty(installPath)) return null;

		var configPath = Path.Combine(installPath, "rtx.conf");
		if (!File.Exists(configPath)) return null;

		try
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var backupPath = Path.Combine(installPath, $"rtx.conf.backup_{timestamp}");
			File.Copy(configPath, backupPath, true);
			return backupPath;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Gets the path where rtx.conf should be located for the current installation.
	/// </summary>
	/// <returns>The full path to rtx.conf</returns>
	public static string GetRtxConfigPath()
	{
		return Path.Combine(GarrysModUtility.GetThisInstallFolder(), "rtx.conf");
	}
}