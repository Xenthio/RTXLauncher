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

		// --- Call the centralized SteamLibraryUtility with override support ---
		string? steamRoot = null;
		if (!string.IsNullOrEmpty(settings.LinuxSteamRootOverride))
		{
			steamRoot = settings.LinuxSteamRootOverride;
			if (!Directory.Exists(steamRoot))
			{
				throw new DirectoryNotFoundException($"Custom Steam root directory not found: {steamRoot}");
			}
		}
		else
		{
			steamRoot = SteamLibraryUtility.GetSteamRoot();
		}
		
		if (string.IsNullOrEmpty(steamRoot))
		{
			throw new DirectoryNotFoundException("Could not automatically detect the Steam root directory. Please check your Steam installation or specify a custom path in Linux Settings.");
		}

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

		// Set multiple Steam App ID environment variables for better compatibility
		processInfo.EnvironmentVariables["SteamAppId"] = "4000";
		processInfo.EnvironmentVariables["SteamAppID"] = "4000";
		processInfo.EnvironmentVariables["SteamGameId"] = "4000";
		processInfo.EnvironmentVariables["SteamOverlayGameId"] = "4000";

		// Optional Proton logging
		if (settings.LinuxEnableProtonLog)
		{
			processInfo.EnvironmentVariables["PROTON_LOG"] = "1";
		}
		
		// Vulkan driver selection
		if (!string.IsNullOrEmpty(settings.LinuxVulkanDriver) && settings.LinuxVulkanDriver != "Auto")
		{
			switch (settings.LinuxVulkanDriver)
			{
				case "AMDVLK":
					processInfo.EnvironmentVariables["VK_ICD_FILENAMES"] = "/usr/share/vulkan/icd.d/amd_icd64.json";
					break;
				case "RADV":
					processInfo.EnvironmentVariables["VK_ICD_FILENAMES"] = "/usr/share/vulkan/icd.d/radeon_icd.x86_64.json";
					break;
				case "Intel ANV":
					processInfo.EnvironmentVariables["VK_ICD_FILENAMES"] = "/usr/share/vulkan/icd.d/intel_icd.x86_64.json";
					break;
				case "NVIDIA":
					processInfo.EnvironmentVariables["VK_ICD_FILENAMES"] = "/usr/share/vulkan/icd.d/nvidia_icd.json";
					break;
			}
		}
		
		// Apply custom environment variables
		if (!string.IsNullOrEmpty(settings.LinuxCustomEnvironmentVariables))
		{
			ApplyCustomEnvironmentVariables(processInfo, settings.LinuxCustomEnvironmentVariables);
		}

		// Try to ensure Steam client is running for SteamAPI
		TryStartSteamClient();

		Process.Start(processInfo);
	}

	/// <summary>
	/// Shared method to build launch arguments for both platforms.
	/// </summary>
	private static List<string> BuildLaunchArgs(SettingsData settings, int width, int height, bool isLinux)
	{
		var args = new List<string>();

		// Console flag
		if (settings.ConsoleEnabled) args.Add("-console");

		// DirectX level - always enforce level 90 for Linux/Proton compatibility
		if (isLinux)
		{
			args.Add("-dxlevel");
			args.Add("90");
		}
		else
		{
			args.Add("-dxlevel");
			args.Add(settings.DXLevel.ToString());
		}

		// D3D9Ex disable and windowing flags
		args.Add("+mat_disable_d3d9ex");
		args.Add("1");
		args.Add("-nod3d9ex");
		args.Add("-windowed");
		args.Add("-noborder");

		// Resolution
		if (width > 0 && height > 0)
		{
			args.Add("-w");
			args.Add(width.ToString());
			args.Add("-h");
			args.Add(height.ToString());
		}

		// Game options
		if (!settings.LoadWorkshopAddons) args.Add("-noworkshop");
		if (settings.DisableChromium) args.Add("-nochromium");
		if (settings.DeveloperMode) args.Add("-dev");
		if (settings.ToolsMode) args.Add("-tools");

		// Custom launch options
		if (!string.IsNullOrWhiteSpace(settings.CustomLaunchOptions))
		{
			args.AddRange(SplitArgsQuoted(settings.CustomLaunchOptions));
		}

		return args;
	}

	/// <summary>
	/// Attempts to start the Steam client silently if not already running.
	/// This helps with SteamAPI initialization in Proton.
	/// </summary>
	private static void TryStartSteamClient()
	{
		try
		{
			// Check if steam is available in PATH
			var processInfo = new ProcessStartInfo
			{
				FileName = "steam",
				Arguments = "-silent",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			// Don't wait for the process, just try to start it
			Process.Start(processInfo);
		}
		catch
		{
			// Ignore errors - Steam might already be running or not in PATH
		}
	}

	/// <summary>
	/// Lists available Proton installations for user selection.
	/// Based on the Rust implementation's list_proton_builds function.
	/// </summary>
	public static List<(string Label, string Path)> ListProtonBuilds(SettingsData settings)
	{
		var results = new List<(string Label, string Path)>();
		var steamRoot = SteamLibraryUtility.GetSteamRoot();
		if (string.IsNullOrEmpty(steamRoot)) return results;

		// Official Proton from Steam common directory
		var steamCommonPath = Path.Combine(steamRoot, "steamapps", "common");
		if (Directory.Exists(steamCommonPath))
		{
			try
			{
				var officialProtonDirs = Directory.GetDirectories(steamCommonPath)
					.Where(dir => Path.GetFileName(dir).StartsWith("Proton ") || Path.GetFileName(dir).StartsWith("Proton - "))
					.Where(dir => File.Exists(Path.Combine(dir, "proton")));

				foreach (var dir in officialProtonDirs)
				{
					var label = Path.GetFileName(dir);
					var protonPath = Path.Combine(dir, "proton");
					results.Add((label, protonPath));
				}
			}
			catch { /* Ignore errors */ }
		}

		// Custom Proton installations (Proton-GE, etc.)
		var compatDirs = new List<string>
		{
			Path.Combine(steamRoot, "compatibilitytools.d")
		};

		var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(homePath))
		{
			compatDirs.AddRange(new[]
			{
				Path.Combine(homePath, ".local", "share", "Steam", "compatibilitytools.d"),
				Path.Combine(homePath, ".steam", "root", "compatibilitytools.d"),
				Path.Combine(homePath, ".steam", "steam", "compatibilitytools.d"),
				Path.Combine(homePath, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam", "compatibilitytools.d")
			});
		}

		foreach (var compatDir in compatDirs.Where(Directory.Exists))
		{
			try
			{
				var customProtonDirs = Directory.GetDirectories(compatDir)
					.Where(dir => File.Exists(Path.Combine(dir, "proton")));

				foreach (var dir in customProtonDirs)
				{
					var label = Path.GetFileName(dir);
					var protonPath = Path.Combine(dir, "proton");
					results.Add((label, protonPath));
				}
			}
			catch { /* Ignore errors */ }
		}

		// Remove duplicates by label, keeping first occurrence
		// This prevents showing the same Proton version multiple times if it exists in multiple directories
		var seen = new HashSet<string>();
		results = results.Where(item => seen.Add(item.Label)).ToList();

		return results;
	}

	private static string? DetectLinuxProton(SettingsData settings, string steamRoot)
	{
		// Check if user selected "Custom" and has a custom path
		if (settings.LinuxSelectedProtonLabel == "Custom" && !string.IsNullOrEmpty(settings.LinuxProtonPath))
		{
			if (File.Exists(settings.LinuxProtonPath))
			{
				return settings.LinuxProtonPath;
			}
			else
			{
				throw new FileNotFoundException($"Custom Proton path not found: {settings.LinuxProtonPath}");
			}
		}
		
		// Check if user has selected a specific Proton version by label
		if (!string.IsNullOrEmpty(settings.LinuxSelectedProtonLabel) && settings.LinuxSelectedProtonLabel != "Custom")
		{
			var availableBuilds = ListProtonBuilds(settings);
			var selectedBuild = availableBuilds.FirstOrDefault(build => build.Label == settings.LinuxSelectedProtonLabel);
			
			if (!string.IsNullOrEmpty(selectedBuild.Path) && File.Exists(selectedBuild.Path))
			{
				return selectedBuild.Path;
			}
			else
			{
				// Selected Proton version is no longer available, fall through to auto-detection
				Console.WriteLine($"Warning: Selected Proton version '{settings.LinuxSelectedProtonLabel}' not found. Falling back to auto-detection.");
			}
		}
		
		// Check user override path (for backwards compatibility)
		if (!string.IsNullOrEmpty(settings.LinuxProtonPath) && File.Exists(settings.LinuxProtonPath))
		{
			return settings.LinuxProtonPath;
		}

		var candidates = new List<string>();
		var steamCommonPath = Path.Combine(steamRoot, "steamapps", "common");

		// Official Proton installs from Steam
		if (Directory.Exists(steamCommonPath))
		{
			// Check for specific official Proton versions
			candidates.Add(Path.Combine(steamCommonPath, "Proton - Experimental", "proton"));
			candidates.Add(Path.Combine(steamCommonPath, "Proton - Hotfix", "proton"));

			// Check for numbered Proton versions (e.g., "Proton 9.0")
			try
			{
				var protonDirs = Directory.GetDirectories(steamCommonPath, "Proton *")
					.Where(dir => File.Exists(Path.Combine(dir, "proton")))
					.Select(dir => Path.Combine(dir, "proton"));
				candidates.AddRange(protonDirs);
			}
			catch { /* Ignore directory access errors */ }
		}

		// Check compatibilitytools.d directories for custom Proton (e.g., Proton-GE)
		var compatDirs = new List<string>
		{
			Path.Combine(steamRoot, "compatibilitytools.d"),
		};

		// Also check common user paths for compatibilitytools.d
		var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(homePath))
		{
			compatDirs.AddRange(new[]
			{
				Path.Combine(homePath, ".local", "share", "Steam", "compatibilitytools.d"),
				Path.Combine(homePath, ".steam", "root", "compatibilitytools.d"),
				Path.Combine(homePath, ".steam", "steam", "compatibilitytools.d"),
				Path.Combine(homePath, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam", "compatibilitytools.d")
			});
		}

		foreach (var compatDir in compatDirs.Where(Directory.Exists))
		{
			try
			{
				var protonInstalls = Directory.GetDirectories(compatDir)
					.Select(dir => Path.Combine(dir, "proton"))
					.Where(File.Exists);
				candidates.AddRange(protonInstalls);
			}
			catch { /* Ignore directory access errors */ }
		}

		// Find the first working Proton executable, preferring newer versions
		return candidates
			.Where(File.Exists)
			.OrderByDescending(p => new FileInfo(p).LastWriteTime)
			.FirstOrDefault();
	}

	/// <summary>
	/// Parses and applies custom environment variables from a string.
	/// Format: "VAR1=value1 VAR2=value2" (space-separated key=value pairs)
	/// Supports quoted values with spaces: VAR="value with spaces"
	/// </summary>
	private static void ApplyCustomEnvironmentVariables(ProcessStartInfo processInfo, string customEnvVars)
	{
		if (string.IsNullOrWhiteSpace(customEnvVars))
			return;

		var parts = new List<string>();
		var currentPart = new StringBuilder();
		bool inQuotes = false;

		// Parse the string, respecting quotes
		foreach (char c in customEnvVars)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
			}
			else if (char.IsWhiteSpace(c) && !inQuotes)
			{
				if (currentPart.Length > 0)
				{
					parts.Add(currentPart.ToString());
					currentPart.Clear();
				}
			}
			else
			{
				currentPart.Append(c);
			}
		}

		// Add the last part if any
		if (currentPart.Length > 0)
		{
			parts.Add(currentPart.ToString());
		}

		// Process each VAR=value pair
		foreach (var part in parts)
		{
			var equalsIndex = part.IndexOf('=');
			if (equalsIndex > 0 && equalsIndex < part.Length - 1)
			{
				var key = part.Substring(0, equalsIndex).Trim();
				var value = part.Substring(equalsIndex + 1).Trim();

				if (!string.IsNullOrEmpty(key))
				{
					try
					{
						// Set or override the environment variable
						processInfo.EnvironmentVariables[key] = value;
						Console.WriteLine($"Set custom environment variable: {key}={value}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Warning: Failed to set environment variable '{key}': {ex.Message}");
					}
				}
			}
			else if (!string.IsNullOrWhiteSpace(part))
			{
				Console.WriteLine($"Warning: Invalid environment variable format (expected KEY=value): {part}");
			}
		}
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

	public static readonly List<string> CommonResolutions = new()
	{
		"3840x2160",
		"3440x1440",
		"2560x1600",
		"2560x1440",
		"2560x1080",
		"1920x1200",
		"1920x1080",
		"1680x1050",
		"1600x900",
		"1440x900",
		"1366x768",
		"1280x800",
		"1280x720"
	};

}