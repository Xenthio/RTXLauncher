using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;

namespace RTXLauncher.Avalonia.Themes.VGUI;

public class VGUITheme : Styles
{
	public VGUITheme(IServiceProvider? sp = null)
	{
		AvaloniaXamlLoader.Load(sp, this);
	}
}