using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RTXLauncher.Core.Utilities;

public static class LauncherUtility
{
	/// <summary>
	/// Opens the application's installation folder in the file explorer.
	/// This reuses logic from GarrysModUtility to find the correct path.
	/// </summary>
	public static void OpenInstallFolder()
	{
		try
		{
			// REUSE: Get the launcher's current directory from the centralized utility.
			var installPath = GarrysModUtility.GetThisInstallFolder();
			if (!Directory.Exists(installPath)) return;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Process.Start(new ProcessStartInfo { FileName = installPath, UseShellExecute = true, Verb = "open" });
			}
			else // For Linux and potentially macOS
			{
				Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = installPath });
			}
		}
		catch (Exception ex)
		{
			throw new Exception("Could not open the installation folder.", ex);
		}
	}

	/// <summary>
	/// Constructs the command-line arguments and launches the game executable,
	/// adapting the launch method based on the operating system.
	/// </summary>
	public static void LaunchGame(SettingsData settings, int screenWidth, int screenHeight)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			LaunchGameWindows(settings, screenWidth, screenHeight);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			LaunchGameLinux(settings, screenWidth, screenHeight);
		}
		else
		{
			throw new PlatformNotSupportedException("Your operating system is not supported for launching the game.");
		}
	}

	/// <summary>
	/// Launch logic specific to Windows.
	/// </summary>
	private static void LaunchGameWindows(SettingsData settings, int screenWidth, int screenHeight)
	{
		var gameExecutablePath = GarrysModUtility.FindGameExecutable();
		if (string.IsNullOrEmpty(gameExecutablePath))
		{
			throw new FileNotFoundException("Could not find a valid game executable (gmod.exe or hl2.exe) in the installation directory.");
		}

		var launchOptions = BuildLaunchArgs(settings, screenWidth, screenHeight, isLinux: false);

		Process.Start(new ProcessStartInfo
		{
			FileName = gameExecutablePath,
			Arguments = string.Join(" ", launchOptions),
			WorkingDirectory = Path.GetDirectoryName(gameExecutablePath)
		});
	}

	/// <summary>
	/// Launch logic specific to Linux, using Proton.
	/// </summary>
	private static void LaunchGameLinux(SettingsData settings, int screenWidth, int screenHeight)
	{
		var gameExecutablePath = GarrysModUtility.FindGameExecutable();
		if (string.IsNullOrEmpty(gameExecutablePath))
		{
			throw new FileNotFoundException("Could not find a valid Windows game executable (gmod.exe or hl2.exe). This is required to run under Proton.");
		}

		var gameDirectory = Path.GetDirectoryName(gameExecutablePath)
			?? throw new DirectoryNotFoundException("Could not determine the game's parent directory.");

		// --- Call the centralized SteamLibraryUtility ---
		var steamRoot = SteamLibraryUtility.GetSteamRoot()
			?? throw new DirectoryNotFoundException("Could not automatically detect the Steam root directory. Please check your Steam installation.");

		var protonPath = DetectLinuxProton(settings, steamRoot)
			?? throw new FileNotFoundException("Could not find a compatible Proton executable. Please ensure Proton is installed via Steam.");

		// Build the game arguments using the shared method
		var launchOptions = BuildLaunchArgs(settings, screenWidth, screenHeight, isLinux: true);

		// Ensure the compat data directory exists for GMod (AppID 4000)
		var compatDataPath = Path.Combine(steamRoot, "steamapps", "compatdata", "4000");
		Directory.CreateDirectory(compatDataPath);

		// Write steam_appid.txt to satisfy SteamAPI
		File.WriteAllText(Path.Combine(gameDirectory, "steam_appid.txt"), "4000");

		var processInfo = new ProcessStartInfo
		{
			FileName = protonPath,
			WorkingDirectory = gameDirectory,
			UseShellExecute = false // Required to set environment variables
		};

		// Add arguments for Proton: proton run <game_executable> <game_args>
		processInfo.ArgumentList.Add("run");
		processInfo.ArgumentList.Add(gameExecutablePath);
		foreach (var arg in launchOptions)
		{
			// The argument list handles spaces and quotes correctly, so we can add complex args directly.
			processInfo.ArgumentList.Add(arg);
		}

		// Set environment variables required by Proton and Steam
		processInfo.EnvironmentVariables["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = steamRoot;
		processInfo.EnvironmentVariables["STEAM_COMPAT_DATA_PATH"] = compatDataPath;
		processInfo.EnvironmentVariables["WINEDLLOVERRIDES"] = "d3d9=n,b"; // CRITICAL: Use native d3d9 for Remix
		processInfo.EnvironmentVariables["SteamAppId"] = "4000";
		processInfo.EnvironmentVariables["SteamGameId"] = "4000";
		// ToDo: Add a setting for this in SettingsDataS
		// if (settings.LinuxEnableProtonLog) processInfo.EnvironmentVariables["PROTON_LOG"] = "1";

		Process.Start(processInfo);
	}

	/// <summary>
	/// Shared method to build launch arguments for both platforms.
	/// </summary>
	private static List<string> BuildLaunchArgs(SettingsData settings, int width, int height, bool isLinux)
	{
		var args = new List<string>();

		if (isLinux)
		{
			// Linux/Proton requires specific DX9 settings for Remix to work reliably.
			args.Add("-dxlevel 90");
			args.Add("+mat_disable_d3d9ex 1");
		}
		else
		{
			args.Add($"-dxlevel {settings.DXLevel}");
		}

		args.Add("-nod3d9ex");
		args.Add("-windowed");
		args.Add("-noborder");
		args.Add($"-w {width}");
		args.Add($"-h {height}");

		if (settings.ConsoleEnabled) args.Add("-console");
		if (!settings.LoadWorkshopAddons) args.Add("-noworkshop");
		if (settings.DisableChromium) args.Add("-nochromium");
		if (settings.DeveloperMode) args.Add("-dev");
		if (settings.ToolsMode) args.Add("-tools");
		if (!string.IsNullOrWhiteSpace(settings.CustomLaunchOptions))
		{
			args.AddRange(SplitArgsQuoted(settings.CustomLaunchOptions));
		}

		return args;
	}

	private static string? DetectLinuxProton(SettingsData settings, string steamRoot)
	{
		// ToDo: Add a user-override setting for the Proton path in SettingsData
		// if (!string.IsNullOrEmpty(settings.LinuxProtonPath) && File.Exists(settings.LinuxProtonPath))
		// {
		//     return settings.LinuxProtonPath;
		// }

		var searchPaths = new List<string>();
		var steamCommonPath = Path.Combine(steamRoot, "steamapps", "common");

		if (Directory.Exists(steamCommonPath))
		{
			searchPaths.AddRange(Directory.GetDirectories(steamCommonPath, "Proton*"));
		}

		// Use SteamLibraryUtility to find all library folders for custom Proton installs (e.g., Proton-GE)
		var allLibraries = SteamLibraryUtility.GetSteamLibraryPaths();
		foreach (var libraryPath in allLibraries)
		{
			var compatToolDir = Path.Combine(libraryPath, "steamapps", "compatibilitytools.d");
			if (Directory.Exists(compatToolDir))
			{
				searchPaths.AddRange(Directory.GetDirectories(compatToolDir));
			}
		}

		// Find the 'proton' executable within the found directories, preferring newer versions
		return searchPaths
			.Select(dir => Path.Combine(dir, "proton"))
			.Where(File.Exists)
			.OrderByDescending(f => new DirectoryInfo(Path.GetDirectoryName(f)!).LastWriteTime) // Sort by folder date
			.FirstOrDefault();
	}

	private static IEnumerable<string> SplitArgsQuoted(string? commandLine)
	{
		if (string.IsNullOrWhiteSpace(commandLine)) yield break;
		var sb = new StringBuilder();
		bool inQuotes = false;
		foreach (char c in commandLine)
		{
			if (c == '\"') inQuotes = !inQuotes;
			else if (char.IsWhiteSpace(c) && !inQuotes)
			{
				if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
			}
			else sb.Append(c);
		}
		if (sb.Length > 0) yield return sb.ToString();
	}

}