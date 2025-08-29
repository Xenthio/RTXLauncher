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

		async void GameMountCheckbox_Click(object sender, System.EventArgs e)
		{
			if (Checked)
			{
				// Check if the game has RTXIO package files and warn the user
				var installPath = SteamLibrary.GetGameInstallFolder(InstallFolder);
				bool hasRTXIOPackages = installPath != null && RTXIOPackageManager.HasRTXIOPackageFiles(installPath, RemixModFolder);

				string message = $"Are you sure you want to mount {InstallFolder}?\n\n";

				if (hasRTXIOPackages)
				{
					var pkgFiles = RTXIOPackageManager.GetPackageFiles(installPath, RemixModFolder);
					message += $"This game contains {pkgFiles.Length} RTXIO package file(s) that will be automatically extracted during mounting.\n\n" +
							  "What will happen:\n" +
							  "• dxvk-remix repository will be downloaded and RTXIO will be extracted (if not already present)\n" +
							  "• Package files will be extracted and original .pkg files will be removed\n" +
							  "• This process may take several minutes depending on file size\n" +
							  "• An internet connection is required for the initial download\n\n" +
							  "This is a one-time process per game installation.\n\n";
				}

				message += "By selecting yes, you agree that you have correctly extracted any RTX IO assets prior to mounting.\n\n" +
						  "You also agree that there will be no user support for assets that have incomplete replacements such as the Half-Life 2: RTX demo.";

				string linkText = "Learn more about content mounting";
				string linkUrl = "https://github.com/Xenthio/garrys-mod-rtx-remixed/wiki/Using-HL2-RTX-Assets-in-Garry's-Mod-RTX";

				using (var customDialog = new CustomWarningDialog(message, "Confirm Mount", linkText, linkUrl))
				{
					var result = customDialog.ShowDialog();

					if (result == System.Windows.Forms.DialogResult.Yes)
					{
						// Disable the checkbox during mounting to prevent multiple operations
						Enabled = false;

						try
						{
							bool mountSuccess = await ContentMountingSystem.MountGameAsync(GameFolder, InstallFolder, RemixModFolder);

							if (!mountSuccess)
							{
								// Revert checkbox state if mounting failed
								Checked = false;
							}
						}
						catch (Exception ex)
						{
							System.Windows.Forms.MessageBox.Show(
								$"An error occurred during mounting:\n\n{ex.Message}",
								"Mounting Error",
								System.Windows.Forms.MessageBoxButtons.OK,
								System.Windows.Forms.MessageBoxIcon.Error);

							// Revert checkbox state if mounting failed
							Checked = false;
						}
						finally
						{
							// Re-enable the checkbox
							Enabled = true;
						}
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
