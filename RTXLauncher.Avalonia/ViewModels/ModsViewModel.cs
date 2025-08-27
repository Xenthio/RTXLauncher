// Avalonia/ViewModels/ModsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using RTXLauncher.Core.Services; // <-- Reference the Core service
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModsViewModel : PageViewModel
{
	private readonly IModService _modBrowserService;
	private List<ModItemViewModel> _allMods = new();
	private bool _modsLoaded;

	[ObservableProperty] private string? _searchText;
	[ObservableProperty] private bool _isBusy;

	public ObservableCollection<ModItemViewModel> Mods { get; } = new();

	// The service from the Core project is passed in here.
	public ModsViewModel(IModService modBrowserService)
	{
		Header = "Mods";
		_modBrowserService = modBrowserService;
	}

	public async Task LoadModsAsync()
	{
		if (_modsLoaded) return;

		IsBusy = true;
		Mods.Clear();
		_allMods.Clear();

		// Call the service in the Core project to get pure data
		var modInfos = await _modBrowserService.GetAllModsAsync();

		// Convert the data Models into UI ViewModels
		foreach (var info in modInfos)
		{
			var vm = new ModItemViewModel(info);
			_allMods.Add(vm);
			Mods.Add(vm);
		}

		IsBusy = false;
		_modsLoaded = true;
	}

	// This method is called automatically when the SearchText property changes in the UI
	async partial void OnSearchTextChanged(string? value)
	{
		// This provides live, as-you-type searching
		IsBusy = true;
		await Task.Delay(300); // Debounce: wait a moment before searching to avoid spamming

		Mods.Clear();
		if (string.IsNullOrWhiteSpace(value))
		{
			foreach (var mod in _allMods) Mods.Add(mod);
		}
		else
		{
			var filteredMods = _allMods.Where(m =>
				m.Title.Contains(value, StringComparison.OrdinalIgnoreCase) ||
				m.Author.Contains(value, StringComparison.OrdinalIgnoreCase));

			foreach (var mod in filteredMods) Mods.Add(mod);
		}
		IsBusy = false;
	}
}