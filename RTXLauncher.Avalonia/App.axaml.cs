using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using RTXLauncher.Avalonia.Themes;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Avalonia.ViewModels;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Linq;     // Add this if your MainWindow is in this namespace

namespace RTXLauncher.Avalonia;

public partial class App : Application
{

	private SettingsService _settingsService;
	private SettingsData _settingsData;

	public App()
	{
		// Initialize the settings service
		_settingsService = new SettingsService();
		_settingsData = _settingsService.LoadSettings();
	}

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		ThemeHelpers.InitializeVguiFonts();

		Resources.Add("VguiMarlettFont", ThemeHelpers.VguiMarlettFont);
		Resources.Add("VguiCheckBoxGlyph", ThemeHelpers.VguiCheckBoxGlyph);
		Resources.Add("VguiUpArrowGlyph", ThemeHelpers.VguiUpArrowGlyph);
		Resources.Add("VguiDownArrowGlyph", ThemeHelpers.VguiDownArrowGlyph);

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{

			LoadTheme(_settingsData.Theme);

			desktop.MainWindow = new MainWindow(new MainWindowViewModel(_settingsService, _settingsData));

		}

		base.OnFrameworkInitializationCompleted();
	}

	//public void LoadTheme(string themeName)
	//{
	//	if (Application.Current!.TryFindResource(themeName, out var theme) && theme is IStyle newThemeStyle)
	//	{
	//		var currentTheme = Styles.FirstOrDefault(s => s is FluentTheme || s is SimpleTheme);
	//		if (currentTheme != null)
	//		{
	//			var themeIndex = Styles.IndexOf(currentTheme);
	//			Styles[themeIndex] = newThemeStyle;
	//		}
	//		else
	//		{
	//			Styles.Add(newThemeStyle);
	//		}
	//	}
	//	else if (Application.Current!.TryFindResource("Fluent", out var defaultTheme) && defaultTheme is IStyle defaultThemeStyle)
	//	{
	//		Styles.Add(defaultThemeStyle);
	//	}
	//}


	public void LoadTheme(string themeName)
	{
		if (Application.Current == null) return;

		// --- THE CORE LOGIC CHANGE ---
		// Find the requested theme in our Application.Resources using its key.
		if (!Application.Current.TryFindResource(themeName, out var theme) || theme is not IStyle newThemeStyle)
		{
			// If the requested theme isn't found, gracefully fall back to Simple.
			if (!Application.Current.TryFindResource("Simple", out var simpleTheme) || simpleTheme is not IStyle simpleStyle)
			{
				return; // Catastrophic failure - no themes defined.
			}
			newThemeStyle = simpleStyle;
		}

		// Find the current theme style that's loaded in the application
		var currentTheme = Application.Current.Styles.FirstOrDefault(s => s is FluentTheme || s is SimpleTheme || s is VGUITheme);
		if (currentTheme != null)
		{
			// Replace the old theme with the new one.
			int themeIndex = Application.Current.Styles.IndexOf(currentTheme);
			Application.Current.Styles[themeIndex] = newThemeStyle;
		}
		else
		{
			// If for some reason no theme was loaded, add it.
			Application.Current.Styles.Add(newThemeStyle);
		}

		//// Redraw
		//if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		//{
		//	// Force the main window to redraw with the new theme
		//	if (desktop.MainWindow != null)
		//	{
		//		desktop.MainWindow.InvalidateVisual();
		//	}
		//}
	}

	public void RestartMainWindow()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var oldWindow = desktop.MainWindow;

			// Store the state of the old window
			var windowState = oldWindow?.WindowState ?? WindowState.Normal;
			var position = oldWindow?.Position;
			var size = new Size(oldWindow?.Width ?? 800, oldWindow?.Height ?? 600);

			LoadTheme(_settingsData.Theme);

			var newWindow = new MainWindow(new MainWindowViewModel(_settingsService, _settingsData));
			desktop.MainWindow = newWindow;

			// Apply the old window's state to the new one
			newWindow.WindowState = windowState;
			if (windowState == WindowState.Normal && position.HasValue)
			{
				newWindow.Position = position.Value;
				newWindow.Width = size.Width;
				newWindow.Height = size.Height;
			}

			newWindow.Show();
			oldWindow?.Close();
		}
	}
}