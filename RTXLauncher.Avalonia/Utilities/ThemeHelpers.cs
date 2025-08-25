// Utilities/ThemeHelpers.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using RTXLauncher.Core.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RTXLauncher.Avalonia.Utilities;

public class ThemeHelpers : AvaloniaObject
{
	// ==========================================================
	//                  VGUI FONT INITIALIZATION
	// ==========================================================

	// Static properties to hold the loaded font and glyph
	public static FontFamily VguiMarlettFont { get; private set; } = new("avares://invalid"); // Default to an invalid font
	public static string VguiCheckBoxGlyph { get; private set; } = "a"; // Default Marlett glyph for a checkmark
	public static string VguiUpArrowGlyph { get; private set; } = "5";   // Default Marlett glyph for up arrow
	public static string VguiDownArrowGlyph { get; private set; } = "6"; // Default Marlett glyph for down arrow


	/// <summary>
	/// Initializes the fonts required by the VGUI theme.
	/// On Linux, it searches for Marlett in common Steam games.
	/// If not found, it falls back to a standard Unicode checkmark.
	/// </summary>
	public static void InitializeVguiFonts()
	{
		// On Windows, we assume Marlett is installed and just use it by name.
		if (OperatingSystem.IsWindows())
		{
			VguiMarlettFont = new FontFamily("Marlett");
			VguiCheckBoxGlyph = "a";
			VguiUpArrowGlyph = "5";
			VguiDownArrowGlyph = "6";
			return;
		}

		// On Linux, try to find the font file.
		var marlettPath = FindVguiFontOnLinux("marlett.ttf");

		if (marlettPath != null)
		{
			// If found, create a FontFamily pointing to the absolute file path.
			VguiMarlettFont = new FontFamily($"file://{marlettPath}");
			VguiCheckBoxGlyph = "a"; // Use the 'a' character from Marlett
			VguiUpArrowGlyph = "5";
			VguiDownArrowGlyph = "6";
		}
		else
		{
			// If not found, fallback to the default system UI font and a Unicode glyph.
			VguiMarlettFont = FontFamily.Default;
			VguiCheckBoxGlyph = "✔"; // Unicode checkmark
			VguiUpArrowGlyph = "▲";   // Unicode up arrow
			VguiDownArrowGlyph = "▼"; // Unicode down arrow
		}
	}

	/// <summary>
	/// Searches for a given font file in common Source Engine game directories.
	/// </summary>
	/// <param name="fontFileName">The name of the font file to find (e.g., "marlett.ttf").</param>
	/// <returns>The full path to the font file if found; otherwise, null.</returns>
	private static string? FindVguiFontOnLinux(string fontFileName)
	{
		// Game directories in steamapps/common to search within.
		var gameFolders = new[] { "Half-Life 2", "GarrysMod", "Half-Life" };

		// Relative paths inside a game folder where fonts are typically stored.
		var fontSubPaths = new[]
		{
			"hl2/resource",         // HL2
			"sourceengine/resource",// GMod
			"garrysmod/resource",   // GMod (older version)
			"valve/resource",       // HL1
			"platform/resource"     // Source SDK Base
		};

		foreach (var gameFolder in gameFolders)
		{
			var installPath = SteamLibraryUtility.GetGameInstallFolder(gameFolder);
			if (string.IsNullOrEmpty(installPath))
			{
				continue;
			}

			foreach (var subPath in fontSubPaths)
			{
				var fontFilePath = Path.Combine(installPath, subPath, fontFileName);
				if (File.Exists(fontFilePath))
				{
					return fontFilePath;
				}
			}
		}

		return null; // Font not found
	}

	// 1. Define the attached property.
	// This makes "DisableFontSmoothing" available in XAML.
	public static readonly AttachedProperty<bool> DisableFontSmoothingProperty =
		AvaloniaProperty.RegisterAttached<ThemeHelpers, Control, bool>("DisableFontSmoothing");

	// Define the standard get/set methods for the attached property.
	public static bool GetDisableFontSmoothing(Control element) =>
		element.GetValue(DisableFontSmoothingProperty);

