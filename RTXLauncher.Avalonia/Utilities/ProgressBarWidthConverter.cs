using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RTXLauncher.Avalonia.Utilities;

public class ProgressBarWidthConverter : IMultiValueConverter
{
	// The total width of one visual segment (8px block + 4px space)
	private const double SegmentWidth = 12.0;

	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count == 4 &&
			values[0] is double value &&
			values[1] is double minimum &&
			values[2] is double maximum &&
			values[3] is double trackWidth)
		{
			if (trackWidth <= 0 || maximum <= minimum)
			{
				return 0.0;
			}

			// Calculate the progress percentage
			double percent = (value - minimum) / (maximum - minimum);

			// Calculate the ideal pixel width based on percentage
			double pixelWidth = percent * trackWidth;

			// Calculate how many full segments fit within that width
			int segmentCount = (int)Math.Floor(pixelWidth / SegmentWidth);

			// Snap the final width to the edge of the last full segment
			return segmentCount * SegmentWidth;
		}

		return 0.0;
	}
}

/// <summary>
/// Converts an animation progress value (0.0 to 1.0) into a pixel offset for the indeterminate progress bar.
/// It calculates the position needed to make a fixed-width indicator travel across a variable-width track.
/// </summary>
public class IndeterminateProgressConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count == 3 &&
			values[0] is double progress &&      // The animated value from 0.0 to 1.0
			values[1] is double trackWidth &&    // The width of the container (PART_Track)
			values[2] is double indicatorWidth)  // The width of the indicator itself
		{
			if (trackWidth <= 0 || indicatorWidth <= 0)
			{
				return -indicatorWidth; // Start off-screen if track isn't rendered yet
			}

			// Calculate the total distance the indicator needs to travel.
			// It starts at -indicatorWidth (fully off-screen left) and ends at +trackWidth (fully off-screen right).
			double totalTravelDistance = trackWidth + indicatorWidth;

			// Calculate the current position based on the animation progress.
			double currentPosition = (progress * totalTravelDistance) - indicatorWidth;

			return currentPosition;
		}

		return 0.0;
	}
}
public static class AnimationProperties
{
	// 1. Define the attached property.
	// We'll call it "IndeterminateValue" and it will be of type 'double'.
	public static readonly AttachedProperty<double> IndeterminateValueProperty =
		AvaloniaProperty.RegisterAttached<ProgressBar, double>("IndeterminateValue", typeof(AnimationProperties));

	// 2. Create the standard "Get" and "Set" methods for the property.
	public static double GetIndeterminateValue(Control element)
	{
		return element.GetValue(IndeterminateValueProperty);
	}

	public static void SetIndeterminateValue(Control element, double value)
	{
		element.SetValue(IndeterminateValueProperty, value);
	}
}
/// <summary>
/// Converts an animation progress value (0.0 to 1.0) into a SNAPPED pixel offset.
/// This creates the classic VGUI "stepped" or "segmented" indeterminate progress bar effect.
/// </summary>
public class IndeterminateSnapConverter : IMultiValueConverter
{
	private const double SegmentWidth = 12.0; // The width of one visual segment (8px block + 4px space)

	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count == 3 &&
			values[0] is double progress &&      // The smooth animated value from 0.0 to 1.0
			values[1] is double trackWidth &&    // The width of the container (PART_Track)
			values[2] is double indicatorWidth)  // The width of the indicator itself
		{
			if (trackWidth <= 0 || indicatorWidth <= 0)
			{
				return -indicatorWidth; // Start off-screen
			}

			// 1. Calculate the IDEAL SMOOTH position, just like before.
			double totalTravelDistance = trackWidth + indicatorWidth;
			double smoothPosition = (progress * totalTravelDistance) - indicatorWidth;

			// 2. THIS IS THE MAGIC: Snap the smooth position to the nearest segment boundary.
			double snappedPosition = Math.Round(smoothPosition / SegmentWidth) * SegmentWidth;

			return snappedPosition;
		}

		return 0.0;
	}
}
