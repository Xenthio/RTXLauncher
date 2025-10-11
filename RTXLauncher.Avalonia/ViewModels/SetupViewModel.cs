using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SetupViewModel : PageViewModel
{
	// --- Services ---
	private readonly QuickInstallService _quickInstallService;
	private readonly IMessenger _messenger;

	// --- UI State Properties ---
	[ObservableProperty]
	private bool _isWelcomeVisible;

	[ObservableProperty]
	private bool _isCompletedVisible;

	[ObservableProperty]
	private bool _isBusy;

	/// <summary>
	/// A collection of warnings to show the user before they install.
	/// </summary>
	public ObservableCollection<string> PreflightWarnings { get; } = new();

	/// <summary>
	/// Available fixes packages for the user to choose from.
	/// </summary>
	public List<FixesPackageInfo> AvailableFixesPackages { get; }

	/// <summary>
	/// The currently selected fixes package.
	/// </summary>
	[ObservableProperty]
	private FixesPackageInfo _selectedFixesPackage;

	/// <summary>
	/// A simple boolean property for the View to bind to, indicating if there are any warnings.
	/// </summary>
	public bool ShowPreflightWarnings => PreflightWarnings.Count > 0;

	public SetupViewModel(QuickInstallService quickInstallService, IMessenger messenger)
	{
		Header = "Setup";
		_quickInstallService = quickInstallService;
		_messenger = messenger;

		// Initialize fixes packages
		AvailableFixesPackages = QuickInstallService.GetAvailableFixesPackages();
		_selectedFixesPackage = AvailableFixesPackages.First(p => p.Option == FixesPackageOption.Standard); // Default to Standard

		// Run the initial checks to determine which view to show.
		CheckInitialState();
	}

	/// <summary>
	/// Checks the current environment and installation status to decide what to show the user.
	/// </summary>
	private void CheckInitialState()
	{
		// First, check if an installation already exists.
		var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
		if (installType != "unknown")
		{
			IsCompletedVisible = true;
			IsWelcomeVisible = false;
			return; // Nothing more to do.
		}

		// If no installation exists, show the welcome screen.
		IsWelcomeVisible = true;
		IsCompletedVisible = false;

		// Now, perform pre-flight checks for potential issues.
		PreflightWarnings.Clear();
		var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

		// Check if running from Downloads folder.
		if (currentDirectory.Contains(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), StringComparison.OrdinalIgnoreCase))
		{
			PreflightWarnings.Add("Warning: It looks like you are running the launcher from your Downloads folder. It is strongly recommended to move it to a dedicated folder (e.g., C:\\Games\\GModRTX) before installing.");
		}

		// A placeholder for the symlink check. A real check is complex and OS-dependent.
		// For now, we can add a general warning.
		// PreflightWarnings.Add("Note: If installation fails, you may need to run this launcher as an Administrator to allow it to create file links (symlinks).");

		OnPropertyChanged(nameof(ShowPreflightWarnings));
	}

	[RelayCommand(CanExecute = nameof(CanRunInstall))]
	private async Task RunInstallAsync()
	{
		IsBusy = true;
		// Hide both panels; progress is shown globally in the main window's progress bar.
		IsWelcomeVisible = false;
		IsCompletedVisible = false;

		var _progressHandle = new Progress<InstallProgressReport>(report => _messenger.Send(new ProgressReportMessage(report)));
		IProgress<InstallProgressReport> progressHandle = _progressHandle;

		try
		{
			await _quickInstallService.PerformQuickInstallAsync(progressHandle, SelectedFixesPackage.Option);
			// After installation, re-run the check. It should now show the 'Completed' view.
			CheckInitialState();
		}
		catch (Exception ex)
		{
			progressHandle.Report(new InstallProgressReport { Message = $"FATAL ERROR: {ex.Message}", Percentage = 100 });
			// If it fails, show the welcome screen again so they can retry.
			IsWelcomeVisible = true;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private bool CanRunInstall() => !IsBusy;

	/// <summary>
	/// Called when the selected fixes package changes to validate compatibility.
	/// </summary>
	partial void OnSelectedFixesPackageChanged(FixesPackageInfo value)
	{
		if (value == null) return;

		// Clear existing warnings related to package compatibility
		var compatWarnings = PreflightWarnings.Where(w => w.Contains("Performance") || w.Contains("64-bit")).ToList();
		foreach (var warning in compatWarnings)
		{
			PreflightWarnings.Remove(warning);
		}

		// If the selected package requires x64, add a warning if we can detect it's x32
		if (value.RequiresX64)
		{
			var installType = GarrysModUtility.GetInstallType(GarrysModUtility.GetThisInstallFolder());
			if (installType != "unknown" && installType != "gmod_x86-64")
			{
				PreflightWarnings.Add($"Warning: The '{value.DisplayName}' package requires a 64-bit installation. Your current installation appears to be 32-bit. The installation will fail if you proceed with this option.");
			}
			else if (installType == "unknown")
			{
				// We don't know yet, but we should still inform the user
				PreflightWarnings.Add($"Note: The '{value.DisplayName}' package requires a 64-bit Garry's Mod installation. Make sure your Steam copy of Garry's Mod is the 64-bit version.");
			}
		}

		OnPropertyChanged(nameof(ShowPreflightWarnings));
	}
}