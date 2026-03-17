using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.Utilities;

public static class DialogUtility
{
	public async static Task<bool> ShowConfirmationAsync(string title, string message)
	{
		var messageBox = MessageBoxManager.GetMessageBoxStandard(
			title,
			message,
			ButtonEnum.YesNo,
			Icon.Question
		);
		var result = await messageBox.ShowAsync();
		return result == ButtonResult.Yes;
	}
	public async static Task ShowErrorAsync(string title, string message)
	{
		var messageBox = MessageBoxManager.GetMessageBoxStandard(
			title,
			message,
			ButtonEnum.Ok,
			Icon.Error
		);
		await messageBox.ShowAsync();
	}

	public async static Task ShowMessageAsync(string title, string message, string buttonText = "OK")
	{
		var messageBox = MessageBoxManager.GetMessageBoxStandard(
			title,
			message,
			ButtonEnum.Ok,
			Icon.Warning
		);
		await messageBox.ShowAsync();
	}

	public async static Task<bool?> ShowBinaryChoiceAsync(string title, string message, string primaryButtonText, string secondaryButtonText)
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
		{
			return null;
		}

		bool? result = null;

		var primaryButton = new Button
		{
			Content = primaryButtonText,
			MinWidth = 100
		};
		var secondaryButton = new Button
		{
			Content = secondaryButtonText,
			MinWidth = 100
		};

		var dialog = new Window
		{
			Title = title,
			CanResize = false,
			ShowInTaskbar = false,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			SizeToContent = SizeToContent.WidthAndHeight,
			Content = new StackPanel
			{
				Margin = new Thickness(20),
				Spacing = 16,
				Children =
				{
					new TextBlock
					{
						Text = message,
						TextWrapping = TextWrapping.Wrap,
						MaxWidth = 420
					},
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 12,
						HorizontalAlignment = HorizontalAlignment.Center,
						Children =
						{
							primaryButton,
							secondaryButton
						}
					}
				}
			}
		};

		primaryButton.Click += (_, _) =>
		{
			result = true;
			dialog.Close();
		};

		secondaryButton.Click += (_, _) =>
		{
			result = false;
			dialog.Close();
		};

		await dialog.ShowDialog(desktop.MainWindow);
		return result;
	}

	/// <summary>
	/// Shows a file picker dialog and returns the selected file path.
	/// </summary>
	/// <param name="title">The title of the dialog</param>
	/// <param name="fileTypes">File type filters (e.g., new FilePickerFileType("Zip files") { Patterns = new[] { "*.zip" } })</param>
	/// <returns>The selected file path, or null if cancelled</returns>
	public async static Task<string?> ShowFilePickerAsync(string title, params FilePickerFileType[] fileTypes)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var mainWindow = desktop.MainWindow;
			if (mainWindow?.StorageProvider is { } storageProvider)
			{
				var options = new FilePickerOpenOptions
				{
					Title = title,
					AllowMultiple = false
				};

				if (fileTypes.Length > 0)
				{
					options.FileTypeFilter = fileTypes;
				}

				var files = await storageProvider.OpenFilePickerAsync(options);

				if (files.Count > 0)
				{
					return files[0].TryGetLocalPath();
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Shows a folder picker dialog and returns the selected folder path.
	/// </summary>
	/// <param name="title">The title of the dialog</param>
	/// <returns>The selected folder path, or null if cancelled</returns>
	public async static Task<string?> ShowFolderPickerAsync(string title)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var mainWindow = desktop.MainWindow;
			if (mainWindow?.StorageProvider is { } storageProvider)
			{
				var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
				{
					Title = title,
					AllowMultiple = false
				});

				if (folders.Count > 0)
				{
					return folders[0].TryGetLocalPath();
				}
			}
		}
		return null;
	}
}