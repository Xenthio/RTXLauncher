using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RTXLauncher.Avalonia.Views;

public partial class SteamAuthCodeWindow : Window
{
    public bool Result { get; private set; }

    public SteamAuthCodeWindow()
    {
        InitializeComponent();
    }

    private void ContinueButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
