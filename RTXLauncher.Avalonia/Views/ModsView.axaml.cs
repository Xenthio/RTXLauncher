using Avalonia.Controls;
using Avalonia.Input;
using RTXLauncher.Avalonia.ViewModels;

namespace RTXLauncher.Avalonia.Views;

public partial class ModsView : UserControl
{
	public ModsView()
	{
		InitializeComponent();
	}
	// Allows pressing Enter in the search box to trigger the search
	private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			if (DataContext is ModsViewModel vm)
			{
				vm.ApplyFiltersCommand.Execute(null);
			}
		}
	}
}