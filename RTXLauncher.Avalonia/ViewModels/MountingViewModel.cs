using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Services;
using System.Collections.ObjectModel;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class MountingViewModel : PageViewModel
{
	public ObservableCollection<MountableGameViewModel> MountableGames { get; } = new();

	public MountingViewModel(MountingService mountingService, IMessenger messenger)
	{
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
}