	public static void SetDisableFontSmoothing(Control element, bool value) =>
		element.SetValue(DisableFontSmoothingProperty, value);

	// 2. This is the magic: a callback that runs when the property value changes.
	static ThemeHelpers()
	{
		DisableFontSmoothingProperty.Changed.AddClassHandler<Control>(OnDisableFontSmoothingChanged);
		UseCustomDecorationsProperty.Changed.AddClassHandler<Window>(OnUseCustomDecorationsChanged);
	}

	private static void OnDisableFontSmoothingChanged(Control control, AvaloniaPropertyChangedEventArgs e)
	{
		// if linux, do nothing
		if (OperatingSystem.IsLinux())
		{
			return;
		}

		// 3. When the property is set to True in a style, this code runs.
		// It sets the NON-STYLEABLE RenderOptions property on the control.
		if (e.NewValue is true)
		{
			RenderOptions.SetTextRenderingMode(control, TextRenderingMode.Alias);
		}
		else
		{
			RenderOptions.SetTextRenderingMode(control, TextRenderingMode.SubpixelAntialias); // Revert to default
		}
	}

	public static readonly AttachedProperty<bool> UseCustomDecorationsProperty =
		AvaloniaProperty.RegisterAttached<ThemeHelpers, Window, bool>("UseCustomDecorations");

	public static bool GetUseCustomDecorations(Window w) => w.GetValue(UseCustomDecorationsProperty);
	public static void SetUseCustomDecorations(Window w, bool v) => w.SetValue(UseCustomDecorationsProperty, v);

	// P/Invoke signature for the native DWM function
	[DllImport("dwmapi.dll", SetLastError = true)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

	private static void OnUseCustomDecorationsChanged(Window window, AvaloniaPropertyChangedEventArgs e)
	{
		if (OperatingSystem.IsLinux())
		{
			// Doesn't work :(
			return;
		}

		if (e.NewValue is true)
		{
			window.SystemDecorations = SystemDecorations.None;
			window.ExtendClientAreaToDecorationsHint = true;
			window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
			window.ExtendClientAreaTitleBarHeightHint = 32;

			// Subscribe to events to apply DWM changes.
			window.Opened += OnWindowOpened;
			window.PropertyChanged += OnWindowPropertyChanged;
		}
		else
		{
			// Revert to default OS decorations
			window.SystemDecorations = SystemDecorations.Full;
			window.ExtendClientAreaToDecorationsHint = false;
			window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
			window.ExtendClientAreaTitleBarHeightHint = -1;
			window.Opened -= OnWindowOpened;
			window.PropertyChanged -= OnWindowPropertyChanged;
		}
	}

	private static void OnWindowOpened(object? sender, EventArgs e)
	{
		if (sender is Window window)
		{
			Dispatcher.UIThread.Post(() => ApplyCustomChrome(window), DispatcherPriority.Loaded);
		}
	}

	private static void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == Window.WindowStateProperty && sender is Window window)
		{
			// Re-apply the attributes when the window is restored.
			if (window.WindowState != WindowState.Minimized)
			{
				// Post the action to the dispatcher to run after the OS has finished its own styling.
				Dispatcher.UIThread.Post(() => ApplyCustomChrome(window), DispatcherPriority.Loaded);
			}
		}
	}

	private static void ApplyCustomChrome(Window window)
	{
		if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle() is not { } platformHandle)
		{
			return;
		}

		var hwnd = platformHandle.Handle;

		// DWMWA_WINDOW_CORNER_PREFERENCE = 33
		// DWMWCP_DONOTROUND = 1
		int cornerPreference = 1;
		DwmSetWindowAttribute(hwnd, 33, ref cornerPreference, sizeof(int));

		// DWMWA_SYSTEMBACKDROP_TYPE = 38
		// DWMSBT_NONE = 1
		int backdropType = 1;
		DwmSetWindowAttribute(hwnd, 38, ref backdropType, sizeof(int));
	}
}