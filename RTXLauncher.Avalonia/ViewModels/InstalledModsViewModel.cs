using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class InstalledModsViewModel : PageViewModel
{
	private readonly ModManagementService _modManagementService;
	private readonly IMessenger _messenger;

	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private string _searchText = string.Empty;

	public ObservableCollection<InstalledModItemViewModel> InstalledMods { get; } = new();
	public ObservableCollection<InstalledModItemViewModel> FilteredMods { get; } = new();

	public InstalledModsViewModel(ModManagementService modManagementService)
	{
		_modManagementService = modManagementService;
		_messenger = WeakReferenceMessenger.Default;
		Header = "Installed Mods";
	}

	/// <summary>
	/// Loads all installed mods
	/// </summary>
	public async Task LoadInstalledModsAsync()
	{
		IsBusy = true;
		StatusMessage = "Loading installed mods...";
		InstalledMods.Clear();
		FilteredMods.Clear();

		try
		{
			var mods = await _modManagementService.GetAllInstalledModsAsync();

			foreach (var mod in mods.OrderBy(m => m.Name))
			{
				var viewModel = new InstalledModItemViewModel(mod);
				InstalledMods.Add(viewModel);
			}

			ApplyFilter();
			StatusMessage = $"Loaded {InstalledMods.Count} mod(s)";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error loading mods: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
			// Clear status message after a delay
			await Task.Delay(2000);
			if (StatusMessage.StartsWith("Loaded") || StatusMessage.StartsWith("Error"))
			{
				StatusMessage = string.Empty;
			}
		}
	}

	/// <summary>
	/// Toggles a mod between enabled and disabled
	/// </summary>
	[RelayCommand]
	private async Task ToggleModEnabled(InstalledModItemViewModel? modItem)
	{
		if (modItem == null) return;

		IsBusy = true;
		try
		{
			if (modItem.IsEnabled)
			{
				StatusMessage = $"Disabling {modItem.Name}...";
				await _modManagementService.DisableModAsync(modItem.FolderName);
				modItem.Model.IsEnabled = false;
			}
			else
			{
				StatusMessage = $"Enabling {modItem.Name}...";
				await _modManagementService.EnableModAsync(modItem.FolderName);
				modItem.Model.IsEnabled = true;
			}

			modItem.Refresh();
			StatusMessage = $"{modItem.Name} {(modItem.IsEnabled ? "enabled" : "disabled")} successfully";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
			await Task.Delay(2000);
			StatusMessage = string.Empty;
		}
	}

	/// <summary>
	/// Deletes a mod after confirmation
	/// </summary>
	[RelayCommand]
	private async Task DeleteMod(InstalledModItemViewModel? modItem)
	{
		if (modItem == null || !modItem.IsDeletable) return;

		// TODO: Add confirmation dialog here
		// For now, we'll just proceed with deletion

		IsBusy = true;
		try
		{
			StatusMessage = $"Deleting {modItem.Name}...";
			var modPageUrl = modItem.ModDBPageUrl; // Save before deletion
			await _modManagementService.DeleteModAsync(modItem.FolderName);

			InstalledMods.Remove(modItem);
			FilteredMods.Remove(modItem);

			// Notify other views (like Get Mods) that a mod was deleted
			if (!string.IsNullOrEmpty(modPageUrl))
			{
				_messenger.Send(new ModDeletedMessage(modPageUrl));
			}

			StatusMessage = $"{modItem.Name} deleted successfully";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error deleting mod: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
			await Task.Delay(2000);
			StatusMessage = string.Empty;
		}
	}

	/// <summary>
	/// Refreshes the mod list
	/// </summary>
	[RelayCommand]
	private async Task RefreshMods()
	{
		await LoadInstalledModsAsync();
	}

	/// <summary>
	/// Applies search filter to the mod list
	/// </summary>
	partial void OnSearchTextChanged(string value)
	{
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		FilteredMods.Clear();

		var filtered = string.IsNullOrWhiteSpace(SearchText)
			? InstalledMods
			: InstalledMods.Where(m =>
				m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
				(m.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
				(m.Author?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

		foreach (var mod in filtered)
		{
			FilteredMods.Add(mod);
		}
	}

	/// <summary>
	/// Opens a URL in the default browser
	/// </summary>
	[RelayCommand]
	private void OpenUrl(string? url)
	{
		if (string.IsNullOrEmpty(url)) return;

		try
		{
			// Use Process.Start with UseShellExecute to open URL in default browser
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error opening URL: {ex.Message}";
		}
	}
}
