using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.Converters;

public class UrlToBitmapConverter : IValueConverter
{
	private static readonly HttpClient s_httpClient = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string url || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
		{
			return null;
		}

		// Return a Task<Bitmap?> which the binding system will handle.
		// This prevents blocking the UI thread while the image downloads.
		return Task.Run(async () =>
		{
			try
			{
				var response = await s_httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();
				await using var stream = await response.Content.ReadAsStreamAsync();
				return new Bitmap(stream);
			}
			catch (Exception)
			{
				// In case of any error, return a fallback image or null.
				// Here, we'll try to load a local asset as a fallback using the static AssetLoader.
				try
				{
					// Ensure you have a placeholder image at this path
					return new Bitmap(AssetLoader.Open(new Uri("avares://RTXLauncher.Avalonia/Assets/gmodrtx.png")));
				}
				catch
				{
					return null;
				}
			}
		});
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}