using System.IO.Compression;

namespace RTXLauncher
{
	partial class Form1
	{
		private Dictionary<string, (string Owner, string Repo)> _remixSources = new Dictionary<string, (string, string)>
		{
			{ "(OFFICIAL) NVIDIAGameWorks/rtx-remix", ("NVIDIAGameWorks", "rtx-remix") },
			{ "sambow23/dxvk-remix-gmod", ("sambow23", "dxvk-remix-gmod") },
		};

		private Dictionary<string, (string Owner, string Repo, string InstallType)> _packageSources = new Dictionary<string, (string, string, string)>
		{
			{ "Xenthio/gmod-rtx-fixes-2 (Any)", ("Xenthio", "gmod-rtx-fixes-2", "Any") },
			{ "Xenthio/RTXFixes (gmod_main)", ("Xenthio", "RTXFixes", "gmod_main") }
		};

		private Dictionary<string, (string Owner, string Repo, string FilePath)> _patchSources = new Dictionary<string, (string, string, string)>
		{
			{ "BlueAmulet/SourceRTXTweaks", ("BlueAmulet", "SourceRTXTweaks", "applypatch.py") },
			{ "Xenthio/SourceRTXTweaks (outdated, here to test multiple repos)", ("Xenthio", "SourceRTXTweaks", "applypatch.py") }
		};

