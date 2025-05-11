using System.IO.Compression;

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

				// Step 6: Process additional dependencies defined in the fixes package
				progressForm.UpdateProgress("Processing additional launcher dependencies...", 90);
				bool dependenciesSuccess = await ProcessLauncherDependenciesAsync(progressForm, installDir);
				if (!dependenciesSuccess)
				{
					// Log or show a warning, but don't necessarily fail the whole install
					// As these might be optional dependencies.
					MessageBox.Show(
						"Warning: Could not process all additional dependencies specified in the fixes package.",
						"Dependency Warning",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
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
				// Restore original error handling, remove incorrect tempDir cleanup
				progressForm.UpdateProgress($"Error during Easy Install: {ex.Message}", 100);

				MessageBox.Show(
					$"Error during Easy Install: {ex.Message}\n\nYou may need to try the manual installation steps.",
					"Easy Install Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);

				// Optionally rethrow or handle as needed, but don't try to clean tempDir here
				// For now, just log the error and return false to indicate failure
				// throw; // Re-throwing might stop the progress form abruptly
				success = false; // Ensure success is false
				resultMessage = ex.Message; // Store message if needed elsewhere
			}
			finally
			{
				// Ensure the progress form is closed if it's still open
				if (progressForm != null && !progressForm.IsDisposed)
				{
					progressForm.Close();
				}
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

		// Process .launcherdependencies
		private async Task<bool> ProcessLauncherDependenciesAsync(ProgressForm progressForm, string installDir)
		{
			string dependenciesFilePath = Path.Combine(installDir, ".launcherdependencies");
			bool allSucceeded = true;

			if (!File.Exists(dependenciesFilePath))
			{
				progressForm.UpdateProgress("No .launcherdependencies file found, skipping.", 95);
				return true; // No dependencies file is not an error
			}

			progressForm.UpdateProgress("Found .launcherdependencies file. Processing...", 91);

			try
			{
				string[] dependencyLines = await File.ReadAllLinesAsync(dependenciesFilePath);
				int totalDependencies = dependencyLines.Length;
				int dependenciesProcessed = 0;

				foreach (string line in dependencyLines)
				{
					string trimmedLine = line.Trim();
					if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#")) // Skip empty lines and comments
						continue;

					string[] parts = trimmedLine.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length != 2)
					{
						progressForm.UpdateProgress($"Skipping invalid dependency line: {trimmedLine}", -1); // Use -1 for messages without progress change
						allSucceeded = false; // Mark as partial failure if format is wrong
						continue;
					}

					string url = parts[0];
					string relativeTargetPath = parts[1].Replace('\\', '/'); // Normalize path

					try
					{
						progressForm.UpdateProgress($"Installing dependency from {url}...", -1);
						await InstallDependencyAsync(url, installDir, relativeTargetPath, (message, progress) =>
						{
							// Remap progress within the 91-99% range for this step
							int baseProgress = 91 + (int)((float)dependenciesProcessed / totalDependencies * 8); // Allocate 8% total for all dependencies
							int remappedProgress = baseProgress + (int)(progress * (8.0f / totalDependencies)); // Scale sub-progress
							progressForm.UpdateProgress($"Dependency ({dependenciesProcessed + 1}/{totalDependencies}): {message}", remappedProgress);
						});
						dependenciesProcessed++;
					}
					catch (Exception depEx)
					{
						progressForm.UpdateProgress($"Failed to install dependency {url}: {depEx.Message}", -1);
						allSucceeded = false; // Mark as partial failure
					}
				}

				progressForm.UpdateProgress("Finished processing dependencies.", 99);
				return allSucceeded;
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error reading .launcherdependencies: {ex.Message}", 99);
				return false; // Reading the file itself failed
			}
		}

		// Install a single dependency
		private async Task InstallDependencyAsync(string zipUrl, string installDir, string relativeTargetPath, Action<string, int> progressCallback)
		{
			progressCallback?.Invoke("Starting dependency installation...", 0);

			// Create a temporary directory for downloading
			string tempDir = Path.Combine(Path.GetTempPath(), $"RTXDepInstall_{Path.GetRandomFileName()}");
			Directory.CreateDirectory(tempDir);

			long lastReportedMB = 0;
			int reportThresholdMB = 5; // Report every 5MB
			string zipFileName = Path.GetFileName(new Uri(zipUrl).AbsolutePath); // Get a filename from URL

			try
			{
				// Download the zip file
				string zipPath = Path.Combine(tempDir, zipFileName);
				progressCallback?.Invoke($"Downloading {zipFileName}...", 5);

				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher"); // GitHub might require User-Agent
					using (var response = await client.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode(); // Throw exception if download failed

						long totalBytes = response.Content.Headers.ContentLength ?? -1;
						using (var downloadStream = await response.Content.ReadAsStreamAsync())
						using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							var buffer = new byte[8192];
							long totalBytesRead = 0;
							int bytesRead;

							while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
							{
								await fileStream.WriteAsync(buffer, 0, bytesRead);
								totalBytesRead += bytesRead;

								if (totalBytes > 0)
								{
									long currentMB = totalBytesRead / 1048576;
									if (currentMB >= lastReportedMB + reportThresholdMB || totalBytesRead == totalBytes)
									{
										lastReportedMB = currentMB;
										int percentComplete = 10 + (int)((float)totalBytesRead / totalBytes * 40); // Progress 10% to 50%
										progressCallback?.Invoke($"Downloading: {currentMB} MB / {totalBytes / 1048576} MB", percentComplete);
									}
								}
								else
								{
									progressCallback?.Invoke($"Downloading: {totalBytesRead / 1048576} MB", 25); // Indeterminate progress
								}
							}
						}
					}
				}

				progressCallback?.Invoke("Download complete. Extracting...", 55);

				string targetFullPath = Path.Combine(installDir, relativeTargetPath);
				Directory.CreateDirectory(targetFullPath); // Ensure target directory exists

				await Task.Run(() => // Run extraction in background thread
				{
					using (var archive = ZipFile.OpenRead(zipPath))
					{
						// Determine the root directory name within the zip
						string rootDirPrefix = archive.Entries.FirstOrDefault()?.FullName.Split('/')[0] + "/";
						if (string.IsNullOrEmpty(rootDirPrefix) || !rootDirPrefix.EndsWith("/"))
						{
							rootDirPrefix = ""; // Handle zips without a single root folder
						}

						int totalEntries = archive.Entries.Count;
						int entriesProcessed = 0;

						foreach (var entry in archive.Entries)
						{
							// Skip directory entries explicitly - we create them as needed
							if (entry.FullName.EndsWith("/")) continue;

							// Construct the destination path
							string relativeEntryPath = entry.FullName;
							// Remove the root directory prefix if it exists
							if (!string.IsNullOrEmpty(rootDirPrefix) && relativeEntryPath.StartsWith(rootDirPrefix))
							{
								relativeEntryPath = relativeEntryPath.Substring(rootDirPrefix.Length);
							}

							// Skip empty relative paths (e.g., the root folder entry itself if it wasn't filtered)
							if (string.IsNullOrEmpty(relativeEntryPath)) continue;

							string destinationPath = Path.Combine(targetFullPath, relativeEntryPath);

							// Ensure the directory for the file exists
							string destinationDir = Path.GetDirectoryName(destinationPath);
							if (!Directory.Exists(destinationDir))
							{
								Directory.CreateDirectory(destinationDir);
							}

							// Extract the file, overwriting if it exists
							entry.ExtractToFile(destinationPath, true);

							entriesProcessed++;
							int progressPercent = 55 + (int)((float)entriesProcessed / totalEntries * 40); // Progress 55% to 95%
							progressCallback?.Invoke($"Extracting: {entriesProcessed} / {totalEntries}", progressPercent);
						}
					}
				});


				progressCallback?.Invoke("Extraction complete. Cleaning up...", 98);
			}
			finally
			{
				// Clean up temporary directory
				try { Directory.Delete(tempDir, true); } catch { /* Ignore cleanup errors */ }
			}

			progressCallback?.Invoke("Dependency installed successfully!", 100);
		}
	}
}