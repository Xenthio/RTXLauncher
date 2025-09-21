using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class MountingViewModel : PageViewModel
{
	public ObservableCollection<MountableGameViewModel> MountableGames { get; } = new();
    private readonly MountingService _mountingService;
    private readonly IMessenger _messenger;

    public MountingViewModel(MountingService mountingService, IMessenger messenger)
	{
        _mountingService = mountingService;
        _messenger = messenger;
		Header = "Content Mounting";

		// Define your games here. To add a new game, just add a new line.
		MountableGames.Add(new MountableGameViewModel(
			name: "Half-Life 2: RTX",
			installFolder: "Half-Life 2 RTX",
			gameFolder: "hl2rtx",
			remixModFolder: "hl2rtx",
			mountingService, messenger));

		MountableGames.Add(new MountableGameViewModel(
			name: "Portal with RTX",
			installFolder: "PortalRTX",
			gameFolder: "portal_rtx",
			remixModFolder: "gameReadyAssets",
			mountingService, messenger));

		MountableGames.Add(new MountableGameViewModel(
			name: "Portal: Prelude RTX",
			installFolder: "Portal Prelude RTX",
			gameFolder: "prelude_rtx",
			remixModFolder: "gameReadyAssets",
			mountingService, messenger));

		MountableGames.Add(new MountableGameViewModel(
			name: "Portal 2 with RTX",
			installFolder: "Portal 2 With RTX",
			gameFolder: "portal2",
			remixModFolder: "portal2rtx",
			mountingService, messenger));

		MountableGames.Add(new MountableGameViewModel(
			name: "Dark Messiah RTX",
			installFolder: "Dark Messiah Might and Magic Single Player RTX",
			gameFolder: "mm",
			remixModFolder: "dmrtx",
			mountingService, messenger));
	}

    [RelayCommand]
    private async Task InstallUsdaFixes()
    {
        var confirmed = await RTXLauncher.Avalonia.Utilities.DialogUtility.ShowConfirmationAsync(
            "USDA Fixes",
            "Install USDA fixes for Half-Life 2 RTX into the mounted folder?"); // hardcoded for hl2rtx for now, will need to be updated for other games.
        if (!confirmed) return;

        var progress = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
        try
        {
            await _mountingService.ApplyHl2UsdaFixesAsync(progress);
        }
        catch (Exception ex)
        {
            _messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 }));
        }
    }
}