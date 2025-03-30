using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace RTXLauncher
{
	public partial class Form1 : Form
	{

		public Form1()
		{
			InitializeComponent();
			RefreshInstallInfo();
			InitialiseUpdater();

			SetInitialRTXCheckBoxValue();
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
			LauncherProgram.LaunchGameWithSettings(settingsDataBindingSource.DataSource as SettingsData);
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
				InitInstallPage();
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
			EnableRTXCheckBox.Checked = RemixSystem.Enabled;
			EnableRTXCheckBox.CheckedChanged += EnableRTXCheckBox_CheckedChanged;
			_hasInitCheckbox = true;
		}
		private void EnableRTXCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (RemixSystem.Enabled)
			{
				RemixSystem.Enabled = false;
			}
			else
			{
				RemixSystem.Enabled = true;
			}
		}
	}
}