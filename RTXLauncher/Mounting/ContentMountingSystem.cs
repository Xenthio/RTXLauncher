using System.Diagnostics;

namespace RTXLauncher
{
	public static class ContentMountingSystem
	{
		// Mounting and unmounting content
		// the gameFolder is the folder name of the game, like "hl2rtx"
		// the installFolder is the folder name of the game in the steamapps\common folder, like "Half-Life 2: RTX", use GetInstallFolder to get the full path
		// the remixModFolder is the folder name of the mod in the installfolder\rtx-remix\mods folder, like "hl2rtx"

		// when mounting, these folders should be symlinked:

		// The source side content: (fullInstallPath)\(gameFolder) -> (garrysmodPath)\garrysmod\addons\mount-(gameFolder)
		// The remix mod: (fullInstallPath)\rtx-remix\mods\(remixModFolder) -> (garrysmodPath)\GarrysMod\rtx-remix\mods\mount-(gameFolder)-(remixModFolder)

		// examples:
		// The source side content: D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\hl2rtx -> D:\SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons\mount-hl2rtx
		// The source side content (for custom folder): D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\hl2rtx\custom\new_rtx_hands -> D:\SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons\mount-hl2rtx-new_rtx_hands
		// The remix mod: D:\SteamLibrary\steamapps\common\Half-Life 2 RTX\rtx-remix\mods\hl2rtx -> D:\SteamLibrary\steamapps\common\GarrysMod\rtx-remix\mods\mount-hl2rtx-hl2rtx

		// However, for source side content, the folder itself shouldn't be linked, but the models, and maps folder should be linked instead, and for materials all folders inside should be linked except for the materials\vgui and materias\dev folders
		// do this for the folder itself, aswell as all folders inside the custom folder

