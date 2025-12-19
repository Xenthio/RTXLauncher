using CommunityToolkit.Mvvm.ComponentModel;
using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class UpdateConfirmationViewModel : ObservableObject
{
	[ObservableProperty]
	private int _totalItems;

	[ObservableProperty]
	private int _newFiles;

	[ObservableProperty]
	private int _changedFiles;

	[ObservableProperty]
	private int _directories;

	[ObservableProperty]
	private bool _binFilesUpdated;

	[ObservableProperty]
	private ObservableCollection<FileUpdateInfo> _updates = new();

	public UpdateConfirmationViewModel()
	{
	}

	public UpdateConfirmationViewModel(List<FileUpdateInfo> updates, bool binFilesUpdated)
	{
		Updates = new ObservableCollection<FileUpdateInfo>(updates);
		TotalItems = updates.Count;
		NewFiles = updates.Count(u => u.IsNew);
		ChangedFiles = updates.Count(u => u.IsChanged);
		Directories = updates.Count(u => u.IsDirectory);
		BinFilesUpdated = binFilesUpdated;
	}
}

