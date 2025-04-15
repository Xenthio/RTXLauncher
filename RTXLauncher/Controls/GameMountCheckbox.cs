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
              string message = 	$"Are you sure you want to mount {InstallFolder}?\n\n" +
					$"By selecting yes, you agree that you have correctly extracted any RTX IO assets prior to mounting.\n\n" +
					$"You also agree that there will be no user support for assets that have incomplete replacements such as the Half-Life 2: RTX demo.";
                string linkText = "Learn more about content mounting";
                string linkUrl = "https://github.com/Xenthio/gmod-rtx-fixes-2/wiki/Using-HL2-RTX-Assets-in-Garry's-Mod-RTX";
                
                using (var customDialog = new CustomWarningDialog(message, "Confirm Mount", linkText, linkUrl))
                {
                    var result = customDialog.ShowDialog();
                    
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        ContentMountingSystem.MountGame(GameFolder, InstallFolder, RemixModFolder);
                    }
                    else
                    {
                        // Revert checkbox state if user cancels
                        Checked = false;
                    }
                }
			}
			else
			{
				// Show confirmation dialog before unmounting
				var result = System.Windows.Forms.MessageBox.Show(
					$"Are you sure you want to unmount {InstallFolder}?",
					"Confirm Unmount",
					System.Windows.Forms.MessageBoxButtons.YesNo,
					System.Windows.Forms.MessageBoxIcon.Warning);
					
				if (result == System.Windows.Forms.DialogResult.Yes)
				{
					ContentMountingSystem.UnMountGame(GameFolder, InstallFolder, RemixModFolder);
				}
				else
				{
					// Revert the checkbox state if user cancels
					Checked = true;
				}
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
