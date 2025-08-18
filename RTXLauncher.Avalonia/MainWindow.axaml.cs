using Avalonia.Controls;
using RTXLauncher.Avalonia.ViewModels;

namespace RTXLauncher.Avalonia;

public partial class MainWindow : Window
{
	public MainWindow(MainWindowViewModel vm)
	{
		InitializeComponent();

		// The DataContext is now the main window's viewmodel
		DataContext = vm;

		this.Closing += (sender, args) => vm.OnWindowClosing();
	}
}