using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

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
			Application.EnableVisualStyles();
			Application.VisualStyleState = VisualStyleState.ClientAndNonClientAreasEnabled;

			// make everything look native
			Application.SetCompatibleTextRenderingDefault(false);

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

		static bool CheckDirectory(string path, ref string outpath)
		{
			var execPath = Path.GetDirectoryName(System.AppContext.BaseDirectory);//Assembly.GetExecutingAssembly().Location);

			var segs = path.Split("/");
			segs = segs.Prepend(execPath).ToArray();
			var testpath = Path.Combine(segs);
			if (File.Exists(testpath))
			{
				outpath = testpath;
				return true;
			}
			return false;
		}

		// FindGameDirectory implementation remains the same
		static string FindGameExecutable()
		{
			var execPath = Path.GetDirectoryName(System.AppContext.BaseDirectory);//Assembly.GetExecutingAssembly().Location);

			var dir = Path.Combine(execPath, "hl2.exe");

			if (CheckDirectory("patcherlauncher.exe", ref dir)) return dir;
			if (CheckDirectory("bin/win64/gmod.exe", ref dir)) return dir;
			if (CheckDirectory("bin/gmod.exe", ref dir)) return dir;
			if (CheckDirectory("gmod.exe", ref dir)) return dir;
			if (CheckDirectory("hl2.exe", ref dir)) return dir;

			return dir;
		}
	}
}