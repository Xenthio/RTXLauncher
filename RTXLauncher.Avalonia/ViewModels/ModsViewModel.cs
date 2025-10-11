using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTXLauncher.Avalonia.Utilities;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModsViewModel : PageViewModel
{
	private readonly IModService _modService;
	public Action<ModItemViewModel>? OnViewDetailsRequested { get; set; }

	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private bool _canGoToPreviousPage;
	[ObservableProperty] private bool _canGoToNextPage;

	public ModQueryOptions QueryOptions { get; } = new();
	public ObservableCollection<ModItemViewModel> Mods { get; } = new();

	// Track if disclaimer has been shown
	private bool _disclaimerShown = false;

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
	}
	public async Task LoadModsAsync()
	{
		// Show disclaimer on first access
		if (!_disclaimerShown)
		{
			_disclaimerShown = true;
			await ShowDisclaimerAsync();
		}

		IsBusy = true;
		Mods.Clear();

		QueryOptions.SortOrder = SelectedSortOption.Value;
		var mods = await _modService.GetAllModsAsync(QueryOptions);

		foreach (var mod in mods)
		{
			Mods.Add(new ModItemViewModel(mod));
		}

		UpdatePagination();
		IsBusy = false;
	}

	private async Task ShowDisclaimerAsync()
	{
		await DialogUtility.ShowMessageAsync(
			"Mods Browser",
			"This is a proof of concept feature.\n\n" +
            "You cannot download and install mods yet"
		);
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