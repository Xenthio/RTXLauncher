using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace RTXLauncher
{
	public partial class Form1
	{
		// Current application version - stored as a string for simple comparison
		private string _currentVersion;

		// Path for the updater helper
		private string _updateTempPath;

		// Latest release found from GitHub
		private GitHubRelease _latestRelease;
		private bool _updateAvailable = false;

		private class UpdateSource
		{
			public string Name { get; set; }
			public string Version { get; set; }
			public string DownloadUrl { get; set; }
			public bool IsStaging { get; set; }
			public GitHubRelease Release { get; set; }

			public override string ToString()
			{
				return IsStaging ? $"{Name}" : $"{Name} ({Version})";
			}
		}

		public void InitialiseUpdater()
		{
			// Get current version from assembly
			var infoAttr = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
			{
				_currentVersion = $"dev-{infoAttr.InformationalVersion}";
			}
			else
			{
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				Version version = assemblyName.Version ?? new Version(0, 0);
				_currentVersion = $"v{version.Major}.{version.Minor}.{version.Build}";
			}

			// Set up initial states
			InstallLauncherUpdateButton.Enabled = false;
			ReleaseNotesRichTextBox.ReadOnly = true;

			// Set up event handlers
			CheckForLauncherUpdatesButton.Click += CheckForLauncherUpdatesButton_Click;
			InstallLauncherUpdateButton.Click += InstallLauncherUpdateButton_Click;
			LauncherUpdateSourceComboBox.SelectedIndexChanged += LauncherUpdateSourceComboBox_SelectedIndexChanged;

			// Create temp folder for updates if it doesn't exist
			_updateTempPath = Path.Combine(Path.GetTempPath(), "RTXLauncherUpdater");
			if (!Directory.Exists(_updateTempPath))
			{
				Directory.CreateDirectory(_updateTempPath);
			}

			// Optionally, check for updates when the form loads
			CheckForUpdatesAsync(false);
		}

		// Add these methods to your Form1 class:

		private async Task PopulateUpdateSourcesComboBox(bool userInitiated = false)
		{
			LauncherUpdateSourceComboBox.Items.Clear();
			LauncherUpdateSourceComboBox.Enabled = false;

			try
			{
				GitHubArtifactInfo devDownload;
				string devDownloadUrl;
				string artifactCommitHash = "latest";

				try
				{
					devDownload = await GitHubAPI.FetchLatestStagingArtifact("Xenthio", "RTXLauncher", userInitiated);
					devDownloadUrl = devDownload.ArchiveDownloadUrl;
					Debug.WriteLine(devDownload.Name);
					artifactCommitHash = devDownload.Name.Remove(0, "RTXLauncher-".Length);
				}
				catch
				{
					devDownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe";
				}

				LauncherUpdateSourceComboBox.Items.Add(new UpdateSource
				{
					// We keep the displayed name for clarity, but store the parsed dev-hash for version comparisons
					Name = "Development Build (Staging)",
					Version = $"dev-{artifactCommitHash}",
					DownloadUrl = devDownloadUrl,
					IsStaging = true
				});

				// Fetch releases from GitHub
				var releases = await GitHubAPI.FetchReleasesAsync("Xenthio", "RTXLauncher", userInitiated);

				// Filter out pre-releases if needed
				var stableReleases = releases.Where(r => !r.Prerelease).ToList();

				// Prefer non-prereleases, but use prereleases if no stable releases are available
				var releasesToUse = stableReleases.Count > 0 ? stableReleases : releases;

				if (releasesToUse.Count > 0)
				{
					// Sort releases by publish date (newest first)
					releasesToUse = releasesToUse.OrderByDescending(r => r.PublishedAt).ToList();

					// Add each release to the combo box
					foreach (var release in releasesToUse)
					{
						// Find appropriate asset
						var exeAsset = release.Assets.FirstOrDefault(a =>
							a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
							a.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));

						if (exeAsset != null)
						{
							LauncherUpdateSourceComboBox.Items.Add(new UpdateSource
							{
								Name = $"Version {release.TagName}",
								Version = release.TagName,
								DownloadUrl = exeAsset.BrowserDownloadUrl,
								IsStaging = false,
								Release = release
							});
						}
						else
						{
							// If no exe found but there is a zip or zipball
							var zipAsset = release.Assets.FirstOrDefault(a =>
								a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
								a.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));

							string downloadUrl = zipAsset?.BrowserDownloadUrl ?? release.ZipballUrl;

							LauncherUpdateSourceComboBox.Items.Add(new UpdateSource
							{
								Name = $"Version {release.TagName}",
								Version = release.TagName,
								DownloadUrl = downloadUrl,
								IsStaging = false,
								Release = release
							});
						}
					}

					// Select the latest release by default (second item, after staging)
					if (LauncherUpdateSourceComboBox.Items.Count > 1)
					{
						LauncherUpdateSourceComboBox.SelectedIndex = 1; // Index 1 is the first release (after staging)
					}
					else
					{
						LauncherUpdateSourceComboBox.SelectedIndex = 0; // Staging if nothing else
					}
				}
				else
				{
					// If no releases found, at least select the staging option
					LauncherUpdateSourceComboBox.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				if (userInitiated)
				{
					MessageBox.Show($"Error loading update sources: {ex.Message}",
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				// Add only staging source as fallback
				if (LauncherUpdateSourceComboBox.Items.Count == 0)
				{
					LauncherUpdateSourceComboBox.Items.Add(new UpdateSource
					{
						Name = "Development Build (Staging)",
						Version = "Latest",
						DownloadUrl = "staging-url-placeholder", // Latest passing action from the repo, or if artifacts dont exist for any passing actions, use https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe
						IsStaging = true
					});
					LauncherUpdateSourceComboBox.SelectedIndex = 0;
				}
			}
			finally
			{
				LauncherUpdateSourceComboBox.Enabled = true;
			}
		}

		private void LauncherUpdateSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (LauncherUpdateSourceComboBox.SelectedItem is UpdateSource selectedSource)
			{
				try
				{
					ReleaseNotesRichTextBox.Clear();

					if (selectedSource.IsStaging)
					{
						// For staging builds, show the dev-hash version directly
						var formatter = new MarkdownFormatter();
						formatter.FormatReleaseNotes(
							ReleaseNotesRichTextBox,
							selectedSource.Version,               // dev-<hash>
							_currentVersion,
							"This is the latest development build from the master branch.\n\n" +
							"Warning: This version may contain experimental features and bugs.",
							true
						);
					}
					else
					{
						// For regular releases, show the release notes
						string latestVersion = selectedSource.Version.TrimStart('v');
						string currentVersion = _currentVersion.TrimStart('v');

						// Check if this release is newer than current version
						bool isNewer = CompareVersions(latestVersion, currentVersion) > 0;

						// Update the _latestRelease field to use in installation
						_latestRelease = selectedSource.Release;
						_updateAvailable = isNewer;

						var formatter = new MarkdownFormatter();
						formatter.FormatReleaseNotes(ReleaseNotesRichTextBox, selectedSource.Version, _currentVersion,
							selectedSource.Release?.Body, isNewer);
					}

					// Always enable install button when a source is selected
					InstallLauncherUpdateButton.Enabled = true;
				}
				catch (Exception ex)
				{
					ReleaseNotesRichTextBox.Text = $"Error loading release information: {ex.Message}";
					InstallLauncherUpdateButton.Enabled = true; // Still allow installation
				}
			}
			else
			{
				ReleaseNotesRichTextBox.Clear();
				InstallLauncherUpdateButton.Enabled = false;
			}
		}

		private async void CheckForLauncherUpdatesButton_Click(object sender, EventArgs e)
		{
			await CheckForUpdatesAsync(true);
		}
		private async Task CheckForUpdatesAsync(bool userInitiated)
		{
			try
			{
				// Update UI
				CheckForLauncherUpdatesButton.Enabled = false;
				InstallLauncherUpdateButton.Enabled = false;
				ReleaseNotesRichTextBox.Clear();

				if (userInitiated)
				{
					ReleaseNotesRichTextBox.Text = "Checking for updates...";
				}

				// Populate the combo box with available versions
				await PopulateUpdateSourcesComboBox(userInitiated);

				// The combo box selection change will handle displaying release notes
				// and setting _updateAvailable

				if (userInitiated && _updateAvailable)
				{
					MessageBox.Show($"Update available! New version: {_latestRelease.TagName}",
						"Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else if (userInitiated && !_updateAvailable && LauncherUpdateSourceComboBox.SelectedIndex > 0)
				{
					MessageBox.Show("You have the latest version of the launcher.",
						"No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				ReleaseNotesRichTextBox.Text = $"Error checking for updates: {ex.Message}";
				if (userInitiated)
				{
					MessageBox.Show($"Error checking for updates: {ex.Message}",
						"Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				CheckForLauncherUpdatesButton.Enabled = true;
			}
		}

		private int CompareVersions(string version1, string version2)
		{
			version1 = version1.TrimStart('v');
			version2 = version2.TrimStart('v');

			// Try to parse as Version objects
			if (Version.TryParse(version1, out Version v1) && Version.TryParse(version2, out Version v2))
			{
				return v1.CompareTo(v2);
			}

			// Check for dev-<hash> patterns
			bool isDev1 = version1.StartsWith("dev-", StringComparison.OrdinalIgnoreCase);
			bool isDev2 = version2.StartsWith("dev-", StringComparison.OrdinalIgnoreCase);

			if (isDev1 && !isDev2) return 1;
			if (!isDev1 && isDev2) return -1;

			if (isDev1 && isDev2)
			{
				string hash1 = version1.Replace("dev-", "", StringComparison.OrdinalIgnoreCase);
				string hash2 = version2.Replace("dev-", "", StringComparison.OrdinalIgnoreCase);

				if (hash1.Equals("latest", StringComparison.OrdinalIgnoreCase)) return 1;
				if (hash2.Equals("latest", StringComparison.OrdinalIgnoreCase)) return -1;

				return string.Compare(hash1, hash2, StringComparison.OrdinalIgnoreCase);
			}

			return string.Compare(version1, version2, StringComparison.Ordinal);
		}

		private async void InstallLauncherUpdateButton_Click(object sender, EventArgs e)
		{
			if (LauncherUpdateSourceComboBox.SelectedItem is not UpdateSource selectedSource)
			{
				MessageBox.Show("No update source selected.",
					"Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Confirm the update
			string versionDisplay = selectedSource.IsStaging ? "Development Build" : selectedSource.Version;
			var result = MessageBox.Show(
				$"Do you want to install {versionDisplay}?\n\n" +
				(selectedSource.IsStaging ? "Warning: This is a development build and may contain bugs.\n\n" : "") +
				"The launcher will restart after the update is installed.",
				"Update Confirmation",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			if (result != DialogResult.Yes)
				return;

			try
			{
				// Disable UI during update
				CheckForLauncherUpdatesButton.Enabled = false;
				InstallLauncherUpdateButton.Enabled = false;
				ReleaseNotesRichTextBox.Text = "Downloading update...";
				Application.DoEvents();

				// Create progress form to show download progress
				using (var progressForm = new ProgressForm())
				{
					progressForm.Text = "Installing Update";
					progressForm.Show(this);
					progressForm.UpdateProgress("Preparing update...", 0);

					// Determine download information
					string assetToDownload = selectedSource.DownloadUrl;

					bool isExe = assetToDownload.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

					// Prepare download paths
					string versionTag = selectedSource.IsStaging ? "dev_build" : selectedSource.Version.Replace(".", "_");
					string downloadFolder = Path.Combine(_updateTempPath, $"RTXLauncher_Update_{versionTag}");
					string extractPath = downloadFolder;

					// File extension depends on what we're downloading
					string downloadPath;

					if (isExe)
					{
						string fileName = Path.GetFileName(assetToDownload);
						if (string.IsNullOrEmpty(fileName))
							fileName = "RTXLauncher.exe";

						downloadPath = Path.Combine(_updateTempPath, fileName);
					}
					else
					{
						downloadPath = Path.Combine(_updateTempPath, $"RTXLauncher_Update_{versionTag}.zip");
					}

					// Clean up any existing files
					try
					{
						if (File.Exists(downloadPath))
							File.Delete(downloadPath);

						if (Directory.Exists(extractPath))
							Directory.Delete(extractPath, true);

						Directory.CreateDirectory(extractPath);
						progressForm.UpdateProgress("Cleaned up previous update files", 10);
					}
					catch (Exception ex)
					{
						progressForm.UpdateProgress($"Warning: Cleanup failed: {ex.Message}", 10);
						// Continue anyway - we'll try to overwrite existing files
					}

					// Calculate total size for progress calculation
					long totalSize = selectedSource.IsStaging ? 1000000 :
									 selectedSource.Release?.Assets.FirstOrDefault()?.Size ?? 1000000;

					// Download the update file with progress reporting
					progressForm.UpdateProgress($"Downloading from: {assetToDownload}", 15);

					using (HttpClient client = new HttpClient())
					{
						client.DefaultRequestHeaders.Add("User-Agent", "RTXLauncherUpdater");
						client.Timeout = TimeSpan.FromMinutes(5); // Set a longer timeout

						using (var response = await client.GetAsync(assetToDownload, HttpCompletionOption.ResponseHeadersRead))
						{
							response.EnsureSuccessStatusCode();

							// Get content length if available
							if (response.Content.Headers.ContentLength.HasValue)
								totalSize = response.Content.Headers.ContentLength.Value;

							progressForm.UpdateProgress($"Downloading {totalSize / 1024} KB...", 20);

							using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
							using (var downloadStream = await response.Content.ReadAsStreamAsync())
							{
								byte[] buffer = new byte[8192];
								long totalBytesRead = 0;
								int bytesRead;

								while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
								{
									await fileStream.WriteAsync(buffer, 0, bytesRead);

									totalBytesRead += bytesRead;
									int progressPercentage = (int)((totalBytesRead * 100) / totalSize);
									int overallProgress = 20 + (int)(progressPercentage * 0.3); // 20% to 50% overall progress

									progressForm.UpdateProgress($"Downloading... {progressPercentage}%", overallProgress);
								}
							}
						}
					}

					// Handle the downloaded file
					string sourceDir;

					if (isExe)
					{
						progressForm.UpdateProgress("Downloaded executable directly, no extraction needed", 55);
						// For direct exe downloads, just copy it to the extraction folder
						sourceDir = extractPath;
						File.Copy(downloadPath, Path.Combine(extractPath, Path.GetFileName(downloadPath)), true);
					}
					else
					{
						// For zip files, extract them
						progressForm.UpdateProgress("Extracting update...", 55);

						try
						{
							ZipFile.ExtractToDirectory(downloadPath, extractPath);
							progressForm.UpdateProgress("Extraction completed", 65);
						}
						catch (Exception ex)
						{
							progressForm.UpdateProgress($"Extraction error: {ex.Message}", 55);
							throw new Exception($"Failed to extract update: {ex.Message}", ex);
						}

						// Find the extracted directory (GitHub zipballs have a folder inside with the repo name)
						sourceDir = extractPath;
						var directories = Directory.GetDirectories(extractPath);
						if (directories.Length > 0)
						{
							sourceDir = directories[0];
							progressForm.UpdateProgress($"Found extracted directory: {Path.GetFileName(sourceDir)}", 70);
						}
					}

					// Create the updater batch script
					progressForm.UpdateProgress("Preparing update installer...", 80);

					// Use AppContext.BaseDirectory for more reliable path resolution
					string currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
					if (string.IsNullOrEmpty(currentExePath))
					{
						// Fallback if MainModule.FileName fails
						currentExePath = Path.Combine(AppContext.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
					}

					string currentDirectory = AppContext.BaseDirectory;

					// Additional safety checks
					if (string.IsNullOrEmpty(currentDirectory))
					{
						currentDirectory = Path.GetDirectoryName(currentExePath);
						if (string.IsNullOrEmpty(currentDirectory))
						{
							throw new Exception("Could not determine application directory");
						}
					}

					// Validate paths before proceeding
					if (string.IsNullOrEmpty(sourceDir))
					{
						throw new Exception("Source directory is empty or invalid");
					}

					if (string.IsNullOrEmpty(currentExePath))
					{
						throw new Exception("Could not determine current executable path");
					}

					if (string.IsNullOrEmpty(currentDirectory))
					{
						throw new Exception("Could not determine current directory");
					}

					// Log debug information
					Debug.WriteLine($"Current executable: {currentExePath}");
					Debug.WriteLine($"Current directory: {currentDirectory}");
					Debug.WriteLine($"Source directory: {sourceDir}");
					Debug.WriteLine($"Update temp path: {_updateTempPath}");

					// Create updater batch file
					string updaterBatchPath = Path.Combine(_updateTempPath, "RTXLauncherUpdater.bat");
					await CreateUpdaterBatchFile(updaterBatchPath, sourceDir, currentDirectory, currentExePath);

					progressForm.UpdateProgress("Launching updater...", 95);

					// Give a moment for the progress to display
					await Task.Delay(500);

					// Launch the updater
					LaunchUpdaterAndExit(updaterBatchPath);
				}
			}
			catch (Exception ex)
			{
				string message = $"Error installing update: {ex.Message}";
				if (ex.InnerException != null)
				{
					message += $"\n\nDetails: {ex.InnerException.Message}";
				}

				MessageBox.Show(message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				ReleaseNotesRichTextBox.Text = $"Update installation failed: {ex.Message}\r\n\r\n" +
					"Please try again later or download the latest version manually from GitHub.";

				CheckForLauncherUpdatesButton.Enabled = true;
				InstallLauncherUpdateButton.Enabled = true;
			}
		}


		private void LaunchUpdaterAndExit(string updaterBatchPath)
		{
			try
			{
				// Create process info
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/C \"{updaterBatchPath}\"",
					UseShellExecute = true,
					CreateNoWindow = false,
					WorkingDirectory = Path.GetDirectoryName(updaterBatchPath) ?? string.Empty
				};

				// Start the process
				Process.Start(psi);

				// Force immediate exit of the application
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to start updater: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async Task CreateUpdaterBatchFile(string batchFilePath, string sourceDir, string targetDir, string exePath)
		{
			// Validate all path inputs to prevent null reference exceptions
			if (string.IsNullOrEmpty(batchFilePath))
				throw new ArgumentNullException(nameof(batchFilePath), "Batch file path cannot be null or empty");

			if (string.IsNullOrEmpty(sourceDir))
				throw new ArgumentNullException(nameof(sourceDir), "Source directory path cannot be null or empty");

			if (string.IsNullOrEmpty(targetDir))
				throw new ArgumentNullException(nameof(targetDir), "Target directory path cannot be null or empty");

			if (string.IsNullOrEmpty(exePath))
				throw new ArgumentNullException(nameof(exePath), "Executable path cannot be null or empty");

			// First ensure any existing batch file is deleted
			try
			{
				if (File.Exists(batchFilePath))
				{
					File.Delete(batchFilePath);
				}
			}
			catch
			{
				// Generate a unique filename instead if delete fails
				string batchDir = Path.GetDirectoryName(batchFilePath);
				if (string.IsNullOrEmpty(batchDir))
					batchDir = Path.GetTempPath(); // Fallback to temp directory

				batchFilePath = Path.Combine(batchDir, $"RTXLauncherUpdater_{Guid.NewGuid():N}.bat");
			}

			// Get file name of the executable
			string exeName = Path.GetFileName(exePath);
			if (string.IsNullOrEmpty(exeName))
				exeName = "RTXLauncher.exe"; // Default fallback name

			// Create full path to target executable
			string fullTargetExePath = Path.Combine(targetDir, exeName);

			// Check if the executable exists in source directory
			string sourceExePath = Path.Combine(sourceDir, exeName);
			bool exeExistsInSource = File.Exists(sourceExePath);

			// Log debugging information
			Debug.WriteLine($"Batch file path: {batchFilePath}");
			Debug.WriteLine($"Source dir: {sourceDir}");
			Debug.WriteLine($"Target dir: {targetDir}");
			Debug.WriteLine($"Exe path: {exePath}");
			Debug.WriteLine($"Exe name: {exeName}");
			Debug.WriteLine($"Full target exe: {fullTargetExePath}");
			Debug.WriteLine($"Source exe exists: {exeExistsInSource}");

			// Properly escape paths with quotes to handle spaces
			string batchScript = @$"
@echo off
setlocal enabledelayedexpansion

echo RTX Launcher Updater
echo ==================

:: Print debug info
echo Source Directory: ""{sourceDir}""
echo Target Directory: ""{targetDir}""
echo Executable: ""{exeName}""

echo Waiting for application to close...

:: Wait a moment for process to exit completely
timeout /t 3 /nobreak > nul

echo Preparing update...

:: Create target directory if it doesn't exist
if not exist ""{targetDir}"" mkdir ""{targetDir}""

:: Check if source executable exists
if not exist ""{sourceExePath}"" (
    echo WARNING: Source executable not found!
    dir ""{sourceDir}""
)

:: Use direct copy for the executable
echo Copying executable...
copy ""{sourceExePath}"" ""{fullTargetExePath}"" /Y
if !ERRORLEVEL! neq 0 (
    echo Failed to copy executable: !ERRORLEVEL!
)

:: Then copy all other files
echo Copying remaining files...
xcopy ""{sourceDir}\*.*"" ""{targetDir}\"" /E /Y /I /Q
if !ERRORLEVEL! neq 0 (
    echo Warning: Some files may not have copied correctly
)

:: Check if the executable exists in the target directory
if exist ""{fullTargetExePath}"" (
    echo Executable successfully copied
) else (
    echo ERROR: Executable not found in target directory!
    dir ""{targetDir}""
)

:: Start the application
echo Starting updated application...
cd /d ""{targetDir}""
start """" ""{fullTargetExePath}""

:: Clean up temporary files
echo Cleaning up...
timeout /t 2 /nobreak > nul
rmdir /S /Q ""{Path.GetDirectoryName(sourceDir)}""

:: Self-delete with a short delay
timeout /t 2 /nobreak > nul
";

#if (DEBUG)
			// In debug mode, pause at the end
			batchScript += "echo Update complete.\npause\n";
#else
    // In release mode, self-delete
    batchScript += @"
:: Self-delete this batch file
(goto) 2>nul & del ""%~f0""
";
#endif

			try
			{
				// Create directory if it doesn't exist
				string batchDir = Path.GetDirectoryName(batchFilePath);
				if (!string.IsNullOrEmpty(batchDir) && !Directory.Exists(batchDir))
				{
					Directory.CreateDirectory(batchDir);
				}

				// Write the batch file with ASCII encoding
				File.WriteAllText(batchFilePath, batchScript, System.Text.Encoding.ASCII);

				// Verify the file was created successfully
				if (!File.Exists(batchFilePath))
				{
					throw new Exception("Failed to create updater batch file!");
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error creating batch file: {ex.Message}", ex);
			}

			await Task.CompletedTask;
		}

		// Make sure to properly handle application exit
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			// If we're in the middle of updating, don't interrupt
			if (CheckForLauncherUpdatesButton.Enabled == false &&
				InstallLauncherUpdateButton.Enabled == false)
			{
				if (e.CloseReason == CloseReason.UserClosing)
				{
					var result = MessageBox.Show("An update is in progress. Are you sure you want to exit?",
						"Update in Progress", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						e.Cancel = true;
					}
				}
			}
		}
	}
}