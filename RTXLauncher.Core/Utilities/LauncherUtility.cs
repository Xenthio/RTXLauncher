using RTXLauncher.Core.Models;
using System.Diagnostics;

namespace RTXLauncher.Core.Utilities;

public static class LauncherUtility
{
	/// <summary>
	/// Opens the application's installation folder in the file explorer.
	/// </summary>
	public static void OpenInstallFolder()
	{
		try
		{
			var installPath = GarrysModUtility.GetThisInstallFolder();
			if (Directory.Exists(installPath))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = installPath,
					UseShellExecute = true, // This is important for opening folders
					Verb = "open"
				});
			}
		}
		catch (Exception ex)
		{
			// The ViewModel will be responsible for showing this error to the user.
			throw new Exception("Could not open the installation folder.", ex);
		}
	}

	/// <summary>
	/// Constructs the command-line arguments and launches the game executable.
	/// </summary>
	/// <param name="settings">The ViewModel containing all user settings.</param>
	/// <param name="screenWidth">The final screen width to use.</param>
	/// <param name="screenHeight">The final screen height to use.</param>
	public static void LaunchGame(SettingsData settings, int screenWidth, int screenHeight)
	{
		var gameExecutablePath = FindGameExecutable();
		if (string.IsNullOrEmpty(gameExecutablePath) || !File.Exists(gameExecutablePath))
		{
			throw new FileNotFoundException("Could not find a valid game executable (gmod.exe or hl2.exe) in the installation directory.");
		}

		var launchOptions = new List<string>();

		if (settings.ConsoleEnabled) launchOptions.Add("-console");

		launchOptions.Add($"-dxlevel {settings.DXLevel}");
		launchOptions.Add("-nod3d9ex");
		launchOptions.Add("-windowed");
		launchOptions.Add("-noborder");
		launchOptions.Add($"-w {screenWidth}");
		launchOptions.Add($"-h {screenHeight}");

		if (!settings.LoadWorkshopAddons) launchOptions.Add("-noworkshop");
		if (settings.DisableChromium) launchOptions.Add("-nochromium");
		if (settings.DeveloperMode) launchOptions.Add("-dev");
		if (settings.ToolsMode) launchOptions.Add("-tools");
		if (!string.IsNullOrWhiteSpace(settings.CustomLaunchOptions)) launchOptions.Add(settings.CustomLaunchOptions);

		var arguments = string.Join(" ", launchOptions);

		Process.Start(new ProcessStartInfo
		{
			FileName = gameExecutablePath,
			Arguments = arguments,
			WorkingDirectory = Path.GetDirectoryName(gameExecutablePath)
		});
	}

	private static string? FindGameExecutable()
	{
		var execPath = GarrysModUtility.GetThisInstallFolder();
		string? foundPath = null;

		if (CheckFile(Path.Combine(execPath, "patcherlauncher.exe"), ref foundPath)) return foundPath;
		if (CheckFile(Path.Combine(execPath, "bin", "win64", "gmod.exe"), ref foundPath)) return foundPath;
		if (CheckFile(Path.Combine(execPath, "bin", "gmod.exe"), ref foundPath)) return foundPath;
		if (CheckFile(Path.Combine(execPath, "gmod.exe"), ref foundPath)) return foundPath;
		if (CheckFile(Path.Combine(execPath, "hl2.exe"), ref foundPath)) return foundPath;

		return foundPath;
	}

	private static bool CheckFile(string path, ref string? outpath)
	{
		if (File.Exists(path))
		{
			outpath = path;
			return true;
		}
		return false;
	}
}