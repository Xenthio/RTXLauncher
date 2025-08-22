using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTXLauncher.Core.Models; // <-- Reference the Core model
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

	public bool IsNotBusy => !IsBusy;

	public ModItemViewModel(ModInfo model)
	{
		_title = model.Title;
		_author = model.Author;
		_summary = model.Summary;
		_thumbnailUrl = model.ThumbnailUrl;
		_isInstalled = model.IsInstalled;
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