		/// <summary>
		/// Synchronous wrapper for MountGameAsync - for backward compatibility
		/// </summary>
		public static bool MountGame(string gameFolder, string installFolder, string remixModFolder)
		{
			return MountGameAsync(gameFolder, installFolder, remixModFolder).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Asynchronously mounts a game with RTXIO package preprocessing if needed
		/// </summary>
		public static async Task<bool> MountGameAsync(string gameFolder, string installFolder, string remixModFolder)
		{
			// Reset the flag at the start of a new mounting operation
			_userAcceptedSymlinkFailures = false;

			try
			{
				// Mount the content
				var installPath = SteamLibrary.GetGameInstallFolder(installFolder);
				if (installPath == null)
				{
					MessageBox.Show("Game not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}

				var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
				var sourceContentPath = Path.Combine(installPath, gameFolder);
				var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
				var remixModPath = Path.Combine(installPath, "rtx-remix", "mods", remixModFolder);
				var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);

				// Ensure source paths exist
				if (!Directory.Exists(sourceContentPath))
				{
					MessageBox.Show($"Source content folder not found at:\n{sourceContentPath}",
						"Mounting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}

				if (!Directory.Exists(remixModPath))
				{
					// This might be a warning rather than an error depending on your requirements
					var result = MessageBox.Show(
						$"RTX Remix mod folder not found at:\n{remixModPath}\n\nContinue anyway?",
						"Missing RTX Content", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

					if (result == DialogResult.No)
						return false;
				}

				// RTXIO Package Preprocessing - Check if the game has .pkg files that need extraction
				if (Directory.Exists(remixModPath) && RTXIOPackageManager.HasRTXIOPackageFiles(installPath, remixModFolder))
				{
					LogMounting("RTXIO package files detected, preprocessing required...", false);
					
					// Show progress dialog for RTXIO extraction
					using (var progressForm = new ProgressForm())
					{
						progressForm.Text = "RTXIO Package Extraction";
						progressForm.Show();
						
						// Ensure the form handle is created
						var handle = progressForm.Handle; // This forces handle creation
						
						// Create a proper event handler that can be unsubscribed
						RTXIOPackageManager.ProgressUpdateHandler rtxioProgressHandler = (message, progress) =>
						{
							if (progressForm.IsHandleCreated && !progressForm.IsDisposed)
							{
								try
								{
									progressForm.Invoke(() =>
									{
										progressForm.UpdateProgress(message, progress);
									});
								}
								catch (ObjectDisposedException)
								{
									// Form was disposed, ignore
								}
								catch (InvalidOperationException)
								{
									// Handle not created or form disposed, ignore
								}
							}
						};
						
						// Subscribe to progress updates
						RTXIOPackageManager.OnProgressUpdate += rtxioProgressHandler;

						try
						{
							LogMounting("Starting RTXIO package extraction...", false);
							bool extractionSuccess = await RTXIOPackageManager.ExtractPackageFilesAsync(installPath, remixModFolder);
							
							if (!extractionSuccess)
							{
								LogMounting("RTXIO package extraction failed", true);
								MessageBox.Show(
									"Failed to extract RTXIO package files. The game may not work correctly without these assets.\n\n" +
									"Common causes:\n" +
									"• Automatic download failed (GitHub ZIP doesn't include packman tools)\n" +
									"• Internet connection issues\n" +
									"• Dependency update script failed\n\n" +
									"Solutions:\n" +
									"• Install dxvk-remix manually: git clone https://github.com/NVIDIAGameWorks/dxvk-remix.git\n" +
									"• Copy RTXIO tools to: ./launcherdeps/rtxio/bin/RtxIoResourceExtractor.exe\n" +
									"• Check README_RTXIO.md for detailed instructions",
									"RTXIO Extraction Failed", 
									MessageBoxButtons.OK, 
									MessageBoxIcon.Warning);
								
								// Ask user if they want to continue anyway
								var continueResult = MessageBox.Show(
									"Do you want to continue mounting without extracting the RTXIO packages?\n\n" +
									"Warning: The game may not display correctly or may crash.",
									"Continue Without RTXIO?",
									MessageBoxButtons.YesNo,
									MessageBoxIcon.Question);
								
								if (continueResult == DialogResult.No)
									return false;
							}
							else
							{
								LogMounting("RTXIO package extraction completed successfully", false);
							}
						}
						finally
						{
							// Properly unsubscribe from progress updates
							RTXIOPackageManager.OnProgressUpdate -= rtxioProgressHandler;
						}
					}
				}

				// USDA Fixes Application - Apply fixes for Half-Life 2: RTX
				if (Directory.Exists(remixModPath) && remixModFolder == "hl2rtx")
				{
					LogMounting("Applying USDA fixes for Half-Life 2: RTX...", false);
					
					// Show progress dialog for USDA fixes
					using (var progressForm = new ProgressForm())
					{
						progressForm.Text = "Applying USDA Fixes";
						progressForm.Show();
						
						// Ensure the form handle is created
						var handle = progressForm.Handle; // This forces handle creation
						
						// Create a proper event handler that can be unsubscribed
						RTXIOPackageManager.ProgressUpdateHandler usdaProgressHandler = (message, progress) =>
						{
							if (progressForm.IsHandleCreated && !progressForm.IsDisposed)
							{
								try
								{
									progressForm.Invoke(() =>
									{
										progressForm.UpdateProgress(message, progress);
									});
								}
								catch (ObjectDisposedException)
								{
									// Form was disposed, ignore
								}
								catch (InvalidOperationException)
								{
									// Handle not created or form disposed, ignore
								}
							}
						};
						
						// Subscribe to progress updates
						RTXIOPackageManager.OnProgressUpdate += usdaProgressHandler;

						try
						{
							LogMounting("Starting USDA fixes application...", false);
							bool usdaSuccess = await RTXIOPackageManager.ApplyUsdaFixesAsync(installPath, remixModFolder);
							
							if (!usdaSuccess)
							{
								LogMounting("USDA fixes application failed", true);
								MessageBox.Show(
									"Failed to apply USDA fixes. The game may have broken references in RTX mode.\n\n" +
									"Please ensure you have an internet connection and try again.",
									"USDA Fixes Failed", 
									MessageBoxButtons.OK, 
									MessageBoxIcon.Warning);
								
								// Ask user if they want to continue anyway
								var continueResult = MessageBox.Show(
									"Do you want to continue mounting without applying the USDA fixes?\n\n" +
									"Warning: Some RTX features may not work correctly.",
									"Continue Without USDA Fixes?",
									MessageBoxButtons.YesNo,
									MessageBoxIcon.Question);
								
								if (continueResult == DialogResult.No)
									return false;
							}
							else
							{
								LogMounting("USDA fixes applied successfully", false);
							}
						}
						finally
						{
							// Properly unsubscribe from progress updates
							RTXIOPackageManager.OnProgressUpdate -= usdaProgressHandler;
						}
					}
				}

				// Create the mount folder structure
				if (!Directory.Exists(Path.Combine(gmodPath, "rtx-remix", "mods")))
				{
					Directory.CreateDirectory(Path.Combine(gmodPath, "rtx-remix", "mods"));
				}

				// Link the remix mod if it exists
				if (Directory.Exists(remixModPath) && !Directory.Exists(remixModMountPath))
				{
					LogMounting($"Mounting RTX Remix content from: {remixModPath}", false);
					if (!CreateDirectorySymbolicLink(remixModMountPath, remixModPath))
					{
						LogMounting("WARNING: Failed to mount RTX Remix content", true);
					}
				}

				// Mount source content
				LogMounting($"Mounting source content from: {sourceContentPath}", false);
				LinkSourceContent(sourceContentPath, sourceContentMountPath);

				// Mount custom content if it exists
				var customPath = Path.Combine(sourceContentPath, "custom");
				if (Directory.Exists(customPath))
				{
					foreach (var folder in Directory.GetDirectories(customPath))
					{
						LogMounting($"Mounting custom content from: {folder}", false);
						LinkSourceContent(folder, Path.Combine($"{sourceContentMountPath}-{Path.GetFileName(folder)}"));
					}
				}

				LogMounting("Mounting completed successfully", false);
				return true;
			}
			catch (OperationCanceledException ex)
			{
				LogMounting($"Mounting cancelled: {ex.Message}", true);
				MessageBox.Show(ex.Message, "Mounting Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}
			catch (Exception ex)
			{
				LogMounting($"Error during mounting: {ex.Message}", true);
				MessageBox.Show($"An error occurred while mounting content:\n\n{ex.Message}",
					"Mounting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		// Link the content of the source content/custom folder
		private static void LinkSourceContent(string path, string destinationMountPath)
		{
			// create path
			Directory.CreateDirectory(destinationMountPath);

			// link the models folder
			if (Directory.Exists(Path.Combine(path, "models")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "models")))
				{
					CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "models"), Path.Combine(path, "models"));
				}
			}

			// link the maps folder
			if (Directory.Exists(Path.Combine(path, "maps")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "maps")))
				{
					CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "maps"), Path.Combine(path, "maps"));
				}
			}

			// link the materials folder, except for vgui and dev folders
			if (Directory.Exists(Path.Combine(path, "materials")))
			{
				if (!Directory.Exists(Path.Combine(destinationMountPath, "materials")))
				{
					Directory.CreateDirectory(Path.Combine(destinationMountPath, "materials"));
				}

				var dontLink = new List<string> { "vgui", "dev", "editor", "perftest", "tools" };
				foreach (var folder in Directory.GetDirectories(Path.Combine(path, "materials")))
				{
					var folderName = Path.GetFileName(folder);
					if (!dontLink.Contains(folderName))
					{
						CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "materials", folderName), folder);
					}
				}
			}
		}

