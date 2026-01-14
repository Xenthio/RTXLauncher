using CommunityToolkit.Mvvm.ComponentModel;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SteamAuthCodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Steam Guard";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private bool _isPassword = false;
}