		private readonly string _defaultIgnorePatterns =
@"
# We ignore these because the installer does rtx remix installations for us, but some of these packages might contain them for easy manual installation

# 32bit Bridge
bin/.trex/*
bin/d3d8to9.dll
bin/d3d9.dll
bin/LICENSE.txt
bin/NvRemixLauncher32.exe
bin/ThirdPartyLicenses-bridge.txt
bin/ThirdPartyLicenses-d3d8to9.txt
bin/ThirdPartyLicenses-dxvk.txt

# Remix in 64 install
bin/win64/usd/*
bin/win64/artifacts_readme.txt
bin/win64/cudart64_12.dll
bin/win64/d3d9.dll
bin/win64/d3d9.pdb
bin/win64/GFSDK_Aftermath_Lib.x64.dll
bin/win64/NRC_Vulkan.dll
bin/win64/NRD.dll
bin/win64/NvLowLatencyVk.dll
bin/win64/nvngx_dlss.dll
bin/win64/nvngx_dlssd.dll
bin/win64/nvngx_dlssg.dll
bin/win64/NvRemixBridge.exe
bin/win64/nvrtc64_120_0.dll
bin/win64/nvrtc-builtins64_125.dll
bin/win64/rtxio.dll
bin/win64/tbb.dll
bin/win64/tbbmalloc.dll
bin/win64/usd_ms.dll
";

		private bool _hasInit = false;

		private void InitInstallPage()
		{
			if (_hasInit)
				return;
			_hasInit = true;

			RefreshInstallInfo();
			//this.Load += async (s, e) =>
			//{
			RefreshPackageInfo();
			//};
		}

		private async Task RefreshPackageInfo()
		{
			// Initialize remix source combo box
			PopulateRemixSourceComboBox();
			remixSourceComboBox.SelectedIndexChanged += RemixSourceComboBox_SelectedIndexChanged;

			// Initial population of remix releases
			await PopulateRemixReleasesComboBoxAsync();

			// Initialize package source combo box
			PopulatePackageSourceComboBox();
			packageSourceComboBox.SelectedIndexChanged += PackageSourceComboBox_SelectedIndexChanged;
			InstallFixesPackageButton.Click += InstallFixesPackageButton_ClickAsync;

			// Initially populate the version combo box based on the default source
			await PopulateFixesVersionComboBoxAsync();

			// Initialize patch sources combo box
			PopulatePatchSourceComboBox();
			patchesSourceComboBox.SelectedIndexChanged += PatchesSourceComboBox_SelectedIndexChanged;
			ApplyPatchesButton.Click += ApplyPatchesButton_ClickAsync;
		}

		private async void CreateInstallButton_ClickAsync(object sender, EventArgs e)
		{
			try
			{
				GarrysModInstallSystem.CleanupEvents();
				GarrysModInstallSystem.OnInstallationCompleted += (success, message) =>
				{
					MessageBox.Show(success ? "Installation completed successfully." : "Installation failed.", "Installation", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
					RefreshInstallInfo();
				};
				await GarrysModInstallSystem.CreateRTXInstallAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Re-fresh everything
			}
		}
		private async void UpdateInstallButton_ClickAsync(object sender, EventArgs e)
		{
			// Disable the button to prevent multiple update operations
			UpdateInstallButton.Enabled = false;

			try
			{
				await GarrysModUpdateSystem.ShowUpdateDialogAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Re-enable the button when done
				UpdateInstallButton.Enabled = true;
			}
		}
		private async void RemixSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (remixSourceComboBox.SelectedItem == null)
				return;

			// Update the releases based on selected repository
			await PopulateRemixReleasesComboBoxAsync();
		}

		private void PopulateRemixSourceComboBox()
		{
			remixSourceComboBox.Items.Clear();
			foreach (var source in _remixSources.Keys)
			{
				remixSourceComboBox.Items.Add(source);
			}

			// Select the default NVIDIA source
			string defaultSource = "(OFFICIAL) NVIDIAGameWorks/rtx-remix";
			int defaultIndex = remixSourceComboBox.Items.IndexOf(defaultSource);

			if (defaultIndex >= 0)
			{
				remixSourceComboBox.SelectedIndex = defaultIndex;
			}
			else if (remixSourceComboBox.Items.Count > 0)
			{
				// Fallback if the default source is not found
				remixSourceComboBox.SelectedIndex = 0;
			}
		}

		private async Task PopulateRemixReleasesComboBoxAsync()
		{
			// Show loading indicator or disable the ComboBox
			remixReleaseComboBox.Enabled = false;
			remixReleaseComboBox.Items.Clear();
			remixReleaseComboBox.Items.Add("Loading releases...");
			remixReleaseComboBox.SelectedIndex = 0;

			try
			{
				// Get selected source
				string selectedSource = remixSourceComboBox.SelectedItem?.ToString();
				if (selectedSource == null || !_remixSources.TryGetValue(selectedSource, out var sourceInfo))
				{
					// Use default if nothing selected
					sourceInfo = ("NVIDIAGameWorks", "rtx-remix");
				}

				// Fetch releases from the selected repository
				List<GitHubRelease> releases = await GitHubAPI.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);

				// Clear the ComboBox
				remixReleaseComboBox.Items.Clear();

				if (releases.Count > 0)
				{
					// Sort releases by published date (newest first)
					releases = releases.OrderByDescending(r => r.PublishedAt).ToList();

					// Add releases to ComboBox
					foreach (var release in releases)
					{
						remixReleaseComboBox.Items.Add(release);
					}

					// Select the latest release
					remixReleaseComboBox.SelectedIndex = 0;
				}
				else
				{
					remixReleaseComboBox.Items.Add("No releases found");
					remixReleaseComboBox.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				remixReleaseComboBox.Items.Clear();
				remixReleaseComboBox.Items.Add($"Error: {ex.Message}");
				remixReleaseComboBox.SelectedIndex = 0;
			}

			remixReleaseComboBox.Enabled = true;
		}

		private async void InstallRTXRemixButton_Click(object sender, EventArgs e)
		{
			if (!(remixReleaseComboBox.SelectedItem is GitHubRelease selectedRelease))
			{
				MessageBox.Show("Please select a release to install.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// Disable the button during installation
			InstallRTXRemixButton.Enabled = false;

			// Create and show the progress form
			var progressForm = new ProgressForm();
			progressForm.Show();
			progressForm.UpdateProgress("Preparing RTX Remix installation...", 0);

			try
			{
				// Get the selected source
				string selectedSource = remixSourceComboBox.SelectedItem?.ToString();
				if (string.IsNullOrEmpty(selectedSource) || !_remixSources.TryGetValue(selectedSource, out var sourceInfo))
				{
					// Use default if nothing selected
					sourceInfo = ("NVIDIAGameWorks", "rtx-remix");
				}

				// Use the static class method to install RTX Remix
				await RTXRemix.Install(
					selectedRelease,
					sourceInfo.Owner,
					sourceInfo.Repo,
					GarrysModInstallSystem.GetInstallType(GarrysModInstallSystem.GetThisInstallFolder()),
					Application.StartupPath,
					progressForm.UpdateProgress
				);

				progressForm.UpdateProgress("RTX Remix installed successfully! You can close this window.", 100);
				MessageBox.Show("RTX Remix has been installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				progressForm.UpdateProgress($"Error: {ex.Message}", 100);
				MessageBox.Show($"Error installing RTX Remix: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Re-enable the button
				InstallRTXRemixButton.Enabled = true;
			}
		}

		private void RefreshInstallInfo()
		{
			// Refresh the install info

			var vanillapath = GarrysModInstallSystem.GetVanillaInstallFolder();
			var vanillatype = GarrysModInstallSystem.GetInstallType(vanillapath);
			VanillaInstallType.Text = vanillatype;
			VanillaInstallPath.Text = vanillapath;

			if (vanillatype == "unknown") VanillaInstallType.Text = "Not installed / not found";

			var thispath = GarrysModInstallSystem.GetThisInstallFolder();
			var thistype = GarrysModInstallSystem.GetInstallType(thispath);
			ThisInstallType.Text = thistype;
			ThisInstallPath.Text = thispath;

			if (thistype == "unknown")
			{
				ThisInstallType.Text = "There's no install here, create one!";
				CreateInstallButton.Enabled = true;
				UpdateInstallButton.Enabled = false;
			}
			else
			{
				CreateInstallButton.Enabled = false;
				UpdateInstallButton.Enabled = true;
			}

			// Update visibility of the QuickInstallGroup
			UpdateQuickInstallGroupVisibility();
		}

		private void PopulatePackageSourceComboBox()
		{
			packageSourceComboBox.Items.Clear();
			foreach (var source in _packageSources.Keys)
			{
				packageSourceComboBox.Items.Add(source);
			}

			// Find and select "Xenthio/gmod-rtx-fixes-2 (Any)" by default
			string defaultSource = "Xenthio/gmod-rtx-fixes-2 (Any)";
			int defaultIndex = packageSourceComboBox.Items.IndexOf(defaultSource);

			if (defaultIndex >= 0)
			{
				packageSourceComboBox.SelectedIndex = defaultIndex;
			}
			else if (packageSourceComboBox.Items.Count > 0)
			{
				// Fallback if the default source is not found
				packageSourceComboBox.SelectedIndex = 0;
			}
		}

		private async void PackageSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (packageSourceComboBox.SelectedItem == null)
				return;

			await PopulateFixesVersionComboBoxAsync();
		}

		private async Task PopulateFixesVersionComboBoxAsync()
		{
			// Get selected source
			string selectedSource = packageSourceComboBox.SelectedItem?.ToString();
			if (selectedSource == null || !_packageSources.TryGetValue(selectedSource, out var sourceInfo))
				return;

			// Show loading indicator
			packageVersionComboBox.Enabled = false;
			packageVersionComboBox.Items.Clear();
			packageVersionComboBox.Items.Add("Loading releases...");
			packageVersionComboBox.SelectedIndex = 0;

			try
			{
				// Fetch releases for the selected repository
				List<GitHubRelease> releases = await GitHubAPI.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);

				// Clear the loading text
				packageVersionComboBox.Items.Clear();

				if (releases.Count > 0)
				{
					// Sort releases by published date (newest first)
					releases = releases.OrderByDescending(r => r.PublishedAt).ToList();

					// First find releases that have a -launcher.zip file
					var launcherReleases = releases
						.Where(r => r.Assets.Any(a => a.Name.EndsWith("-launcher.zip")))
						.ToList();

					// Then find all releases that have any zip file
					var allZipReleases = releases
						.Where(r => r.Assets.Any(a => a.Name.EndsWith(".zip")))
						.Except(launcherReleases)
						.ToList();

					// Combine the lists, with launcher releases first
					var combinedReleases = launcherReleases.Concat(allZipReleases).ToList();

					if (combinedReleases.Count > 0)
					{
						// Add releases to ComboBox
						foreach (var release in combinedReleases)
						{
							// Mark launcher packages for easier identification
							if (launcherReleases.Contains(release))
							{
								release.Name = $"{release.Name} [Launcher]";
							}
							packageVersionComboBox.Items.Add(release);
						}

						// Select the latest release
						packageVersionComboBox.SelectedIndex = 0;
					}
					else
					{
						packageVersionComboBox.Items.Add("No compatible packages found");
						packageVersionComboBox.SelectedIndex = 0;
					}
				}
				else
				{
					packageVersionComboBox.Items.Add("No releases found");
					packageVersionComboBox.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				packageVersionComboBox.Items.Clear();
				packageVersionComboBox.Items.Add($"Error: {ex.Message}");
				packageVersionComboBox.SelectedIndex = 0;
			}

			packageVersionComboBox.Enabled = true;
		}

		private async void InstallFixesPackageButton_ClickAsync(object sender, EventArgs e)
		{
			// Validate selections
			if (!(packageSourceComboBox.SelectedItem is string selectedSource) ||
				!(packageVersionComboBox.SelectedItem is GitHubRelease selectedRelease))
			{
				MessageBox.Show("Please select a package source and version.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (!_packageSources.TryGetValue(selectedSource, out var sourceInfo))
			{
				MessageBox.Show("Invalid package source selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Find a suitable asset to download (prioritize -launcher.zip)
			GitHubAsset assetToDownload = selectedRelease.Assets.FirstOrDefault(a => a.Name.EndsWith("-launcher.zip"));

			// If no launcher zip is found, get any zip file
			if (assetToDownload == null)
			{
				assetToDownload = selectedRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
			}

			if (assetToDownload == null)
			{
				MessageBox.Show("This release does not contain any zip packages.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Create and show the progress form
			var progressForm = new ProgressForm();
			progressForm.Show();

			try
			{
				// Get the installation directory
				string installDir = GarrysModInstallSystem.GetThisInstallFolder();
				if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
				{
					throw new Exception("Invalid installation directory.");
				}

				await InstallFixesPackage(assetToDownload, installDir, _defaultIgnorePatterns, progressForm.UpdateProgress);
				MessageBox.Show("Fixes package installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error installing fixes package: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async Task InstallFixesPackage(GitHubAsset asset, string installDir, string ignorePatterns, Action<string, int> progressCallback)
		{
			progressCallback?.Invoke("Starting fixes package installation...", 0);

			// Create a temporary directory for downloading
			string tempDir = Path.Combine(Path.GetTempPath(), "RTXFixesTemp");
			if (Directory.Exists(tempDir))
				Directory.Delete(tempDir, true);
			Directory.CreateDirectory(tempDir);

			// Parse ignore patterns
			HashSet<string> ignoredPaths = ParseIgnorePatterns(ignorePatterns);

			// Check for .launcherignore in the zip file
			bool hasLauncherIgnore = false;
			string launcherIgnorePath = string.Empty;

			// Track the last reported progress
			long lastReportedMB = 0;
			int reportThresholdMB = 5; // Report every 5MB

			try
			{
				// Download the zip file
				string zipPath = Path.Combine(tempDir, asset.Name);
				progressCallback?.Invoke($"Downloading {asset.Name}...", 10);

				using (var client = new HttpClient())
				{
					using (var response = await client.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
					{
						if (!response.IsSuccessStatusCode)
						{
							throw new Exception($"Failed to download: {response.StatusCode}");
						}

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
									// Only report progress if we've downloaded at least reportThresholdMB more
									long currentMB = totalBytesRead / 1048576;
									if (currentMB >= lastReportedMB + reportThresholdMB || totalBytesRead == totalBytes)
									{
										lastReportedMB = currentMB;
										int percentComplete = (int)((float)totalBytesRead / totalBytes * 40) + 10;
										progressCallback?.Invoke(
											$"Downloading: {totalBytesRead / 1048576} MB / {totalBytes / 1048576} MB",
											percentComplete);
									}
								}
							}
						}
					}
				}

				progressCallback?.Invoke("Download complete. Checking package contents...", 50);

				// First, check if .launcherignore exists in the zip and parse it if it does
				using (var zip = ZipFile.OpenRead(zipPath))
				{
					var launcherIgnoreEntry = zip.Entries.FirstOrDefault(e =>
						e.Name == ".launcherignore" ||
						e.FullName == ".launcherignore");

					if (launcherIgnoreEntry != null)
					{
						hasLauncherIgnore = true;
						// Extract .launcherignore to temp directory
						launcherIgnorePath = Path.Combine(tempDir, ".launcherignore");
						launcherIgnoreEntry.ExtractToFile(launcherIgnorePath, true);

						// Parse the custom ignore patterns
						string customIgnorePatterns = File.ReadAllText(launcherIgnorePath);
						HashSet<string> customIgnoredPaths = ParseIgnorePatterns(customIgnorePatterns);

						// Merge with default ignore patterns
						foreach (var path in customIgnoredPaths)
						{
							ignoredPaths.Add(path);
						}

						progressCallback?.Invoke("Found .launcherignore file. Applying custom ignore patterns.", 55);
					}
				}

				progressCallback?.Invoke("Extracting files...", 60);

				// Extract files, respecting ignore patterns
				await Task.Run(() =>
				{
					using (var zip = ZipFile.OpenRead(zipPath))
					{
						// Filter out entries that match ignore patterns
						var entriesToExtract = zip.Entries
							.Where(entry => !ShouldIgnore(entry.FullName, ignoredPaths))
							.ToList();

						int totalEntries = entriesToExtract.Count;
						int entriesProcessed = 0;

						foreach (var entry in entriesToExtract)
						{
							// Skip .launcherignore file
							if (entry.Name == ".launcherignore" || entry.FullName == ".launcherignore")
								continue;

							string entryDestPath = Path.Combine(installDir, entry.FullName);

							// Create directory if needed
							if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
							{
								Directory.CreateDirectory(entryDestPath);
								continue;
							}

							// Create directory for file if needed
							string fileDirectory = Path.GetDirectoryName(entryDestPath);
							if (!Directory.Exists(fileDirectory))
								Directory.CreateDirectory(fileDirectory);

							// Extract file
							entry.ExtractToFile(entryDestPath, true);

							entriesProcessed++;
							int progressPercent = 60 + (int)((float)entriesProcessed / totalEntries * 35);
							progressCallback?.Invoke($"Extracting: {entriesProcessed} / {totalEntries}", progressPercent);
						}
					}
				});

				// Clean up
				progressCallback?.Invoke("Cleaning up temporary files...", 95);
				try { Directory.Delete(tempDir, true); } catch { }

				progressCallback?.Invoke("Fixes package installed successfully!", 100);
			}
			catch
			{
				// Clean up on error
				try { Directory.Delete(tempDir, true); } catch { }
				throw;
			}
		}

		private HashSet<string> ParseIgnorePatterns(string ignorePatterns)
		{
			HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrWhiteSpace(ignorePatterns))
				return result;

			string[] lines = ignorePatterns.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				string trimmedLine = line.Trim();

				// Skip comments and empty lines
				if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
					continue;

				// Normalize path separators
				trimmedLine = trimmedLine.Replace('\\', '/');

				// Remove leading slash if present
				if (trimmedLine.StartsWith("/"))
					trimmedLine = trimmedLine.Substring(1);

				// Add the pattern
				result.Add(trimmedLine);
			}

			return result;
		}

		private bool ShouldIgnore(string entryPath, HashSet<string> ignoredPaths)
		{
			// Normalize the entry path
			string normalizedPath = entryPath.Replace('\\', '/');

			// Remove leading slash if present
			if (normalizedPath.StartsWith("/"))
				normalizedPath = normalizedPath.Substring(1);

			// Check if the path exactly matches any ignore pattern
			if (ignoredPaths.Contains(normalizedPath))
				return true;

			// Check for wildcard patterns (currently only supports directory/* pattern)
			foreach (string pattern in ignoredPaths)
			{
				// Handle directory/* pattern
				if (pattern.EndsWith("/*") && normalizedPath.StartsWith(pattern.Substring(0, pattern.Length - 1)))
				{
					return true;
				}
			}

			return false;
		}

		// Add these methods for patch source handling
		private void PopulatePatchSourceComboBox()
		{
			patchesSourceComboBox.Items.Clear();
			foreach (var source in _patchSources.Keys)
			{
				patchesSourceComboBox.Items.Add(source);
			}

			// Find and select "BlueAmulet/SourceRTXTweaks" as default if available
			string defaultSource = "BlueAmulet/SourceRTXTweaks";
			int defaultIndex = patchesSourceComboBox.Items.IndexOf(defaultSource);

			if (defaultIndex >= 0)
			{
				patchesSourceComboBox.SelectedIndex = defaultIndex;
			}
			else if (patchesSourceComboBox.Items.Count > 0)
			{
				// Fallback if the default source is not found
				patchesSourceComboBox.SelectedIndex = 0;
			}
		}

		private async void PatchesSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Nothing to do when changing the selection, just update the UI if needed
			string selectedSource = patchesSourceComboBox.SelectedItem?.ToString();

			if (string.IsNullOrEmpty(selectedSource))
				return;

			// Clear any cached patch file for this source to ensure we get a fresh copy next time
			if (_cachedPatchFiles.ContainsKey(selectedSource))
			{
				_cachedPatchFiles.Remove(selectedSource);
			}
		}

		// Modify the FetchPatchFileAsync method to extract just the patches
		private async Task<string> FetchPatchFileAsync(string owner, string repo, string filePath)
		{
			try
			{
				// Using GitHub's raw content URL
				string url = $"https://raw.githubusercontent.com/{owner}/{repo}/master/{filePath}";

				using (HttpClient client = new HttpClient())
				{
					// Set user agent (GitHub API requires this)
					client.DefaultRequestHeaders.Add("User-Agent", "RTXRemixLauncher");

					// Download the file
					string content = await client.GetStringAsync(url);

					// Extract just the patches32 and patches64 dictionaries
					return PatchParser.ExtractPatchDictionaries(content);
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to fetch patch file: {ex.Message}");
			}
		}


		private Dictionary<string, string> _cachedPatchFiles = new Dictionary<string, string>();
		private async void ApplyPatchesButton_ClickAsync(object sender, EventArgs e)
		{
			// Disable the button to prevent multiple operations
			ApplyPatchesButton.Enabled = false;

			// Create and show the progress form
			var progressForm = new ProgressForm();
			progressForm.Show();
			progressForm.UpdateProgress("Preparing patching process...", 0);

			try
			{
				// Get the installation directory
				string installDir = GarrysModInstallSystem.GetThisInstallFolder();
				if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
				{
					throw new Exception("Invalid installation directory.");
				}

				// Get selected patch source
				string selectedSource = patchesSourceComboBox.SelectedItem?.ToString();
				if (string.IsNullOrEmpty(selectedSource))
				{
					throw new Exception("No patch source selected.");
				}

				// Confirm with user before applying patches directly
				DialogResult result = MessageBox.Show(
					"This will back up your original game files and replace them with patched versions.\n\n" +
					"Are you sure you want to continue?",
					"Apply Patches",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning);

				if (result != DialogResult.Yes)
				{
					progressForm.Close();
					ApplyPatchesButton.Enabled = true;
					return;
				}

				// Get the patch file content
				string patchFileContent;

				if (_cachedPatchFiles.ContainsKey(selectedSource))
				{
					// Use cached file content
					progressForm.UpdateProgress("Using cached patch file...", 5);
					patchFileContent = _cachedPatchFiles[selectedSource];
				}
				else
				{
					// Fetch from GitHub
					var (owner, repo, filePath) = _patchSources[selectedSource];
					progressForm.UpdateProgress($"Fetching patches from {selectedSource}...", 5);

					patchFileContent = await FetchPatchFileAsync(owner, repo, filePath);

					// Cache the content
					_cachedPatchFiles[selectedSource] = patchFileContent;
					progressForm.UpdateProgress("Patch file fetched successfully.", 10);
				}

				// Apply patches
				await PatchingSystem.ApplyPatches(installDir, patchFileContent, progressForm.UpdateProgress);

				// Update progress form with completion message
				progressForm.UpdateProgress("Patching completed successfully. You can now close this window.", 100);

				// Show a message box to indicate completion
				MessageBox.Show(
					"Patching completed. Original files have been backed up and replaced with patched versions.",
					"Patching Complete",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				// Update progress form with error message
				progressForm.UpdateProgress($"Error: {ex.Message}", 100);

				MessageBox.Show($"Error applying patches: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Re-enable the button when done
				ApplyPatchesButton.Enabled = true;
			}
		}
	}
}