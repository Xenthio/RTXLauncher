namespace RTXLauncher
{
	public partial class Form1
	{
		private void UpdateQuickInstallGroupVisibility()
		{
			// Get the current installation type
			string installType = GarrysModInstallSystem.GetInstallType(GarrysModInstallSystem.GetThisInstallFolder());

			// Show the QuickInstallGroup only when no valid installation is detected
			QuickInstallGroup.Visible = (installType == "unknown");
		}
		private async void OneClickEasyInstallButton_Click(object sender, EventArgs e)
		{
			// Disable the button to prevent multiple installation attempts
			OneClickEasyInstallButton.Enabled = false;

			await RefreshPackageInfo();

			await PerformEasyInstallAsync();

			// Re-enable the button
			OneClickEasyInstallButton.Enabled = true;

			// Refresh install info and update UI visibility
			RefreshInstallInfo();
		}

		private async Task<bool> PerformEasyInstallAsync()
		{
			bool success = false;
			string resultMessage = "";

			// First show confirmation dialog
			DialogResult result = MessageBox.Show(
				"This will perform a complete installation with recommended settings:\n\n" +
				"• Create a new RTX installation (if needed)\n" +
				"• Install the latest official RTX Remix\n" +
				"• Apply recommended patches\n" +
				"• Install recommended fixes package\n\n" +
				"Do you want to continue?",
				"Easy Install Confirmation",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			if (result != DialogResult.Yes)
			{
				return false;
			}

			// Create and show the progress form
			var progressForm = new ProgressForm();
			progressForm.Show();

			try
			{
				// Step 1: Check if we have an RTX installation already
				progressForm.UpdateProgress("Checking for existing RTX installation...", 5);
				string installDir = GarrysModInstallSystem.GetThisInstallFolder();
				string installType = GarrysModInstallSystem.GetInstallType(installDir);
				bool needToCreateInstall = (installType == "unknown");

				// Step 2: Create RTX installation if needed
				if (needToCreateInstall)
				{
					progressForm.UpdateProgress("No RTX installation detected. Creating a new one...", 10);

					// Create new installation
					bool installSuccess = await CreateInstallationAsync(progressForm);
					if (!installSuccess)
					{
						throw new Exception("Failed to create RTX installation.");
					}

					// Refresh paths after installation
					installDir = GarrysModInstallSystem.GetThisInstallFolder();
					installType = GarrysModInstallSystem.GetInstallType(installDir);
				}

				// Step 3: Install the latest RTX Remix
				progressForm.UpdateProgress("Fetching latest RTX Remix release...", 30);

				// Install the latest RTX Remix
				bool remixSuccess = await InstallLatestRemixAsync(progressForm, installType);
				if (!remixSuccess)
				{
					throw new Exception("Failed to install RTX Remix.");
				}

				// Step 4: Apply patches
				progressForm.UpdateProgress("Applying recommended patches...", 60);

				// Apply recommended patches
				bool patchesSuccess = await ApplyRecommendedPatchesAsync(progressForm, installDir);
				if (!patchesSuccess)
				{
					throw new Exception("Failed to apply patches.");
				}

				// Step 5: Install the recommended fixes package
				progressForm.UpdateProgress("Installing recommended fixes package...", 80);

				// Install recommended fixes package
				bool fixesSuccess = await InstallRecommendedFixesAsync(progressForm, installDir);
				if (!fixesSuccess)
				{
					throw new Exception("Failed to install fixes package.");
				}

				// Complete!
				progressForm.UpdateProgress("Easy Install completed successfully! You can now close this window.", 100);

				MessageBox.Show(
					"Easy Install completed successfully! Your RTX Garry's Mod installation is ready to use.",
					"Easy Install Complete",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);

				success = true;
				resultMessage = "Easy Install completed successfully!";
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error during Easy Install: {ex.Message}", 100);

				MessageBox.Show(
					$"Error during Easy Install: {ex.Message}\n\nYou may need to try the manual installation steps.",
					"Easy Install Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);

				success = false;
				resultMessage = ex.Message;
			}

			return success;
		}

		private async Task<bool> CreateInstallationAsync(ProgressForm progressForm)
		{
			bool installationComplete = false;
			bool installationSuccess = false;

			// Set up installation event handlers
			GarrysModInstallSystem.CleanupEvents();

			// Hook up progress updates
			GarrysModInstallSystem.OnProgressUpdate += (message, progress) =>
			{
				// Remap progress to 10-30% range of our overall process
				int remappedProgress = 10 + (int)(progress * 0.2); // 20% of our total progress
				progressForm.UpdateProgress($"Creating installation: {message}", remappedProgress);
			};

			// Hook up completion handler
			GarrysModInstallSystem.OnInstallationCompleted += (success, message) =>
			{
				installationComplete = true;
				installationSuccess = success;
			};

			// Start the installation
			await GarrysModInstallSystem.CreateRTXInstallAsync(false);

			// Wait for installation to complete
			while (!installationComplete)
			{
				await Task.Delay(100); // Short delay to avoid CPU spinning
			}

			return installationSuccess;
		}

		private async Task<bool> InstallLatestRemixAsync(ProgressForm progressForm, string installType)
		{
			try
			{
				// Use the official NVIDIA release
				var remixSource = _remixSources["sambow23/dxvk-remix-gmod"];

				// Get the latest release
				List<GitHubRelease> releases = await GitHubAPI.FetchReleasesAsync(remixSource.Owner, remixSource.Repo);

				if (releases.Count == 0)
				{
					throw new Exception("Could not find any RTX Remix releases");
				}

				// Get the latest release (sorted by published date)
				GitHubRelease latestRelease = releases.OrderByDescending(r => r.PublishedAt).First();

				progressForm.UpdateProgress($"Installing RTX Remix {latestRelease.Name}...", 35);

				// Install RTX Remix
				await RTXRemix.Install(
					latestRelease,
					remixSource.Owner,
					remixSource.Repo,
					installType,
					Application.StartupPath,
					(message, progress) =>
					{
						// Remap progress to 35-60% range
						int remappedProgress = 35 + (int)(progress * 0.25); // 25% of our total progress
						progressForm.UpdateProgress($"RTX Remix: {message}", remappedProgress);
					}
				);

				return true;
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error installing RTX Remix: {ex.Message}", 60);
				return false;
			}
		}

		private async Task<bool> ApplyRecommendedPatchesAsync(ProgressForm progressForm, string installDir)
		{
			try
			{
				// Determine if the installation is x64
				string installType = GarrysModInstallSystem.GetInstallType(installDir);
				bool isX64 = installType == "gmod_x86-64";

				// Choose the appropriate patches source based on architecture
				string patchesSource = isX64
					? "sambow23/SourceRTXTweaks"   // For x64 installations
					: "BlueAmulet/SourceRTXTweaks"; // For x86 installations

				progressForm.UpdateProgress($"Detected {(isX64 ? "x64" : "x86")} installation, using {patchesSource} patches", 62);

				var (owner, repo, filePath) = _patchSources[patchesSource];

				// Get the patch file content
				string patchFileContent;

				if (_cachedPatchFiles.ContainsKey(patchesSource))
				{
					patchFileContent = _cachedPatchFiles[patchesSource];
				}
				else
				{
					patchFileContent = await FetchPatchFileAsync(owner, repo, filePath);
					_cachedPatchFiles[patchesSource] = patchFileContent;
				}

				progressForm.UpdateProgress("Applying patches...", 65);

				// Apply the patches
				await PatchingSystem.ApplyPatches(installDir, patchFileContent,
					(message, progress) =>
					{
						// Remap progress to 65-80% range
						int remappedProgress = 65 + (int)(progress * 0.15); // 15% of our total progress
						progressForm.UpdateProgress($"Patching: {message}", remappedProgress);
					}
				);

				return true;
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error applying patches: {ex.Message}", 80);
				return false;
			}
		}

		private async Task<bool> InstallRecommendedFixesAsync(ProgressForm progressForm, string installDir)
		{
			try
			{
				// Use the recommended fixes package (Xenthio/gmod-rtx-fixes-2)
				var fixesSource = _packageSources["Xenthio/gmod-rtx-fixes-2 (Any)"];

				// Get the latest fixes release
				List<GitHubRelease> fixesReleases = await GitHubAPI.FetchReleasesAsync(fixesSource.Owner, fixesSource.Repo);

				if (fixesReleases.Count == 0)
				{
					throw new Exception("Could not find any fixes packages");
				}

				// Find releases with -launcher.zip first, then any zip
				var launcherReleases = fixesReleases
					.Where(r => r.Assets.Any(a => a.Name.EndsWith("-launcher.zip")))
					.OrderByDescending(r => r.PublishedAt)
					.ToList();

				var allZipReleases = fixesReleases
					.Where(r => r.Assets.Any(a => a.Name.EndsWith(".zip")))
					.OrderByDescending(r => r.PublishedAt)
					.ToList();

				GitHubRelease fixesRelease = launcherReleases.FirstOrDefault() ?? allZipReleases.FirstOrDefault();

				if (fixesRelease == null)
				{
					throw new Exception("Could not find a suitable fixes package");
				}

				// Find the asset to download (prioritize -launcher.zip)
				GitHubAsset fixesAsset = fixesRelease.Assets.FirstOrDefault(a => a.Name.EndsWith("-launcher.zip")) ??
										 fixesRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));

				if (fixesAsset == null)
				{
					throw new Exception("Could not find a suitable fixes package asset");
				}

				progressForm.UpdateProgress($"Installing fixes package: {fixesAsset.Name}...", 85);

				// Install the fixes package
				await InstallFixesPackage(fixesAsset, installDir, _defaultIgnorePatterns,
					(message, progress) =>
					{
						// Remap progress to 85-100% range
						int remappedProgress = 85 + (int)(progress * 0.15); // 15% of our total progress
						progressForm.UpdateProgress($"Fixes: {message}", remappedProgress);
					}
				);

				return true;
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error installing fixes package: {ex.Message}", 100);
				return false;
			}
		}
	}
}