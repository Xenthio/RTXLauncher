using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace RTXLauncher.WinForms;

public partial class Form1 : Form
{
	public static GarrysModInstallService _gmodInstallService { get; set; }
	public static GarrysModUpdateService _gmodUpdateService { get; set; }
	public static GitHubService _githubService { get; set; }
	public static MountingService _mountingService { get; set; }
	public static PackageInstallService _packageInstallService { get; set; }
	public static PatchingService _patchingService { get; set; }
	public static QuickInstallService _quickInstallService { get; set; }
	public static UpdateService _updateService { get; set; } // New unified update service
	//public SettingsService SettingsService { get; set; }

	public Form1()
	{
		_gmodInstallService = new GarrysModInstallService();
		_gmodUpdateService = new GarrysModUpdateService();
		_githubService = new GitHubService();
		_mountingService = new MountingService();
		_packageInstallService = new PackageInstallService();
		_patchingService = new PatchingService();
		_quickInstallService = new QuickInstallService(_gmodInstallService, _githubService, _packageInstallService, _patchingService);
		_updateService = new UpdateService(_githubService); // Initialize the unified update service
		//SettingsService = new SettingsService();



		InitializeComponent();
		InitInstallPage();
		InitialiseUpdater();
		InitializeMountingTab();
		SetInitialRTXCheckBoxValue();


	}


	private void InitializeMountingTab()
	{
		MountHL2RTXCheckbox.Initialize(_mountingService);
		MountPortalRTXCheckbox.Initialize(_mountingService);
		MountPortalPreludeRTXCheckBox.Initialize(_mountingService);
		MountP2RTXCheckBox.Initialize(_mountingService);
		gameMountCheckbox1.Initialize(_mountingService); // The Dark Messiah one, todo fix name
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
		SaveSettings();
	}

	public void Refresh()
	{
		settingsDataBindingSource.ResetBindings(false);
	}
	public void LoadSettings()
	{
		var serializer = new XmlSerializer(typeof(SettingsData));
		var filePath = "settings.xml";
		if (File.Exists(filePath))
		{
			using (var reader = new StreamReader(filePath))
			{
				settingsDataBindingSource.DataSource = (SettingsData)serializer.Deserialize(reader);
			}
		}
		else
		{
			settingsDataBindingSource.DataSource = new SettingsData();
		}
		if (settingsDataBindingSource.DataSource is SettingsData settings)
		{
			//SteamLibrary.ManuallySpecifiedGameInstallPath = settings.ManuallySpecifiedInstallPath;
			WidthHeightComboBox.Text = $"{settings.Width}x{settings.Height}";
			if (settings.Width == 0 || settings.Height == 0)
			{
				WidthHeightComboBox.Text = "Native Resolution";
			}
		}
	}
	public void SaveSettings()
	{
		// save to xml config file
		var serializer = new XmlSerializer(typeof(SettingsData));
		var filePath = "settings.xml";
		var dataSource = settingsDataBindingSource.DataSource as SettingsData;
		if (dataSource != null)
		{
			using (var writer = new StreamWriter(filePath))
			{
				serializer.Serialize(writer, dataSource);
			}
		}
		else
		{
			// Handle the case where the data source is not of type SettingsData
			throw new InvalidOperationException("Data source is not of type SettingsData.");
		}
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		// refresh the form
		LoadSettings();
		Refresh();
	}

	private void groupBox1_Enter(object sender, EventArgs e)
	{

	}

	private void LaunchGameButton_Click(object sender, EventArgs e)
	{
		LauncherUtility.LaunchGame(settingsDataBindingSource.DataSource as SettingsData, 1920, 1080);
	}

	private void textBox2_TextChanged(object sender, EventArgs e)
	{

	}

	private void label1_Click(object sender, EventArgs e)
	{

	}

	private void label2_Click(object sender, EventArgs e)
	{

	}

	private void groupBox2_Enter(object sender, EventArgs e)
	{

	}

	private void WidthHeightComboBox_TextUpdate(object sender, EventArgs e)
	{
		// set width and height from combo box string
		var resolution = WidthHeightComboBox.Text;
		// if "Native Resolution" is selected, set width and height to 0
		if (resolution == "Native Resolution")
		{
			if (settingsDataBindingSource.DataSource is SettingsData settings)
			{
				settings.Width = 0;
				settings.Height = 0;
				Refresh();
			}
			return;
		}
		var parts = resolution.Split('x');
		if (parts.Length == 2)
		{
			if (settingsDataBindingSource.DataSource is SettingsData settings)
			{
				settings.Width = int.Parse(parts[0]);
				settings.Height = int.Parse(parts[1]);
				Refresh();
			}
		}
	}

