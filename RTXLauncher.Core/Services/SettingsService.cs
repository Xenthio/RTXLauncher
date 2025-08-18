using RTXLauncher.Core.Models;
using System.Xml.Serialization;

namespace RTXLauncher.Core.Services;

public class SettingsService
{
	private readonly string _filePath = "settings.xml";

	public SettingsData LoadSettings()
	{
		if (!File.Exists(_filePath))
		{
			return new SettingsData(); // Return default settings
		}

		try
		{
			var serializer = new XmlSerializer(typeof(SettingsData));
			using var reader = new StreamReader(_filePath);
			return (SettingsData)serializer.Deserialize(reader)!;
		}
		catch (Exception ex)
		{
			// If loading fails, return defaults to prevent a crash
			System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
			return new SettingsData();
		}
	}

	public void SaveSettings(SettingsData settings)
	{
		try
		{
			var serializer = new XmlSerializer(typeof(SettingsData));
			using var writer = new StreamWriter(_filePath);
			serializer.Serialize(writer, settings);
		}
		catch (Exception ex)
		{
			// The ViewModel should catch this and inform the user
			throw new IOException("Failed to save settings to disk.", ex);
		}
	}
}