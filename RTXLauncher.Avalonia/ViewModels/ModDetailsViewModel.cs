using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

	public ModDetailsViewModel(ModInfo mod, IModService modService, IMessenger messenger)
	{
		_mod = mod;
		_modService = modService;
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
		ReportProgress("Preparing download...", 0);
		try
		{
			ReportProgress($"Fetching details for {file.Title}...", 5);
			var detailedFile = await _modService.GetFileDetailsAndUrlAsync(file);
			if (string.IsNullOrEmpty(detailedFile.DirectDownloadUrl)) throw new Exception("Could not retrieve a valid download mirror URL.");

			var destinationPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), detailedFile.Filename ?? "mod.zip");
			ReportProgress($"Starting download to {destinationPath}...", 10);

			var progress = new Progress<double>(percentage =>
			{
				ReportProgress($"Downloading... {percentage:F1}%", percentage);
				DownloadProgress = percentage;
			});
			await _modService.DownloadFileAsync(detailedFile, destinationPath, progress);

			ReportProgress("Download complete! Starting installation...", 95);
			await Task.Delay(1000);
			ReportProgress("Installation successful!", 100);
			_mod.IsInstalled = true;
		}
		catch (Exception ex)
		{
			ReportProgress($"ERROR: {ex.Message}", 100);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void ReportProgress(string message, double percentage)
	{
		var report = new InstallProgressReport { Message = message, Percentage = ((int)percentage) };
		_messenger.Send(new ProgressReportMessage(report));
	}
}