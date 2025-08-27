using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
	private readonly IModService _modBrowserService;
	private List<ModItemViewModel> _allMods = new();
	private bool _modsLoaded;

	// This Action is the key to communication. The MainWindowViewModel will subscribe to it.
	public Action<ModItemViewModel>? OnViewDetailsRequested { get; set; }

	[ObservableProperty] private string? _searchText;
	[ObservableProperty] private bool _isBusy;

	public ObservableCollection<ModItemViewModel> Mods { get; } = new();

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

		var modInfos = await _modBrowserService.GetAllModsAsync();

		foreach (var info in modInfos)
		{
			var vm = new ModItemViewModel(info);
			_allMods.Add(vm);
			Mods.Add(vm);
		}

		IsBusy = false;
		_modsLoaded = true;
	}

	// This command is called from the "View Details" button in the View.
	[RelayCommand]
	private void ViewDetails(ModItemViewModel? modItem)
	{
		if (modItem != null)
		{
			Debug.WriteLine($"[ModsViewModel] ViewDetailsCommand executed for: '{modItem.Title}'.");
			Debug.WriteLine("[ModsViewModel] Invoking OnViewDetailsRequested Action...");

			// Invoke the action
			OnViewDetailsRequested?.Invoke(modItem);

			Debug.WriteLine("[ModsViewModel] OnViewDetailsRequested Action was invoked.");
		}
		else
		{
			Debug.WriteLine("[ModsViewModel] ViewDetailsCommand executed but modItem was null.");
		}
	}

	/// <summary>
	/// Refreshes the properties of a ModItemViewModel from its underlying model.
	/// This is useful for updating the UI after changes are made in a details view.
	/// </summary>
	public void RefreshModItem(ModItemViewModel? item)
	{
		if (item is null) return;
		item.IsInstalled = item.Model.IsInstalled;
	}

	async partial void OnSearchTextChanged(string? value)
	{
		IsBusy = true;
		await Task.Delay(300); // Debounce

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