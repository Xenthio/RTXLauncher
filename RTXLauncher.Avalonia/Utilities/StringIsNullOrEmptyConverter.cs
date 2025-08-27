using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace RTXLauncher.Avalonia.Converters;

public class StringIsNullOrEmptyConverter : IValueConverter
{
	// This converter will return 'true' if the string is NOT null or empty,
	// and 'false' if it IS null or empty.
	// We can then bind a control's IsVisible property to this result.
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		// The 'value' is the data from our ViewModel (e.g., the Genre string)
		if (value is string str)
		{
			return !string.IsNullOrEmpty(str);
		}

		return false;
	}

	// ConvertBack is not needed for this scenario.
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}