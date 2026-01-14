using CommunityToolkit.Mvvm.ComponentModel;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class SteamCredentialsViewModel : ObservableObject
{
	[ObservableProperty]
	private string _username = string.Empty;

	[ObservableProperty]
	private string _password = string.Empty;

	[ObservableProperty]
	private string _twoFactorCode = string.Empty;

	[ObservableProperty]
	private bool _showTwoFactorInput = false;

	[ObservableProperty]
	private bool _usernameOnly = false;

	[ObservableProperty]
	private bool _useQrCode = false;

	[ObservableProperty]
	private bool _rememberPassword = false;

	[ObservableProperty]
	private bool _skipAppConfirmation = false;

	[ObservableProperty]
	private string _manifestId = "2195078592256565401"; // Default to known working manifest

	public SteamCredentialsViewModel()
	{
	}
}
