using CommunityToolkit.Mvvm.ComponentModel;

namespace RTXLauncher.Avalonia.ViewModels;

public class ViewModelBase : ObservableObject
{
}
// A base class for all pages that will appear in the sidebar
public abstract partial class PageViewModel : ViewModelBase
{
	[ObservableProperty]
	private string _header;
}