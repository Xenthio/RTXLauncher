// Utilities/ThemeHelpers.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

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

	private static void OnUseCustomDecorationsChanged(Window window, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.NewValue is true)
		{
			window.SystemDecorations = SystemDecorations.None;
			window.ExtendClientAreaToDecorationsHint = false;

		}
		else
		{
			// Revert to default OS decorations
			window.SystemDecorations = SystemDecorations.Full;
			window.ExtendClientAreaToDecorationsHint = false;
		}
	}
}