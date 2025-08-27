using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModDetailsViewModel : PageViewModel
{
	private readonly ModInfo _mod;
	private readonly IModService _modService;
	private readonly AddonInstallService _addonInstallService;
	private readonly IMessenger _messenger;

	public Action? OnNavigateBackRequested { get; set; }

	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private double _downloadProgress;

	public string Title => _mod.Title;
	public string Summary => _mod.Summary;
	public string? Author => _mod.Author;
	public string? Genre => _mod.Genre;
	public int? Rank => _mod.Rank;
	public int? TotalVisits => _mod.TotalVisits;
	public DateTime? ReleaseDate => _mod.ReleaseDate;

	public ObservableCollection<ModFile> Files { get; } = new();

	public ModDetailsViewModel(ModInfo mod, IModService modService, AddonInstallService addonInstallService, IMessenger messenger)
	{
		_mod = mod;
		_modService = modService;
		_addonInstallService = addonInstallService;
		_messenger = messenger;
		// Combine Header with a "Back" button hint for better UX
		Header = "‹ Mods / " + mod.Title;

		LoadFilesCommand.Execute(null);
	}

	[RelayCommand]
	private void NavigateBack()
	{
		OnNavigateBackRequested?.Invoke();
	}

	// ... (LoadFilesAsync and InstallFileAsync are the same as the previous step) ...
	[RelayCommand]
	private async Task LoadFilesAsync()
	{
		IsBusy = true;
		Files.Clear();
		var files = await _modService.GetFilesForModAsync(_mod);
		foreach (var file in files) Files.Add(file);
		IsBusy = false;
	}

	[RelayCommand]
	private async Task InstallFileAsync(ModFile? file)
	{
		if (file is null) return;
		IsBusy = true;
		DownloadProgress = 0;

		try
		{
			var progress = new Progress<InstallProgressReport>(report =>
			{
				ReportProgress(report.Message, report.Percentage);
				DownloadProgress = report.Percentage;
			});

			// The confirmation provider links the core service to a UI implementation
			Func<string, Task<bool>> confirmationProvider = async (message) =>
			{
				return await DialogUtility.ShowConfirmationAsync("Confirm Action", message);
			};

			await _modService.InstallModFileAsync(_mod, file, confirmationProvider, progress);
		}
		catch (Exception ex)
		{
			ReportProgress($"ERROR: {ex.Message}", 100);
			await DialogUtility.ShowErrorAsync("Installation Failed", $"An error occurred during the installation process: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
			DownloadProgress = 0; // Reset progress bar
		}
	}

	private void ReportProgress(string message, double percentage)
	{
		var report = new InstallProgressReport { Message = message, Percentage = ((int)percentage) };
		_messenger.Send(new ProgressReportMessage(report));
	}
}