using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

// TODO: move to core models
public class UpdateSource
{
	public string Name { get; set; }
	public string Version { get; set; }

	// TODO: Move properties from old code.

	public override string ToString() => Name; // For the ComboBox display
}

public partial class AboutViewModel : PageViewModel
{
	private readonly IMessenger _messenger;
	private readonly GitHubService _gitHubService;

	// --- Properties for Data Binding ---

	[ObservableProperty]
	private string _currentVersion = "v1.0.0"; // You'll get this from the assembly

	[ObservableProperty]
	private string? _releaseNotes;

	[ObservableProperty]
	private int _updateProgress;

	// A collection for the ComboBox's ItemsSource
	public ObservableCollection<UpdateSource> UpdateSources { get; } = new();

	[ObservableProperty]
	private UpdateSource? _selectedUpdateSource;

	[ObservableProperty]
	private bool _isCheckingForUpdates; // To disable buttons during operation

	public AboutViewModel(IMessenger messenger, GitHubService gitHubService)
	{
		_messenger = messenger;
		_gitHubService = gitHubService;
		Header = "About";
		// TODO: Move the logic from InitialiseUpdater() here.
		// For example, get the current version from the assembly.
	}

	// --- Commands for Buttons ---

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		IsCheckingForUpdates = true;
		ReleaseNotes = "Checking for updates...";
		// TODO: Move the logic from CheckForUpdatesAsync() here.
		// This will involve calling a service to fetch data from GitHub.
		IsCheckingForUpdates = false;
	}

	[RelayCommand]
	private async Task InstallUpdate()
	{
		IsCheckingForUpdates = true;
		// TODO: Move the logic from InstallLauncherUpdateButton_Click() here.
		// This will involve downloading files and running the batch script.
		IsCheckingForUpdates = false;
	}
}