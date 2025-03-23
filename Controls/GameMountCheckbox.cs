using System.ComponentModel;

namespace RTXLauncher
{
	public class GameMountCheckbox : System.Windows.Forms.CheckBox, ISupportInitialize
	{
		public string InstallFolder { get; set; } = "Half-Life 2: RTX";
		public string GameFolder { get; set; } = "hl2rtx";
		public string RemixModFolder { get; set; } = "hl2rtx";

		public GameMountCheckbox()
		{
		}

		public void InitMountBox()
		{
			Enabled = RTXLauncher.ContentMountingSystem.IsContentInstalled(GameFolder, InstallFolder, RemixModFolder);
			Checked = RTXLauncher.ContentMountingSystem.IsContentMounted(GameFolder, InstallFolder, RemixModFolder);
			//System.Diagnostics.Debug.WriteLine("GameMountCheckbox: " + GameFolder + " " + InstallFolder + " " + RemixModFolder + " " + Checked);
			Click += GameMountCheckbox_Click;
		}

		void GameMountCheckbox_Click(object sender, System.EventArgs e)
		{
			if (Checked)
			{
				RTXLauncher.ContentMountingSystem.MountContent(GameFolder, InstallFolder, RemixModFolder);
			}
			else
			{
				RTXLauncher.ContentMountingSystem.UnMountContent(GameFolder, InstallFolder, RemixModFolder);
			}
		}

		public void BeginInit()
		{
		}

		public void EndInit()
		{
			InitMountBox();
		}
	}
}
