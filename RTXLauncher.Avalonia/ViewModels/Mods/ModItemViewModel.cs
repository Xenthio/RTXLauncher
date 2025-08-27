using CommunityToolkit.Mvvm.ComponentModel;
using RTXLauncher.Core.Models;
using System;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class ModItemViewModel : ViewModelBase
{
	public ModInfo Model { get; }

	[ObservableProperty] private string _title;
	[ObservableProperty] private string _author;
	[ObservableProperty] private bool _isInstalled;
	[ObservableProperty] private int? _totalVisits;
	[ObservableProperty] private DateTime? _releaseDate;
	[ObservableProperty] private string? _genre;
	[ObservableProperty] private string? _thumbnailUrl;

	public ModItemViewModel(ModInfo model)
	{
		Model = model;
		_title = model.Title;
		_author = model.Author;
		_isInstalled = model.IsInstalled;
		_totalVisits = model.TotalVisits;
		_releaseDate = model.ReleaseDate;
		_genre = model.Genre;
		_thumbnailUrl = model.ThumbnailUrl;
	}
}