		// Add a flag to track if the user has already been prompted about symlink failures
		private static bool _userAcceptedSymlinkFailures = false;

		// Add events for progress reporting if needed
		public delegate void MountingProgressHandler(string message, bool isError);
		public static event MountingProgressHandler OnMountingProgress;

		// Modified symlink creation method with user prompting
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
						"Content mounting requires symbolic links to function correctly.\n\n" +
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
								LogMounting($"Failed to copy directory: {ex2.Message}", true);
								bool continueInstallation = PromptContinueWithout(
									$"Failed to copy directory to {Path.GetFileName(path)}. " +
									$"Error: {ex2.Message}\n\n" +
									"Would you like to continue without this content?\n" +
									"(Some or all Remix replaced content may not be visible in-game)");

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
								"Would you like to continue without this content?\n" +
								"(Some or all Remix replaced content may not be visible in-game)");
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

				LogMounting($"ERROR: Insufficient privileges to create symlink: {Path.GetFileName(path)}", true);
				return false;
			}
		}

		// Helper for logging
		private static void LogMounting(string message, bool isError)
		{
			System.Diagnostics.Debug.WriteLine(message);
			OnMountingProgress?.Invoke(message, isError);
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
						"RTX Content Mounting Error",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					result = (dialogResult == DialogResult.Yes);
				}));

				return result;
			}
			else
			{
				// Fallback for no UI context
				System.Diagnostics.Debug.WriteLine("No UI context available for symlink failure prompt. Message: " + message);
				return false;
			}
		}

		// Helper method to prompt the user about copying instead of symlink
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
						"RTX Content Mounting Error",
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
						"RTX Content Mounting Error",
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

		// Helper to restart the application as administrator
		private static void RestartAsAdmin()
		{
			try
			{
				// Get the current executable path
				string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

				// Create process start info
				var startInfo = new System.Diagnostics.ProcessStartInfo
				{
					UseShellExecute = true,
					WorkingDirectory = Environment.CurrentDirectory,
					FileName = exePath,
					Verb = "runas" // This triggers the UAC prompt
				};

				// Start the new process
				System.Diagnostics.Process.Start(startInfo);

				// Exit the current process
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to restart as administrator: {ex.Message}",
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		// Unmounting content - modified to add logging and error handling
		public static bool UnMountGame(string gameFolder, string installFolder, string remixModFolder)
		{
			try
			{
				// Unmount the content
				var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
				var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
				var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);

				// Delete the remix mod mount
				if (Directory.Exists(remixModMountPath))
				{
					LogMounting($"Unmounting RTX Remix content: {remixModMountPath}", false);
					Directory.Delete(remixModMountPath, true);
				}

				// Delete the source content mount
				if (Directory.Exists(sourceContentMountPath))
				{
					LogMounting($"Unmounting source content: {sourceContentMountPath}", false);
					Directory.Delete(sourceContentMountPath, true);
				}

				// Delete all custom source side content folders
				var customSourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons");
				foreach (var directory in Directory.GetDirectories(customSourceContentMountPath, "mount-" + gameFolder + "-*"))
				{
					LogMounting($"Unmounting custom content: {directory}", false);
					Directory.Delete(directory, true);
				}

				LogMounting("Unmounting completed successfully", false);
				return true;
			}
			catch (Exception ex)
			{
				LogMounting($"Error during unmounting: {ex.Message}", true);
				MessageBox.Show($"An error occurred while unmounting content:\n\n{ex.Message}",
					"Unmounting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		// Unchanged IsGameMounted method
		public static bool IsGameMounted(string gameFolder, string installFolder, string remixModFolder)
		{
			// Check if the content is mounted
			var gmodPath = GarrysModInstallSystem.GetThisInstallFolder();
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", "mount-" + gameFolder);
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-" + gameFolder + "-" + remixModFolder);
			return Directory.Exists(sourceContentMountPath) && Directory.Exists(remixModMountPath);
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
}

