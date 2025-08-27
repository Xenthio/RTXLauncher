using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTXLauncher.Core.Models;
using System; // Add this for DateTime
using System.Threading.Tasks;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModItemViewModel : ViewModelBase
{
	[ObservableProperty] private string _title;
	[ObservableProperty] private string _author;
	[ObservableProperty] private string _summary;
	[ObservableProperty] private string? _thumbnailUrl;
	[ObservableProperty] private bool _isInstalled;
	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private int? _totalVisits;
	[ObservableProperty] private DateTime? _releaseDate;
	[ObservableProperty] private string? _genre;
	[ObservableProperty] private int? _modId;
	[ObservableProperty] private int? _rank;

	public bool IsNotBusy => !IsBusy;

	public ModItemViewModel(ModInfo model)
	{
		_title = model.Title;
		_author = model.Author;
		_summary = model.Summary;
		_thumbnailUrl = model.ThumbnailUrl;
		_isInstalled = model.IsInstalled;
		_totalVisits = model.TotalVisits;
		_releaseDate = model.ReleaseDate;
		_genre = model.Genre;
		_modId = model.ModId;
		_rank = model.Rank;
	}

	[RelayCommand(CanExecute = nameof(IsNotBusy))]
	private async Task ManageInstallation()
	{
		IsBusy = true;
		if (IsInstalled)
		{
			await Task.Delay(1000); // Simulate uninstall service call
			IsInstalled = false;
		}
		else
		{
			await Task.Delay(3000); // Simulate install service call
			IsInstalled = true;
		}
		IsBusy = false;
	}
}