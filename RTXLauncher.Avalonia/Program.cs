using Avalonia;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Linq;

namespace RTXLauncher.Avalonia;

internal class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		if (args.Contains("--skip-launcher", StringComparer.OrdinalIgnoreCase))
		{
			var settings = new SettingsService().LoadSettings();
			GarrysModUtility.UseLocalInstallPath = settings.UseLocalInstallPath;
			LauncherUtility.LaunchGame(settings, 0, 0);
			return;
		}

		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}
