using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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