// Utilities/ThemeHelpers.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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
	}

	private static void OnDisableFontSmoothingChanged(Control control, AvaloniaPropertyChangedEventArgs e)
	{
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
}