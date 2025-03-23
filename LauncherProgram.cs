using System.Diagnostics;

namespace RTXLauncher
{
	internal static class LauncherProgram
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();
			Application.Run(new Form1());
		}

		public static void LaunchGameWithSettings(SettingsData settings)
		{
			// Launch the game with the specified settings

			//-console -dxlevel 90 +mat_disable_d3d9ex 1 -windowed -noborder

			var launchOptions = "";

			if (settings.ConsoleEnabled)
				launchOptions += " -console";

			launchOptions += $" -dxlevel {settings.DXLevel}";

			launchOptions += $" +mat_disable_d3d9ex 1";

			launchOptions += $" -windowed -noborder";

			launchOptions += $" -w {settings.Width}";
			launchOptions += $" -h {settings.Height}";

			if (!settings.LoadWorkshopAddons)
				launchOptions += " -noworkshop";

			if (settings.DisableChromium)
				launchOptions += " -nochromium";

			if (settings.DeveloperMode)
				launchOptions += " -dev";

			if (settings.ToolsMode)
				launchOptions += " -tools";


			launchOptions += settings.CustomLaunchOptions;

			var game = FindGameExecutable();

			// launch the game
			if (File.Exists(game))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = game,
					Arguments = launchOptions,
					WorkingDirectory = Path.GetDirectoryName(game)
				});
			}
			else
			{
				MessageBox.Show("Game executable not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

		}

		// FindGameDirectory implementation remains the same
		static string FindGameExecutable()
		{
			var execPath = Path.GetDirectoryName(System.AppContext.BaseDirectory);//Assembly.GetExecutingAssembly().Location);

			var patcherlauncher = Path.Combine(execPath, "patcherlauncher.exe");
			if (File.Exists(patcherlauncher))
			{
				return patcherlauncher;
			}

			var currentPath = Path.Combine(execPath, "bin", "win64");
			if (currentPath != null)
			{
				for (int i = 0; i < 3; i++)
				{
					var testPath = Path.Combine(currentPath, "gmod.exe");
					if (File.Exists(testPath))
					{
						return testPath;
					}
					// try up one directory
					currentPath = Path.GetDirectoryName(currentPath);
				}
			}
			return Path.Combine(execPath, "hl2.exe");
		}
	}
}