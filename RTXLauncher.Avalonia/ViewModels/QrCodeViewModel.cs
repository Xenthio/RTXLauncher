using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTXLauncher.Avalonia.ViewModels;

public partial class QrCodeViewModel : ObservableObject
{
	[ObservableProperty]
	private Bitmap? _qrCodeImage;

	[ObservableProperty]
	private string _challengeUrl = string.Empty;
}
