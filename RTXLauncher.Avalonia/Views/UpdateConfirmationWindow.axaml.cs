using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RTXLauncher.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RTXLauncher.Avalonia.Views;

public partial class UpdateConfirmationWindow : Window
{
	public bool Result { get; private set; }

	public UpdateConfirmationWindow()
	{
		InitializeComponent();
	}

	private void ProceedButton_Click(object? sender, RoutedEventArgs e)
	{
		Result = true;
		Close();
	}

	private void CancelButton_Click(object? sender, RoutedEventArgs e)
	{
		Result = false;
		Close();
	}
}

/// <summary>
/// Converter for Status column (IsNew -> "NEW" or "CHANGED")
/// </summary>
public class StatusToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool isNew)
		{
			return isNew ? "NEW" : "CHANGED";
		}
		return "UNKNOWN";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Converter for Type column (IsDirectory -> "Directory" or "File")
/// </summary>
public class TypeToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool isDirectory)
		{
			return isDirectory ? "Directory" : "File";
		}
		return "Unknown";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Converter for Status color (IsNew -> Green, otherwise Orange)
/// </summary>
public class StatusColorConverter : IMultiValueConverter
{
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count > 0 && values[0] is bool isNew)
		{
			return isNew ? Brushes.LightGreen : Brushes.Orange;
		}
		return Brushes.White;
	}
}

