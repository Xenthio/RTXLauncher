namespace RTXLauncher
{
	public static class GarrysModInstallSystem
	{
		// Add a completed event
		public delegate void InstallationCompletedHandler(bool success, string message);
		public static event InstallationCompletedHandler OnInstallationCompleted;

		// Create a delegate for the progress updates
		public delegate void ProgressUpdateHandler(string message, int progress);
		public static event ProgressUpdateHandler OnProgressUpdate;

		public static bool TestMode = false;
		public static string TestModeReturn = "unknown_type";

		/// <summary>
		/// Get the game folder, should be like D:\SteamLibrary\steamapps\common\GarrysMod, should be where this exe is.
		/// </summary>
		/// <returns></returns>
		public static string GetThisInstallFolder()
		{
			return Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? "N/A";//Assembly.GetExecutingAssembly().Location);
		}
		public static string GetVanillaInstallFolder()
		{
			return SteamLibrary.GetGameInstallFolder("GarrysMod");//Assembly.GetExecutingAssembly().Location);
		}
		public static string GetInstallType(string path)
		{
			if (TestMode) return TestModeReturn;
			if (path == null) return "unknown";

			if (Directory.Exists(Path.Combine(path, "garrysmod")))
			{
				if (File.Exists(Path.Combine(path, "bin", "win64", "gmod.exe"))) return "gmod_x86-64";
				if (File.Exists(Path.Combine(path, "bin", "gmod.exe"))) return "gmod_i386";
				if (File.Exists(Path.Combine(path, "gmod.exe"))) return "gmod_main";
				if (File.Exists(Path.Combine(path, "hl2.exe"))) return "gmod_main-legacy";
				return "gmod_unknown";
			}

			return "unknown";
		}

		// Add a flag to track if the user has already been prompted about symlink failures
		private static bool _userAcceptedSymlinkFailures = false;
		private static bool CreateDirectorySymbolicLink(string path, string pathToTarget)
		{
			try
			{
				// Attempt to create a symbolic link
				Directory.CreateSymbolicLink(path, pathToTarget);
				return true;
			}
			catch (Exception ex)
			{
				// If symlink fails, check if the user wants to continue
				if (!_userAcceptedSymlinkFailures)
				{
					bool continueOperation = false;

					// only prompt if we're not already running as admin.
					bool isAdmin = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
					if (!isAdmin)
					{
						continueOperation = PromptSymlinkFailure(
						$"Failed to create directory symlink to {Path.GetFileName(path)}. " +
						"This may be due to insufficient privileges.\n\n" +
						"File size efficient installation requires symbolic links to function correctly.\n\n" +
						"Do you want to run RTX Launcher as administrator and try again?");
					}


					if (continueOperation)
					{
						// Restart as admin
						RestartAsAdmin();
						// We won't actually get past this point if restart succeeds
					}
					else
					{
						bool copyInstead = PromptCopyInstead(
						$"Failed to create directory symlink to {Path.GetFileName(path)}. " +
						"The installation can continue by copying files instead, but this will use more disk space." +
						"Do you want to copy instead? This may substantially increase disk space usage.");

						if (copyInstead)
						{
							try
							{
								CopyDirectory(pathToTarget, path);
								return true;
							}
							catch (Exception ex2)
							{
								LogProgress($"Failed to copy directory: {ex2.Message}", 0);
								bool continueInstallation = PromptContinueWithout(
									$"Failed to copy directory to {Path.GetFileName(path)}. " +
									$"Error: {ex2.Message}\n\n" +
									$"Would you like to continue without this content?" +
									$"Best case scenario is you won't see some things ingame." +
									$"Worst case, the game will crash or not start at all.");

								if (!continueInstallation)
								{
									throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
								}
								else
								{
									_userAcceptedSymlinkFailures = true;
								}
							}
						}
						else
						{
							bool continueInstallation = PromptContinueWithout(
								$"Would you like to continue without this content?" +
								$"Best case scenario is you won't see some things ingame." +
								$"Worst case, the game will crash or not start at all.");
							if (!continueInstallation)
							{
								throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
							}
							else
							{
								_userAcceptedSymlinkFailures = true;
							}
						}
					}
				}

				LogProgress($"ERROR: Insufficient privileges to create symlink: {Path.GetFileName(path)}", 0);
				return false;
			}
		}

		private static bool CreateFileSymbolicLink(string path, string pathToTarget)
		{
			try
			{
				// Attempt to create a symbolic link
				File.CreateSymbolicLink(path, pathToTarget);
				return true;
			}
			catch (Exception ex)
			{
				// If symlink fails, check if the user wants to continue
				if (!_userAcceptedSymlinkFailures)
				{
					bool continueOperation = false;

					// only prompt if we're not already running as admin.
					bool isAdmin = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
					if (!isAdmin)
					{
						continueOperation = PromptSymlinkFailure(
						$"Failed to create file symlink to {Path.GetFileName(path)}. " +
						"This may be due to insufficient privileges.\n\n" +
						"File size efficient installation requires symbolic links to function correctly.\n\n" +
						"Do you want to run RTX Launcher as administrator and try again?");
					}

					if (continueOperation)
					{
						// Restart as admin
						RestartAsAdmin();
						// We won't actually get past this point if restart succeeds
					}
					else
					{
						bool copyInstead = PromptCopyInstead(
						$"Failed to create file symlink to {Path.GetFileName(path)}. " +
						"The installation can continue by copying files instead, but this will use more disk space." +
						"Do you want to copy instead? This may substantially increase disk space usage.");

						if (copyInstead)
						{
							try
							{
								File.Copy(pathToTarget, path, true);
								return true;
							}
							catch (Exception ex2)
							{
								LogProgress($"Failed to copy file: {ex2.Message}", 0);
								bool continueInstallation = PromptContinueWithout(
									$"Failed to copy file to {Path.GetFileName(path)}. " +
									$"Error: {ex2.Message}\n\n" +
									$"Would you like to continue without this content?" +
									$"Best case scenario is you won't see some things ingame." +
									$"Worst case, the game will crash or not start at all.");

								if (!continueInstallation)
								{
									throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
								}
								else
								{
									_userAcceptedSymlinkFailures = true;
								}
							}
						}
						else
						{
							bool continueInstallation = PromptContinueWithout(
								$"Would you like to continue without this content?" +
								$"Best case scenario is you won't see some things ingame." +
								$"Worst case, the game will crash or not start at all.");
							if (!continueInstallation)
							{
								throw new OperationCanceledException("Installation cancelled due to symlink creation failure.");
							}
							else
							{
								_userAcceptedSymlinkFailures = true;
							}
						}
					}
				}

				LogProgress($"ERROR: Insufficient privileges to create symlink: {Path.GetFileName(path)}", 0);
				return false;
			}
		}

		private static void RestartAsAdmin()
		{
			var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			var startInfo = new System.Diagnostics.ProcessStartInfo(exeName)
			{
				Verb = "runas"
			};

			try
			{
				System.Diagnostics.Process.Start(startInfo);
				System.Windows.Forms.Application.Exit();
			}
			catch (Exception ex)
			{
				LogProgress($"Failed to restart as admin: {ex.Message}", 0);
			}
		}

		// Helper method to prompt the user about symlink failures
		private static bool PromptSymlinkFailure(string message)
		{
			// Use a simpler approach that doesn't rely on CurrentSynchronizationContext
			if (Application.OpenForms.Count > 0)
			{
				// If we have a UI form, use Invoke to show dialog on UI thread
				var mainForm = Application.OpenForms[0];
				bool result = false;

				mainForm.Invoke(new Action(() =>
				{
					DialogResult dialogResult = MessageBox.Show(
						message,
						"Symlink Creation Failed",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					result = (dialogResult == DialogResult.Yes);

					// Set the flag if user clicked OK to prevent future prompts
					if (result)
					{
						_userAcceptedSymlinkFailures = true;
					}
				}));

				return result;
			}
			else
			{
				// Fallback for no UI context
				// Just log and assume "cancel" (false)
				System.Diagnostics.Debug.WriteLine("No UI context available for symlink failure prompt. Message: " + message);
				return false;
			}
		}

		// Apply the same pattern to these two methods as well
		private static bool PromptCopyInstead(string message)
		{
			// Use a simpler approach that doesn't rely on CurrentSynchronizationContext
			if (Application.OpenForms.Count > 0)
			{
				// If we have a UI form, use Invoke to show dialog on UI thread
				var mainForm = Application.OpenForms[0];
				bool result = false;

				mainForm.Invoke(new Action(() =>
				{
					DialogResult dialogResult = MessageBox.Show(
						message,
						"Symlink Creation Failed",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);

					result = (dialogResult == DialogResult.Yes);
				}));

				return result;
			}
			else
			{
				// Fallback for no UI context
				System.Diagnostics.Debug.WriteLine("No UI context available for copy instead prompt. Message: " + message);
				return false;
			}
		}

		private static bool PromptContinueWithout(string message)
		{
			// Use a simpler approach that doesn't rely on CurrentSynchronizationContext
			if (Application.OpenForms.Count > 0)
			{
				// If we have a UI form, use Invoke to show dialog on UI thread
				var mainForm = Application.OpenForms[0];
				bool result = false;

				mainForm.Invoke(new Action(() =>
				{
					DialogResult dialogResult = MessageBox.Show(
						message,
						"Symlink Creation Failed",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);

					result = (dialogResult == DialogResult.Yes);
				}));

				return result;
			}
			else
			{
				// Fallback for no UI context
				System.Diagnostics.Debug.WriteLine("No UI context available for continue without prompt. Message: " + message);
				return false;
			}
		}

		// Log a message with progress update
		private static void LogProgress(string message, int progress)
		{
			System.Diagnostics.Debug.WriteLine(message);
			OnProgressUpdate?.Invoke(message, progress);
		}

		// Modify the ShowConfirmationDialog method to include the warning
		public static bool ShowConfirmationDialog()
		{
			var vanillaPath = GetVanillaInstallFolder();
			var newInstallPath = GetThisInstallFolder();

			// Check if directory is not empty
			bool isEmpty = IsDirectoryEmpty(newInstallPath);
			string warningMessage = "";

			if (!isEmpty)
			{
				warningMessage = "WARNING: The installation directory is not empty! " +
					"Existing files may be overwritten or cause conflicts.\n\n";
			}

			string message = $"{warningMessage}An RTX install will be created in:\n\n{newInstallPath}\n\n" +
				$"Using files from vanilla Garry's Mod installation at:\n\n{vanillaPath}\n\nDo you want to proceed?";

			MessageBoxIcon icon = isEmpty ? MessageBoxIcon.Information : MessageBoxIcon.Warning;

			DialogResult result = MessageBox.Show(
				message,
				"Confirm RTX Installation",
				MessageBoxButtons.OKCancel,
				icon
			);

			return result == DialogResult.OK;
		}

		public static bool IsDirectoryEmpty(string path, List<string> excludedFiles = null)
		{
			excludedFiles = excludedFiles ?? new List<string>();

			// Get executable name
			string executableName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

			// Add default excluded files
			if (!excludedFiles.Contains(executableName, StringComparer.OrdinalIgnoreCase))
			{
				excludedFiles.Add(executableName);
			}
			if (!excludedFiles.Contains("settings.xml", StringComparer.OrdinalIgnoreCase))
			{
				excludedFiles.Add("settings.xml");
			}

			// Check for files other than excluded ones
			bool hasNonExcludedFiles = Directory.GetFiles(path)
				.Select(file => Path.GetFileName(file))
				.Any(fileName => !excludedFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase));

			// Check for any directories
			bool hasDirectories = Directory.GetDirectories(path).Any();

			// Return true if there are no non-excluded files and no directories
			return !hasNonExcludedFiles && !hasDirectories;
		}

		// Here's what installing clones from the vanilla folder.
		// We symlink VanillaGMOD/garrysmod/garrysmod_*.vpk, (garrysmod_000.vpk, garrysmod_001.vpk, garrysmod_dir.vpk etc.) files to the new install folder.
		// We symlink VanillaGMOD/garrysmod/fallbacks_*.vpk, (fallbacks_000.vpk, fallbacks_001.vpk, fallbacks_dir.vpk etc.) files to the new install folder.
		// We symlink VanillaGMOD/sourceengine entirely to the new install folder.
		// we copy over everything in VanillaGMOD/bin to the new install folder.
		// we copy over everything in VanillaGMOD/garrysmod to the new install folder, except for the addons, saves, settings, download, cache folder. (ignore filetypes like .dem and .log)
		// we create a blank RTXGMOD/garrysmod/addons folder.
		// We symlink VanillaGMOD/garrysmod/saves entirely to the new install folder.
		// We symlink VanillaGMOD/garrysmod/settings entirely to the new install folder.
		public static async Task<bool> CreateRTXInstallAsync(bool createform = true)
		{
			bool success = false;
			string resultMessage = "";

			// First show confirmation dialog
			if (!ShowConfirmationDialog())
			{
				LogProgress("Installation cancelled by user.", 0);
				OnInstallationCompleted?.Invoke(false, "Installation cancelled by user.");
				return false;
			}

			ProgressForm progressForm = null;
			if (createform)
			{
				// Create and show the progress form
				progressForm = new ProgressForm();
				// Subscribe to progress updates
				OnProgressUpdate += progressForm.UpdateProgress;

				// Show the form
				progressForm.Show();
			}

			// Run the installation in a background task
			await Task.Run(() =>
			{
				try
				{
					PerformInstallation();
					LogProgress("RTX installation completed successfully!", 100);
					success = true;
					resultMessage = "Installation completed successfully!";
				}
				catch (Exception ex)
				{
					LogProgress($"Error during installation: {ex.Message}", 100);
					success = false;
					resultMessage = ex.Message;
					MessageBox.Show($"An error occurred during installation:\n\n{ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			});

			if (createform)
			{
				// Unsubscribe from events
				OnProgressUpdate -= progressForm.UpdateProgress;
			}

			// Trigger the completed event
			OnInstallationCompleted?.Invoke(success, resultMessage);
			return success;
		}

		public static void CleanupEvents()
		{
			OnProgressUpdate = null;
			OnInstallationCompleted = null;
		}
		private static void PerformInstallation()
		{
			var vanillaPath = GetVanillaInstallFolder();
			var newInstallPath = GetThisInstallFolder();

			int totalSteps = 12; // Total number of installation steps
			int currentStep = 0;

			LogProgress($"Creating RTX install from {vanillaPath} to {newInstallPath}",
				(currentStep++ * 100) / totalSteps);

			// 1. Copy bin folder
			LogProgress("Copying bin folder...", (currentStep * 100) / totalSteps);
			CopyDirectory(Path.Combine(vanillaPath, "bin"), Path.Combine(newInstallPath, "bin"));

			// 2. Create garrysmod folder if it doesn't exist
			var newGarrymodPath = Path.Combine(newInstallPath, "garrysmod");
			var vanillaGarrymodPath = Path.Combine(vanillaPath, "garrysmod");
			Directory.CreateDirectory(newGarrymodPath);
			LogProgress("Created garrysmod folder", (currentStep++ * 100) / totalSteps);

			// 3. Copy gmod.exe
			LogProgress("Copying gmod.exe", (currentStep * 100) / totalSteps);
			if (File.Exists(Path.Combine(vanillaPath, "gmod.exe")))
			{
				File.Copy(Path.Combine(vanillaPath, "gmod.exe"), Path.Combine(newInstallPath, "gmod.exe"), true);
			}
			else if (File.Exists(Path.Combine(vanillaPath, "hl2.exe")))
			{
				// Fallback to hl2.exe if gmod.exe doesn't exist
				LogProgress("gmod.exe not found, copying hl2.exe instead", (currentStep * 100) / totalSteps);
				File.Copy(Path.Combine(vanillaPath, "hl2.exe"), Path.Combine(newInstallPath, "hl2.exe"), true);
			}
			currentStep++;

			// 4. Copy steam_appid.txt
			LogProgress("Copying steam_appid.txt", (currentStep * 100) / totalSteps);
			if (File.Exists(Path.Combine(vanillaPath, "steam_appid.txt")))
			{
				File.Copy(Path.Combine(vanillaPath, "steam_appid.txt"), Path.Combine(newInstallPath, "steam_appid.txt"), true);
			}
			currentStep++;

			// 5. Create a set of files we'll symlink to avoid copying them in step 6
			var symlinkedFiles = new HashSet<string>();

			// 6. Symlink VPK files (*.vpk)
			LogProgress("Symlinking VPK files...", (currentStep * 100) / totalSteps);
			foreach (var vpkFile in Directory.GetFiles(vanillaGarrymodPath, "*.vpk"))
			{
				var fileName = Path.GetFileName(vpkFile);
				var targetPath = Path.Combine(newGarrymodPath, fileName);

				if (!File.Exists(targetPath))
				{
					try
					{
						CreateFileSymbolicLink(targetPath, vpkFile);
						symlinkedFiles.Add(fileName.ToLower()); // Add to our tracking list
						LogProgress($"  Symlinked {fileName}", (currentStep * 100) / totalSteps);
					}
					catch (Exception ex)
					{
						LogProgress($"  Failed to symlink {fileName}: {ex.Message}", (currentStep * 100) / totalSteps);
					}
				}
			}
			currentStep++;

			// 7. List of external folders to symlink
			var externalFoldersToSymlink = new List<string> { "sourceengine", "platform" };

			// Symlink external folders
			LogProgress("Symlinking external folders...", (currentStep * 100) / totalSteps);
			foreach (var folderName in externalFoldersToSymlink)
			{
				var vanillaFolderPath = Path.Combine(vanillaPath, folderName);
				var newFolderPath = Path.Combine(newInstallPath, folderName);

				if (Directory.Exists(vanillaFolderPath) && !Directory.Exists(newFolderPath))
				{
					LogProgress($"Symlinking {folderName} folder...", (currentStep * 100) / totalSteps);
					try
					{
						CreateDirectorySymbolicLink(newFolderPath, vanillaFolderPath);
					}
					catch (Exception ex)
					{
						LogProgress($"Failed to symlink {folderName} folder: {ex.Message}", (currentStep * 100) / totalSteps);
					}
				}
			}
			currentStep++;

			// 8. List of garrysmod subfolders to symlink instead of copy
			var garrymodFoldersToSymlink = new List<string> {
				"saves", "dupes", "demos", "settings", "cache",
				"materials", "models", "maps", "screenshots", "videos", "download"
			};

			// List of excluded folders (these won't be copied)
			var excludedFolders = new HashSet<string>(garrymodFoldersToSymlink.Select(f => f.ToLower()));
			excludedFolders.Add("addons"); // We'll create a blank addons folder

			var excludedExtensions = new HashSet<string> { ".dem", ".log" };

			// 9. Copy garrysmod folder contents (except for excluded folders and already symlinked files)
			LogProgress("Copying garrysmod folder contents (excluding specified folders)...", (currentStep * 100) / totalSteps);

			foreach (var file in Directory.GetFiles(vanillaGarrymodPath, "*", SearchOption.TopDirectoryOnly))
			{
				var fileName = Path.GetFileName(file);
				var fileExt = Path.GetExtension(file).ToLower();

				// Skip files that we've already symlinked
				if (symlinkedFiles.Contains(fileName.ToLower()))
				{
					LogProgress($"  Skipping {fileName} (already symlinked)", (currentStep * 100) / totalSteps);
					continue;
				}

				if (!excludedExtensions.Contains(fileExt))
				{
					try
					{
						File.Copy(file, Path.Combine(newGarrymodPath, fileName), true);
					}
					catch (Exception ex)
					{
						LogProgress($"  Failed to copy {fileName}: {ex.Message}", (currentStep * 100) / totalSteps);
					}
				}
			}
			currentStep++;

			// 10
			LogProgress("Copying non-excluded directories...", (currentStep * 100) / totalSteps);
			foreach (var dir in Directory.GetDirectories(vanillaGarrymodPath, "*", SearchOption.TopDirectoryOnly))
			{
				var dirName = Path.GetFileName(dir);

				if (!excludedFolders.Contains(dirName.ToLower()))
				{
					try
					{
						CopyDirectory(dir, Path.Combine(newGarrymodPath, dirName));
						LogProgress($"  Copied directory {dirName}", (currentStep * 100) / totalSteps);
					}
					catch (Exception ex)
					{
						LogProgress($"  Failed to copy directory {dirName}: {ex.Message}", (currentStep * 100) / totalSteps);
					}
				}
			}
			currentStep++;

			// 11. Create a blank addons folder
			LogProgress("Creating blank addons folder...", (currentStep * 100) / totalSteps);
			Directory.CreateDirectory(Path.Combine(newGarrymodPath, "addons"));
			currentStep++;

			// 12. Symlink all the garrysmod folders from our list
			LogProgress("Symlinking garrysmod subfolders...", (currentStep * 100) / totalSteps);
			foreach (var folderName in garrymodFoldersToSymlink)
			{
				var vanillaFolderPath = Path.Combine(vanillaGarrymodPath, folderName);
				var newFolderPath = Path.Combine(newGarrymodPath, folderName);

				if (Directory.Exists(vanillaFolderPath) && !Directory.Exists(newFolderPath))
				{
					LogProgress($"  Symlinking {folderName} folder...", (currentStep * 100) / totalSteps);
					try
					{
						CreateDirectorySymbolicLink(newFolderPath, vanillaFolderPath);
					}
					catch (Exception ex)
					{
						LogProgress($"  Failed to symlink {folderName} folder: {ex.Message}", (currentStep * 100) / totalSteps);
					}
				}
			}
			currentStep++;
		}

		// Original method for backward compatibility
		public static void CreateRTXInstall()
		{
			// Reset the flag at the start of a new installation
			_userAcceptedSymlinkFailures = false;

			if (ShowConfirmationDialog())
			{
				try
				{
					PerformInstallation();
					LogProgress("RTX installation complete!", 100);
				}
				catch (OperationCanceledException ex)
				{
					LogProgress($"Installation cancelled: {ex.Message}", 100);
					MessageBox.Show(ex.Message, "Installation Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			else
			{
				LogProgress("Installation cancelled by user.", 0);
			}
		}

		// Helper method to copy directories recursively
		private static void CopyDirectory(string sourceDir, string destinationDir)
		{
			// Create the destination directory if it doesn't exist
			Directory.CreateDirectory(destinationDir);

			// Copy files
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				var fileName = Path.GetFileName(file);
				var destFile = Path.Combine(destinationDir, fileName);
				File.Copy(file, destFile, true);
			}

			// Copy subdirectories recursively
			foreach (var directory in Directory.GetDirectories(sourceDir))
			{
				var dirName = Path.GetFileName(directory);
				var destDir = Path.Combine(destinationDir, dirName);
				CopyDirectory(directory, destDir);
			}
		}
	}

	// Progress Form to display installation progress
	public class ProgressFormOld : Form
	{
		private ProgressBar progressBar;
		private RichTextBox logTextBox;
		private Label statusLabel;

		public ProgressFormOld()
		{
			// Form setup
			this.Text = "RTX Installation Progress";
			this.Size = new Size(600, 500);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;

			// Status label
			statusLabel = new Label
			{
				Text = "Installing RTX Mod...",
				Dock = DockStyle.Top,
				Padding = new Padding(10),
				Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
			};
			this.Controls.Add(statusLabel);

			// Progress bar
			progressBar = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 30,
				Margin = new Padding(10),
				Style = ProgressBarStyle.Continuous
			};
			this.Controls.Add(progressBar);

			// Log text box
			logTextBox = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BackColor = Color.Black,
				ForeColor = Color.LightGreen,
				Font = new Font("Consolas", 9),
				Margin = new Padding(10)
			};
			this.Controls.Add(logTextBox);

			// Make sure form doesn't close until we're done
			this.FormClosing += (s, e) =>
			{
				if (progressBar.Value < 100)
				{
					var result = MessageBox.Show(
						"Installation is still in progress. Are you sure you want to cancel?",
						"Cancel Installation",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning
					);
					if (result == DialogResult.No)
					{
						e.Cancel = true;
					}
				}
			};
		}

		// Method to update the progress bar and log
		public void UpdateProgress(string message, int progress)
		{
			if (this.InvokeRequired)
			{
				this.Invoke(new Action<string, int>(UpdateProgress), message, progress);
				return;
			}

			// Update progress bar
			progressBar.Value = Math.Min(progress, 100);

			// Update status if it's the last message
			if (progress >= 100)
			{
				statusLabel.Text = "Installation Complete!";
				statusLabel.ForeColor = Color.Green;
			}

			// Add message to log
			logTextBox.AppendText($"{message}\n");
			logTextBox.ScrollToCaret();
		}
	}
}
