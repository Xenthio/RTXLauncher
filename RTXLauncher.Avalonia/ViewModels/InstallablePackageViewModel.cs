using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace RTXLauncher.Avalonia.ViewModels;

// A base class for any package that can be installed from a GitHub source.
public abstract partial class InstallablePackageViewModel : ViewModelBase
{
	protected readonly GitHubService? GitHubService;
	protected readonly InstalledPackagesService? InstalledPackagesService;

	[ObservableProperty]
	private string? _title;

	[ObservableProperty]
	private string _buttonText = "Install/Update";

	[ObservableProperty]
	private bool _isBusy; // Use this to disable controls during operations

	[ObservableProperty]
	private string? _installedVersion;

	[ObservableProperty]
	private string? _installedSource;

	[ObservableProperty]
	private bool _hasInstalledVersion;

	// A simple derived property for easier binding
	public bool IsNotBusy => !IsBusy;

	// Properties for the ComboBoxes
	public ObservableCollection<string> Sources { get; } = new();
	public ObservableCollection<GitHubRelease> Releases { get; } = new();

	[ObservableProperty]
	private string? _selectedSource;

	[ObservableProperty]
	private GitHubRelease? _selectedRelease;

	protected InstallablePackageViewModel(GitHubService? githubService, InstalledPackagesService? installedPackagesService = null)
	{
		GitHubService = githubService;
		InstalledPackagesService = installedPackagesService;
	}

	// The command for the install button
	[RelayCommand]
	protected abstract Task Install();

	// Abstract methods to be implemented by specific package types
	protected abstract Task LoadSources();
	protected abstract Task LoadReleases();

	/// <summary>
	/// Override in derived classes to load the installed version for this package type
	/// </summary>
	protected virtual Task LoadInstalledVersion()
	{
		// Base implementation does nothing - override in derived classes
		return Task.CompletedTask;
	}

	/// <summary>
	/// Updates the installed version display properties
	/// Override in derived classes to customize what version string is displayed
	/// </summary>
	protected virtual void SetInstalledVersionDisplay(InstalledPackageVersion? version)
	{
		if (version != null)
		{
			InstalledVersion = version.Version;
			InstalledSource = version.Source;
			HasInstalledVersion = true;
		}
		else
		{
			InstalledVersion = null;
			InstalledSource = null;
			HasInstalledVersion = false;
		}
	}

	/// <summary>
	/// Refreshes the installed version display
	/// </summary>
	public async Task RefreshInstalledVersionAsync()
	{
		// Clear cache to force reload from disk
		InstalledPackagesService?.ClearCache();
		await LoadInstalledVersion();
	}

	// This method is called when the ViewModel is created
	public async Task InitializeAsync()
	{
		await LoadSources();
		await LoadInstalledVersion();
		if (Sources.Count > 0)
		{
			SelectedSource = Sources[0];
		}
	}

	// When SelectedSource changes, automatically reload the releases
	async partial void OnSelectedSourceChanged(string? value)
	{
		await LoadReleases();
	}
}