	private void WidthHeightComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{

	}

	private void checkBox1_CheckedChanged(object sender, EventArgs e)
	{
		Refresh();
	}

	private void CloseButton_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void label4_Click(object sender, EventArgs e)
	{

	}

	private void textBox2_TextChanged_1(object sender, EventArgs e)
	{

	}

	private void VanillaInstallPath_Click(object sender, EventArgs e)
	{

	}

	private void tabControl1_Selected(object sender, TabControlEventArgs e)
	{
		if (e.TabPage?.Name == "InstallPage")
		{
			//InitInstallPage();
		}
	}

	private void OpenGameInstallFolderButton_Click(object sender, EventArgs e)
	{
		// open the game install folder in explorer (where this executable is)
		var executablePath = System.AppContext.BaseDirectory;
		var folderPath = Path.GetDirectoryName(executablePath);
		if (folderPath != null)
		{
			Process.Start("explorer.exe", folderPath);
		}
	}
	bool _hasInitCheckbox;
	void SetInitialRTXCheckBoxValue()
	{
		if (_hasInitCheckbox) EnableRTXCheckBox.CheckedChanged -= EnableRTXCheckBox_CheckedChanged;
		EnableRTXCheckBox.Checked = RemixUtility.IsEnabled();
		EnableRTXCheckBox.CheckedChanged += EnableRTXCheckBox_CheckedChanged;
		_hasInitCheckbox = true;
	}
	private void EnableRTXCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		if (RemixUtility.IsEnabled())
		{
			//RemixSystem.Enabled = false;
		}
		else
		{
			//RemixSystem.Enabled = true;
		}
	}

	private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
	{

	}

	private void groupBox8_Enter(object sender, EventArgs e)
	{

	}

	private void groupBox7_Enter(object sender, EventArgs e)
	{

	}

	private void label21_Click(object sender, EventArgs e)
	{

	}

	private void BrowseButton_Click(object sender, EventArgs e)
	{
		using (OpenFileDialog openFileDialog = new OpenFileDialog())
		{
			openFileDialog.Title = "Select gmod.exe or hl2.exe";
			openFileDialog.Filter = "Game Executables|gmod.exe;hl2.exe|All Executable Files|*.exe";
			openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			openFileDialog.CheckFileExists = true;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				string selectedFilePath = openFileDialog.FileName;
				string fileName = Path.GetFileName(selectedFilePath).ToLower();

				// Make sure it's either gmod.exe or hl2.exe
				if (fileName != "gmod.exe" && fileName != "hl2.exe")
				{
					MessageBox.Show("Please select either gmod.exe or hl2.exe", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				// Get the directory containing the executable
				string installDir = Path.GetDirectoryName(selectedFilePath);

				// Validate that it's a valid installation directory
				if (!ValidateGameDirectory(installDir))
				{
					MessageBox.Show(
						"The selected directory does not appear to be a valid game installation folder. Please select a valid installation.",
						"Invalid Installation",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}
				//SteamLibraryUtility.ManuallySpecifiedGameInstallPath = installDir;
				if (settingsDataBindingSource.DataSource is SettingsData settings)
				{
					settings.ManuallySpecifiedInstallPath = installDir;
					MessageBox.Show(
						$"Install path set to: {installDir}",
						"Success",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
				Refresh();
				//RefreshInstallInfo();
			}
		}
	}

	// Helper method to validate that the selected directory is a valid game installation
	private bool ValidateGameDirectory(string directory)
	{
		// Simple validation to check for common game files/folders
		bool hasGameFolder = Directory.Exists(Path.Combine(directory, "garrysmod")) ||
							Directory.Exists(Path.Combine(directory, "hl2"));

		bool hasBinFolder = Directory.Exists(Path.Combine(directory, "bin"));

		// Check for Steam app ID which would indicate a Steam game
		bool hasSteamAppID = File.Exists(Path.Combine(directory, "steam_appid.txt"));

		// If it has either garrysmod or hl2 folder AND either bin folder or Steam App ID, consider it valid
		return (hasGameFolder) && (hasBinFolder || hasSteamAppID);
	}
}