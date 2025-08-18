// ViewModels/MountableGameViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class MountableGameViewModel : ViewModelBase
{
	private readonly MountingService _mountingService;
	private readonly IMessenger _messenger;

	// Configuration Properties
	public string Name { get; }
	public string InstallFolder { get; }
	public string GameFolder { get; }
	public string RemixModFolder { get; }

	// UI State Properties
	[ObservableProperty] private bool _isInstalled;
	[ObservableProperty] private bool _isBusy;

	public MountableGameViewModel(string name, string installFolder, string gameFolder, string remixModFolder, MountingService mountingService, IMessenger messenger)
	{
		Name = name;
		InstallFolder = installFolder;
		GameFolder = gameFolder;
		RemixModFolder = remixModFolder;
		_mountingService = mountingService;
		_messenger = messenger;

		IsInstalled = SteamLibraryUtility.GetGameInstallFolder(InstallFolder) != null;
	}

	// This is the property the CheckBox will bind to
	public bool IsMounted
	{
		get => IsInstalled && Directory.Exists(Path.Combine(GarrysModUtility.GetThisInstallFolder(), "garrysmod", "addons", $"mount-{GameFolder}"));
		set => _ = ToggleMountAsync(value); // Fire-and-forget async method
	}

	private async Task ToggleMountAsync(bool shouldMount)
	{
		if (IsBusy) return;

		// Prevent the UI from updating the checkbox until we are done
		OnPropertyChanged(nameof(IsMounted));

		IsBusy = true;

		try
		{
			if (shouldMount)
			{
				var confirmed = await DialogUtility.ShowConfirmationAsync("Confirm Mount", $"Are you sure you want to mount {Name}?");
				if (confirmed)
				{
					var progress = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
					await _mountingService.MountGameAsync(Name, GameFolder, InstallFolder, RemixModFolder, progress);
				}
			}
			else
			{
				var confirmed = await DialogUtility.ShowConfirmationAsync("Confirm Unmount", $"Are you sure you want to unmount {Name}?");
				if (confirmed)
				{
					_mountingService.UnmountGame(GameFolder, RemixModFolder);
					_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"{Name} unmounted successfully." }));
				}
			}
		}
		catch (Exception ex)
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 }));
		}
		finally
		{
			IsBusy = false;
			OnPropertyChanged(nameof(IsMounted)); // Notify UI of the final, correct state
		}
	}
}