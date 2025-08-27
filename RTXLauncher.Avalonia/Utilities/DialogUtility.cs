using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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
}