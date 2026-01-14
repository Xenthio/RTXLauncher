using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RTXLauncher.Avalonia.ViewModels;
using System;
using System.Linq;

namespace RTXLauncher.Avalonia.Views;

public partial class SteamCredentialsWindow : Window
{
	public bool Result { get; private set; }

	public SteamCredentialsWindow()
	{
		InitializeComponent();
	}

	private async void ContinueButton_Click(object? sender, RoutedEventArgs e)
	{
		// Validate manifest ID if present
		if (DataContext is SteamCredentialsViewModel viewModel)
		{
			// Validate username and password for non-QR login
			if (!viewModel.UseQrCode)
			{
				if (string.IsNullOrWhiteSpace(viewModel.Username))
				{
					await Utilities.DialogUtility.ShowMessageAsync("Missing Username",
						"Please enter your Steam username.");
					return;
				}

				if (string.IsNullOrWhiteSpace(viewModel.Password))
				{
					await Utilities.DialogUtility.ShowMessageAsync("Missing Password",
						"Please enter your Steam password.\n\n" +
						"If you don't want to enter your password, use the QR code login option instead.");
					return;
				}
			}

			if (!string.IsNullOrWhiteSpace(viewModel.ManifestId))
			{
				// Check if manifest ID is numeric
				if (!viewModel.ManifestId.All(char.IsDigit))
				{
					await Utilities.DialogUtility.ShowMessageAsync("Invalid Manifest ID",
						"Manifest ID must contain only numbers.\n\n" +
						"Example: 2195078592256565401\n\n" +
						"Find valid manifest IDs at:\nhttps://steamdb.info/depot/4002/manifests/");
					return;
				}

				// Check if manifest ID is a reasonable length (Steam manifest IDs are typically 19 digits)
				if (viewModel.ManifestId.Length < 10 || viewModel.ManifestId.Length > 25)
				{
					await Utilities.DialogUtility.ShowMessageAsync("Invalid Manifest ID",
						"Manifest ID appears to be invalid (should be 19 digits).\n\n" +
						"Example: 2195078592256565401\n\n" +
						"Find valid manifest IDs at:\nhttps://steamdb.info/depot/4002/manifests/");
					return;
				}
			}
		}

		Result = true;
		Close();
	}

	private void CancelButton_Click(object? sender, RoutedEventArgs e)
	{
		Result = false;
		Close();
	}

	private void SteamDbLink_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		try
		{
			var url = "https://steamdb.info/depot/4002/manifests/";
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to open URL: {ex.Message}");
		}
	}
}
