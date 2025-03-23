using System.ComponentModel;
using System.Xml.Serialization;

namespace RTXLauncher
{
	public class SettingsData : INotifyPropertyChanged
	{
		[XmlAttribute]
		public bool IsFullscreen { get; set; } = true;
		[XmlAttribute]
		public bool UseCustomResolution { get; set; } = false;
		[XmlAttribute]
		public int Width { get; set; } = 1920;
		[XmlAttribute]
		public int Height { get; set; } = 1080;
		[XmlAttribute]
		public bool LoadWorkshopAddons { get; set; } = true;
		[XmlAttribute]
		public bool DisableChromium { get; set; } = false;

		// Advanced settings
		[XmlAttribute]
		public bool ConsoleEnabled { get; set; } = true;
		[XmlAttribute]
		public bool DeveloperMode { get; set; } = false;
		[XmlAttribute]
		public bool ToolsMode { get; set; } = false;
		[XmlAttribute]
		public int DXLevel { get; set; } = 90;
		[XmlAttribute]
		public string CustomLaunchOptions { get; set; } = "";

		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
