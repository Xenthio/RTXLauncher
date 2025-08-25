// Utilities/ThemeHelpers.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;

namespace RTXLauncher.Avalonia.Utilities;

public class ThemeHelpers : AvaloniaObject
{
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
			ApplyCustomChrome(window);
		}
	}

	private static void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == Window.WindowStateProperty && sender is Window window)
		{
			// Re-apply the attributes when the window is restored.
			if (window.WindowState != WindowState.Minimized)
			{
				ApplyCustomChrome(window);
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