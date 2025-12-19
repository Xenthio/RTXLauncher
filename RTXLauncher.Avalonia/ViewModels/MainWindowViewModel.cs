// ViewModels/MainWindowViewModel.cs
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace RTXLauncher.Avalonia.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	private readonly IMessenger _messenger;
	private readonly IModService _modBrowserService; // Keep a reference to the service
	private readonly ModsViewModel _modsPageInstance; // Store the ModsViewModel instance
	private readonly AddonInstallService _addonInstallService; // Store the ModsViewModel instance

	// --- NEW: Properties for the Top Progress Bar ---

	[ObservableProperty]
	private int _progressValue;

	// The full log for the expandable view
	public ObservableCollection<string> FullLog { get; } = new();
	public ObservableCollection<string> CarouselLog { get; } = new();

	[ObservableProperty]
	private int _carouselIndex;

	[ObservableProperty]
	private bool _isLogVisible;

	public ObservableCollection<PageViewModel> Pages { get; }
	[ObservableProperty]
	private PageViewModel? _currentPage; // This will drive the ContentControl

	[ObservableProperty]
	private PageViewModel? _selectedSidebarItem; // This is for the ListBox ONLY

	private readonly SettingsService _settingsService;
	private readonly SettingsData _settingsData;


	public MainWindowViewModel(SettingsService settingsService, SettingsData settingsData)
	{
		_settingsService = settingsService;
		_settingsData = settingsData;

		// Use the default singleton messenger instance
		_messenger = WeakReferenceMessenger.Default;

		// ** SUBSCRIBE to progress messages **
		_messenger.Register<ProgressReportMessage>(this, (recipient, message) =>
		{
			// When a message is received, update our properties
			ProgressValue = message.Report.Percentage;
			FullLog.Add(message.Report.Message);
			CarouselLog.Insert(0, message.Report.Message);
			CarouselIndex = 0;
		});


		CarouselLog.Add("Welcome to the Garry's Mod RTX Launcher!");
		FullLog.Add("Welcome to the Garry's Mod RTX Launcher!");

		// --- Existing Page Setup ---
		// Pass the messenger instance down to the page ViewModels
		var gitHubService = new GitHubService();
		var installService = new GarrysModInstallService();
		var updateService = new GarrysModUpdateService();
		var packageInstallService = new PackageInstallService();
		var patchingService = new PatchingService();
		var mountingService = new MountingService();
		var installedPackagesService = new InstalledPackagesService();
		var quickInstallService = new QuickInstallService(installService, gitHubService, packageInstallService, patchingService, installedPackagesService);
		var installedModsService = new InstalledModsService();
		_addonInstallService = new AddonInstallService();
		_modBrowserService = new ModDBModService(_addonInstallService, installedModsService);

		// 1. Create the instance of ModsViewModel
		var modsViewModel = new ModsViewModel(_modBrowserService);

		// 2. ** CONNECT THE WIRE **: Subscribe to the child's request event.
		//    When modsViewModel invokes OnViewDetailsRequested, our ShowModDetails method will run.
		modsViewModel.OnViewDetailsRequested = ShowModDetails;

		// 3. Save the instance so we can navigate back to it.
		_modsPageInstance = modsViewModel;



		Pages = new ObservableCollection<PageViewModel>
		{
			new SettingsViewModel(_settingsData, _messenger),
			new MountingViewModel(mountingService, _messenger),
			new AdvancedInstallViewModel(_messenger, gitHubService, packageInstallService, patchingService, installService, updateService, installedPackagesService),
			new AboutViewModel(_messenger, gitHubService),
			new LauncherSettingsViewModel(_settingsData, _settingsService),
			modsViewModel,
		};

		var setupViewModel = new SetupViewModel(quickInstallService, _messenger);

		// if not installed, show setup first, else put it before advanced install
		var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
		if (installType == "unknown")
		{
			Pages.Insert(0, setupViewModel);
		}
		else
		{
			// get pos of advanced install
			var advInstallIndex = Pages.IndexOf(Pages.First(p => p is AdvancedInstallViewModel));
			Pages.Insert(advInstallIndex, setupViewModel);
		}


		_selectedSidebarItem = Pages.FirstOrDefault();
		_currentPage = _selectedSidebarItem;
	}
	private void ShowModDetails(ModItemViewModel modItem)
	{
		Debug.WriteLine($"[MainWindowViewModel] ShowModDetails called for: '{modItem.Title}'.");

		var detailsViewModel = new ModDetailsViewModel(modItem.Model, _modBrowserService, _messenger);
		detailsViewModel.OnNavigateBackRequested = () =>
		{
			// When we navigate back, refresh the item in the main list
			_modsPageInstance.RefreshModItem(modItem);
			ShowModsList();
		};
		Debug.WriteLine("[MainWindowViewModel] New ModDetailsViewModel created. Now setting SelectedPage...");

		// This is the most critical line.
		CurrentPage = detailsViewModel;

		Debug.WriteLine($"[MainWindowViewModel] SelectedPage has been set to '{CurrentPage?.GetType().FullName}'.");
	}
	private void ShowModsList()
	{
		CurrentPage = _modsPageInstance;
	}
	async partial void OnSelectedSidebarItemChanged(PageViewModel? value)
	{
		if (value != null)
		{
			CurrentPage = value; // Change the content to match the sidebar selection

			// mod s
			if (value is ModsViewModel modsViewModel)
			{
				await modsViewModel.LoadModsAsync();
			}
		}
	}

	public void OnWindowClosing()
	{
		try
		{
			_settingsService.SaveSettings(_settingsData);
			(_modBrowserService as IDisposable)?.Dispose();
		}
		catch (IOException ex)
		{
			// Show an error to the user (via a dialog service in a real app)
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}" }));
		}
	}

	[RelayCommand]
	private async Task SaveLog()
	{
		// This is a placeholder. A real implementation would use a Dialog Service.
		var logContent = string.Join("\n", FullLog);
		// await dialogService.ShowSaveFileDialogAsync(logContent, "log.txt");
		System.Diagnostics.Debug.WriteLine("---- LOG SAVED ----\n" + logContent);
	}

	[RelayCommand]
	private void OpenInstallFolder()
	{
		try
		{
			LauncherUtility.OpenInstallFolder();
		}
		catch (Exception ex)
		{
			// Use the messenger to send an error to the UI to be displayed
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 }));
		}
	}

	[RelayCommand]
	private void LaunchGame(Window window) // Pass the window to get screen info
	{
		if (_settingsData == null)
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = "ERROR: Settings could not be loaded.", Percentage = 100 }));
			return;
		}

		try
		{
			var width = _settingsData.Width;
			var height = _settingsData.Height;

			// This logic is now in the ViewModel, which has access to UI-related info
			if (width == 0 || height == 0)
			{
				var screen = window.Screens.Primary;
				if (screen != null)
				{
					width = (int)screen.WorkingArea.Width;
					height = (int)screen.WorkingArea.Height;
				}
			}

			LauncherUtility.LaunchGame(_settingsData, width, height);
		}
		catch (Exception ex)
		{
			_messenger.Send(new ProgressReportMessage(new InstallProgressReport { Message = $"ERROR: {ex.Message}", Percentage = 100 }));
		}
	}

	[RelayCommand]
	private void Close(Window window)
	{
		// The command receives the Window instance from the CommandParameter
		window?.Close();
	}
}