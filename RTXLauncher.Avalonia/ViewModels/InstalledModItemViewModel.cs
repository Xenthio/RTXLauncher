using CommunityToolkit.Mvvm.ComponentModel;
using RTXLauncher.Core.Models;
using System;

namespace RTXLauncher.Avalonia.ViewModels;

/// <summary>
/// ViewModel wrapper for an installed mod
/// </summary>
public partial class InstalledModItemViewModel : ObservableObject
{
	public InstalledMod Model { get; }

	public string Name => Model.Name;
	public string FolderName => Model.FolderName;
	public string Description => Model.Description;
	public string? Author => Model.Author;
	public string? Version => Model.Version;
	public bool IsEnabled => Model.IsEnabled;
	public bool IsFromModDB => Model.IsFromModDB;
	public string? ModDBPageUrl => Model.ModDBPageUrl;
	public DateTime? InstallDate => Model.InstallDate;
	public bool IsDeletable => Model.IsDeletable;
	public string FullPath => Model.FullPath;
	public bool HasModUsda => Model.HasModUsda;

	// UI-specific properties
	public string StatusText => IsEnabled ? "Enabled" : "Disabled";
	public string StatusColor => IsEnabled ? "LawnGreen" : "Gray";
	public string SourceBadge => IsFromModDB ? "ModDB" : "Local";
	public string ToggleButtonText => IsEnabled ? "Disable" : "Enable";
	public string DeleteButtonTooltip => IsDeletable ? "Delete this mod" : "base_mod cannot be deleted";

	public InstalledModItemViewModel(InstalledMod model)
	{
		Model = model;
	}

	/// <summary>
	/// Updates the view model when the underlying model changes
	/// </summary>
	public void Refresh()
	{
		OnPropertyChanged(nameof(IsEnabled));
		OnPropertyChanged(nameof(StatusText));
		OnPropertyChanged(nameof(StatusColor));
		OnPropertyChanged(nameof(ToggleButtonText));
	}
}
