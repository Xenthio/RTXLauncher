using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModsViewModel : PageViewModel
{
	private readonly IModService _modService;
	public Action<ModItemViewModel>? OnViewDetailsRequested { get; set; }

	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private bool _canGoToPreviousPage;
	[ObservableProperty] private bool _canGoToNextPage;

	public ModQueryOptions QueryOptions { get; } = new();
	public ObservableCollection<ModItemViewModel> Mods { get; } = new();

	public Dictionary<string, string> SortOptions { get; } = new()
	{
		{ "Popular (All Time)", "visitstotal-desc" },
		{ "Popular (Today)", "ranktoday-asc" },
		{ "Newest First", "dateup-desc" },
		{ "Oldest First", "dateup-asc" },
		{ "Name (A-Z)", "name-asc" }
	};

	[ObservableProperty]
	private KeyValuePair<string, string> _selectedSortOption;

	public ModsViewModel(IModService modService)
	{
		_modService = modService;
		Header = "Mods";
		_selectedSortOption = SortOptions.First(); // Set default sort order
		
		// Listen for mod deletion messages to update IsInstalled status
		WeakReferenceMessenger.Default.Register<ModDeletedMessage>(this, (recipient, message) =>
		{
			Debug.WriteLine($"[ModsViewModel] Received ModDeletedMessage for: {message.ModPageUrl}");
			
			// Find the mod in the list and update its IsInstalled status
			var mod = Mods.FirstOrDefault(m => m.Model.ModPageUrl == message.ModPageUrl);
			if (mod != null)
			{
				mod.Model.IsInstalled = false;
				mod.IsInstalled = false; // Update the ViewModel property which will trigger UI update
				Debug.WriteLine($"[ModsViewModel] Updated IsInstalled to false for: {mod.Title}");
			}
		});
	}
	public async Task LoadModsAsync()
	{
		IsBusy = true;
		StatusMessage = "Loading mods...";
		Mods.Clear();

		try
		{
			// Set up progress callback for Chrome download if needed
			var progress = new Progress<string>(status => StatusMessage = status);
			
			// Pass progress to ModDBModService if it's the implementation
			if (_modService is ModDBModService modDbService)
			{
				modDbService.OnStatusUpdate = progress;
			}

			QueryOptions.SortOrder = SelectedSortOption.Value;
			var mods = await _modService.GetAllModsAsync(QueryOptions);

			foreach (var mod in mods)
			{
				Mods.Add(new ModItemViewModel(mod));
			}

			UpdatePagination();
			StatusMessage = $"Loaded {Mods.Count} mods";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
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

	[RelayCommand]
	private async Task ApplyFiltersAsync()
	{
		QueryOptions.Page = 1; // Reset to first page for new search/filter
		await LoadModsAsync();
	}

	[RelayCommand]
	private async Task NextPageAsync()
	{
		if (!CanGoToNextPage) return;
		QueryOptions.Page++;
		await LoadModsAsync();
	}

	[RelayCommand]
	private async Task PreviousPageAsync()
	{
		if (!CanGoToPreviousPage) return;
		QueryOptions.Page--;
		await LoadModsAsync();
	}

	private void UpdatePagination()
	{
		CanGoToPreviousPage = QueryOptions.Page > 1;
		// We can go to the next page if we got a full page of results (ModDB shows 30 per page)
		CanGoToNextPage = Mods.Count == 30;
	}

	[RelayCommand]
	private void ViewDetails(ModItemViewModel? item)
	{
		if (item != null)
		{
			OnViewDetailsRequested?.Invoke(item);
		}
	}

	public void RefreshModItem(ModItemViewModel? item)
	{
		if (item is null) return;
		var displayedItem = Mods.FirstOrDefault(m => m.Model.ModPageUrl == item.Model.ModPageUrl);
		if (displayedItem != null)
		{
			displayedItem.IsInstalled = item.Model.IsInstalled;
		}
	}
}