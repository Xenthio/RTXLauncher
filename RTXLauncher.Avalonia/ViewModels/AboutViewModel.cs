using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class AboutViewModel : PageViewModel
{
	private readonly IMessenger _messenger;
	private readonly GitHubService _gitHubService;
	private readonly UpdateService _updateService;

	// --- Properties for Data Binding ---

	[ObservableProperty]
	private string _currentVersion = "v1.0.0";

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

	[ObservableProperty]
	private bool _updateAvailable;

	[ObservableProperty]
	private bool _canInstallUpdate;

	public AboutViewModel(IMessenger messenger, GitHubService gitHubService)
	{
		_messenger = messenger;
		_gitHubService = gitHubService;
		_updateService = new UpdateService(gitHubService);
		Header = "About";
		
		// Initialize with current version
		CurrentVersion = _updateService.GetCurrentVersion();
		
		// Load update sources on startup
		Task.Run(async () => await LoadUpdateSourcesAsync());
	}

	// --- Commands for Buttons ---

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		IsCheckingForUpdates = true;
		ReleaseNotes = "Checking for updates...";

		try
		{
			var result = await _updateService.CheckForUpdatesAsync(forceRefresh: true);
			
			if (result.Error != null)
			{
				ReleaseNotes = $"Error checking for updates: {result.Error.Message}";
				return;
			}

			// Update the UI with results
			UpdateAvailable = result.UpdateAvailable;
			CurrentVersion = result.CurrentVersion;
			
			// Update sources
			UpdateSources.Clear();
			foreach (var source in result.AvailableUpdates)
			{
				UpdateSources.Add(source);
			}

			// Select the latest release by default
			if (UpdateSources.Count > 1)
			{
				SelectedUpdateSource = UpdateSources[1]; // Skip staging, select first release
			}
			else if (UpdateSources.Count > 0)
			{
				SelectedUpdateSource = UpdateSources[0]; // Select staging if only option
			}

			if (UpdateAvailable && result.LatestUpdate != null)
			{
				ReleaseNotes = $"Update available: {result.LatestUpdate.Version}\n\n" +
					$"Current version: {CurrentVersion}\n\n" +
					(result.LatestUpdate.Release?.Body ?? "No release notes available.");
			}
			else
			{
				ReleaseNotes = "You have the latest version.";
			}
		}
		catch (Exception ex)
		{
			ReleaseNotes = $"Error checking for updates: {ex.Message}";
		}
		finally
		{
			IsCheckingForUpdates = false;
		}
	}

	[RelayCommand]
	private async Task InstallUpdate()
	{
		if (SelectedUpdateSource == null)
		{
			ReleaseNotes = "No update source selected.";
			return;
		}

		IsCheckingForUpdates = true;
		UpdateProgress = 0;

		try
		{
			var progress = new Progress<UpdateProgress>(p =>
			{
				UpdateProgress = p.PercentComplete;
				if (!string.IsNullOrEmpty(p.Message))
				{
					ReleaseNotes = p.Message;
				}

				if (p.Error != null)
				{
					ReleaseNotes = $"Update failed: {p.Error.Message}";
				}
			});

			string downloadFolder = await _updateService.DownloadUpdateAsync(SelectedUpdateSource, progress);
			
			// Note: The actual installation (replacing executables, etc.) would need platform-specific logic
			// For now, just show success message
			ReleaseNotes = $"Update downloaded successfully to: {downloadFolder}\n\n" +
				"Please restart the application to complete the update.";
		}
		catch (Exception ex)
		{
			ReleaseNotes = $"Update installation failed: {ex.Message}";
		}
		finally
		{
			IsCheckingForUpdates = false;
		}
	}

	private async Task LoadUpdateSourcesAsync()
	{
		try
		{
			var sources = await _updateService.GetAvailableUpdateSourcesAsync();
			
			// Update UI on main thread
			await Task.Run(() =>
			{
				UpdateSources.Clear();
				foreach (var source in sources)
				{
					UpdateSources.Add(source);
				}
			});
		}
		catch (Exception ex)
		{
			// Log error but don't crash
			System.Diagnostics.Debug.WriteLine($"Failed to load update sources: {ex.Message}");
		}
	}

	partial void OnSelectedUpdateSourceChanged(UpdateSource? value)
	{
		if (value != null)
		{
			CanInstallUpdate = true;
			UpdateReleaseNotes(value);
		}
		else
		{
			CanInstallUpdate = false;
			ReleaseNotes = null;
		}
	}

	private void UpdateReleaseNotes(UpdateSource source)
	{
		if (source.IsStaging)
		{
			ReleaseNotes = $"Development Build: {source.Version}\n\n" +
				"This is the latest development build from the master branch.\n\n" +
				"Warning: This version may contain experimental features and bugs.";
		}
		else if (source.Release != null)
		{
			var isNewer = Core.Utilities.VersionUtility.CompareVersions(source.Version, CurrentVersion) > 0;
			var status = isNewer ? "🆕 Newer version available" : "ℹ️ Same or older version";
			
			ReleaseNotes = $"{source.Name}\n{status}\n\n" +
				$"Published: {source.Release.PublishedAt:yyyy-MM-dd}\n\n" +
				(source.Release.Body ?? "No release notes available.");
				
			UpdateAvailable = isNewer;
		}
		else
		{
			ReleaseNotes = $"{source.Name}\n\nNo additional information available.";
		}
	}
}