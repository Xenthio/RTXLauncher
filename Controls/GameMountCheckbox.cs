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
			Enabled = SteamLibrary.IsGameInstalled(GameFolder, InstallFolder, RemixModFolder);
			Checked = ContentMountingSystem.IsGameMounted(GameFolder, InstallFolder, RemixModFolder);
			//System.Diagnostics.Debug.WriteLine("GameMountCheckbox: " + GameFolder + " " + InstallFolder + " " + RemixModFolder + " " + Checked);
			Click += GameMountCheckbox_Click;
		}

		void GameMountCheckbox_Click(object sender, System.EventArgs e)
		{
			if (Checked)
			{
				ContentMountingSystem.MountGame(GameFolder, InstallFolder, RemixModFolder);
			}
			else
			{
				ContentMountingSystem.UnMountGame(GameFolder, InstallFolder, RemixModFolder);
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
