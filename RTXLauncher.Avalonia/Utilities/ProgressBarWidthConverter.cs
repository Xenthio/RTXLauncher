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

			// ====================================================================
			//      CONTROL THE DELAY HERE
			// ====================================================================
			// This is the width of the invisible "gap" after the bar.
			// A smaller number means a shorter delay and a tighter loop.
			// Try a value like 48.0 for a quick loop, or 0.0 for an instant loop.
			const double loopPaddingWidth = 48.0;
			// ====================================================================

			// 1. Calculate the total distance the indicator must travel in one full loop.
			//    It's the visible track, plus the indicator's own width (to exit completely), plus our custom gap.
			double totalTravelDistance = trackWidth + indicatorWidth + loopPaddingWidth;

			// 2. Calculate the ideal SMOOTH position based on the animation progress.
			double smoothPosition = (progress * totalTravelDistance) - indicatorWidth;

			// 3. Snap the smooth position to the nearest segment boundary to create the VGUI effect.
			double snappedPosition = Math.Round(smoothPosition / SegmentWidth) * SegmentWidth;

			return snappedPosition;
		}

		return 0.0;
	}
}
