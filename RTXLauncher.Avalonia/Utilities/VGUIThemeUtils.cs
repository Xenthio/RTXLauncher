using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace RTXLauncher.Avalonia.Utilities;

public class BoolToColorConverter : IValueConverter
{
	public static readonly BoolToColorConverter Instance = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool isFocused)
		{
			// Get the color resources from the application
			if (Application.Current?.TryFindResource("VguiTextActiveBrush", out var activeBrush) == true && activeBrush is IBrush active &&
				Application.Current?.TryFindResource("VguiTextLabelBrush", out var labelBrush) == true && labelBrush is IBrush label)
			{
				return isFocused ? active : label;
			}
		}

		// Fallback to label brush
		if (Application.Current?.TryFindResource("VguiTextLabelBrush", out var fallback) == true && fallback is IBrush fallbackBrush)
		{
			return fallbackBrush;
		}

		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}