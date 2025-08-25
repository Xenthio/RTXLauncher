using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RTXLauncher.Avalonia.Utilities
{
	/// <summary>
	/// Converts ProgressBar value to a width that snaps to 12px increments.
	/// </summary>
	public class ProgressBarWidthConverter : IMultiValueConverter
	{
		public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
		{
			if (values.Count < 4 ||
				values[0] is not double val ||
				values[1] is not double min ||
				values[2] is not double max ||
				values[3] is not double trackWidth)
			{
				return 0.0;
			}

			if (trackWidth <= 0 || val < min || max <= min)
			{
				return 0.0;
			}

			const double chunkWidth = 12.0; // 8px rectangle + 4px space

			// Calculate progress percentage
			var percent = (val - min) / (max - min);

			// Calculate the maximum number of chunks that can fit
			var maxChunks = Math.Floor(trackWidth / chunkWidth);

			// Calculate how many chunks to show based on progress
			var visibleChunks = Math.Floor(maxChunks * percent);

			// Return the snapped width
			return visibleChunks * chunkWidth;
		}
	}
}