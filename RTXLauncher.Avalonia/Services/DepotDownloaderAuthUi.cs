using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.ViewModels;
using RTXLauncher.Avalonia.Views;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.Services;

public sealed class DepotDownloaderAuthUi : IDepotDownloaderAuthUi
{
    private readonly IMessenger _messenger;

    public DepotDownloaderAuthUi(IMessenger messenger)
    {
        _messenger = messenger;
    }

    public Task<string?> RequestTwoFactorCodeAsync(bool previousIncorrect)
    {
        var message = previousIncorrect
            ? "The previous 2FA code was incorrect. Enter a new Steam Guard code."
            : "Enter the Steam Guard code from your authenticator app.";
        return PromptForCodeAsync("Steam Guard", message);
    }

    public Task<string?> RequestEmailCodeAsync(string emailHint, bool previousIncorrect)
    {
        var message = previousIncorrect
            ? $"The previous code was incorrect. Enter the code sent to {emailHint}."
            : $"Enter the Steam Guard code sent to {emailHint}.";
        return PromptForCodeAsync("Steam Guard Email", message);
    }

    public async Task<bool> RequestDeviceConfirmationAsync()
    {
        var confirmed = await Utilities.DialogUtility.ShowConfirmationAsync(
            "Steam Guard Confirmation",
            "Confirm the login in the Steam mobile app, then click Continue.");
        return confirmed;
    }

    public async Task ShowQrCodeAsync(string challengeUrl)
    {
        await Utilities.DialogUtility.ShowMessageAsync(
            "Steam QR Login",
            "Scan this QR code using the Steam mobile app:\n\n" + challengeUrl);
    }

    public void LogOutput(string message, bool isError)
    {
        _messenger.Send(new ProgressReportMessage(new InstallProgressReport
        {
            Message = message,
            Percentage = 30
        }));
    }

    private static async Task<string?> PromptForCodeAsync(string title, string message)
    {
        var mainWindow = App.GetMainWindow();
        if (mainWindow == null)
        {
            return null;
        }

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var viewModel = new SteamAuthCodeViewModel
            {
                Title = title,
                Message = message,
                IsPassword = false
            };
            var dialog = new SteamAuthCodeWindow
            {
                DataContext = viewModel
            };

            await dialog.ShowDialog(mainWindow);

            return dialog.Result ? viewModel.Code : null;
        });
    }
}
