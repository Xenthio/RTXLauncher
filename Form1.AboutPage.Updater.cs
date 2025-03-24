using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RTXLauncher
{
	public partial class Form1
	{// Current application version - stored as a string for simple comparison
		private string _currentVersion;

		// Path for the updater helper
		private string _updateTempPath;

		// Latest release found from GitHub
		private GitHubRelease _latestRelease;
		private bool _updateAvailable = false;

		// Constructor - make sure this integrates with your existing form
		public void InitialiseUpdater()
		{
			// Get current version from assembly
			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			Version version = assemblyName.Version ?? new Version(0, 0);
			_currentVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

			// Set up initial states
			InstallLauncherUpdateButton.Enabled = false;
			ReleaseNotesRichTextBox.ReadOnly = true;

			// Set up event handlers
			CheckForLauncherUpdatesButton.Click += CheckForLauncherUpdatesButton_Click;
			InstallLauncherUpdateButton.Click += InstallLauncherUpdateButton_Click;

			// Create temp folder for updates if it doesn't exist
			_updateTempPath = Path.Combine(Path.GetTempPath(), "RTXLauncherUpdater");
			if (!Directory.Exists(_updateTempPath))
			{
				Directory.CreateDirectory(_updateTempPath);
			}

			// Optionally, check for updates when the form loads
			CheckForUpdatesAsync(false);
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

				// Fetch releases from GitHub
				var releases = await GitHubAPI.FetchReleasesAsync("Xenthio", "RTXLauncher", userInitiated);

				// Filter out pre-releases if needed
				var stableReleases = releases.Where(r => !r.Prerelease).ToList();

				// Prefer non-prereleases, but use prereleases if no stable releases are available
				var releasesToUse = stableReleases.Count > 0 ? stableReleases : releases;

				if (releasesToUse.Count == 0)
				{
					if (userInitiated)
					{
						ReleaseNotesRichTextBox.Text = "No releases found on GitHub.";
						MessageBox.Show("No releases found on GitHub.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					return;
				}

				// Get the latest release
				_latestRelease = releasesToUse.OrderByDescending(r => r.PublishedAt).First();

				// Check if update is available by comparing versions
				string latestVersion = _latestRelease.TagName.TrimStart('v');
				string currentVersion = _currentVersion.TrimStart('v');

				// Compare versions (simple string comparison might not work for all versioning schemes)
				_updateAvailable = CompareVersions(latestVersion, currentVersion) > 0;

				// Update UI based on update availability
				if (_updateAvailable)
				{
					ReleaseNotesRichTextBox.Clear();
					ReleaseNotesRichTextBox.AppendText($"New version available: {_latestRelease.TagName}\r\n");
					ReleaseNotesRichTextBox.AppendText($"Current version: {_currentVersion}\r\n\r\n");
					ReleaseNotesRichTextBox.AppendText("Release Notes:\r\n");
					ReleaseNotesRichTextBox.AppendText(_latestRelease.Body ?? "No release notes available");

					FormatReleaseNotes(_latestRelease.TagName, _currentVersion, _latestRelease.Body);

					InstallLauncherUpdateButton.Enabled = true;

					if (userInitiated)
					{
						MessageBox.Show($"Update available! New version: {_latestRelease.TagName}",
							"Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				else
				{
					ReleaseNotesRichTextBox.Text = $"You have the latest version ({_currentVersion}).\r\n\r\n" +
						"Latest release notes:\r\n" + (_latestRelease.Body ?? "No release notes available");

					FormatReleaseNotes(_latestRelease.TagName, _currentVersion, _latestRelease.Body, false);

					if (userInitiated)
					{
						MessageBox.Show("You have the latest version of the launcher.",
							"No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
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

		// Store link information for click detection
		private List<LinkInfo> _links = new List<LinkInfo>();

		/// <summary>
		/// Formats GitHub markdown release notes in the RichTextBox with syntax highlighting and proper formatting
		/// </summary>
		private void FormatReleaseNotes(string newVersion, string currentVersion, string releaseNotes, bool isUpdate = true)
		{
			// Clear previous links
			_links.Clear();

			ReleaseNotesRichTextBox.Clear();
			ReleaseNotesRichTextBox.SuspendLayout();

			// Default font
			ReleaseNotesRichTextBox.Font = new Font("Segoe UI", 9.0f);

			// Add header
			if (isUpdate)
			{
				// Update available styling
				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);
				ReleaseNotesRichTextBox.SelectionColor = Color.Green;
				ReleaseNotesRichTextBox.AppendText("Update Available!\n");

				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Regular);
				ReleaseNotesRichTextBox.SelectionColor = Color.Black;
				ReleaseNotesRichTextBox.AppendText($"New version: ");

				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Bold);
				ReleaseNotesRichTextBox.AppendText($"{newVersion}\n");

				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Regular);
				ReleaseNotesRichTextBox.AppendText($"Current version: {currentVersion}\n\n");
			}
			else
			{
				// No update styling
				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);
				ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
				ReleaseNotesRichTextBox.AppendText("You're up to date!\n");

				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f);
				ReleaseNotesRichTextBox.SelectionColor = Color.Black;
				ReleaseNotesRichTextBox.AppendText($"Current version: {currentVersion}\n");

				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f);
				ReleaseNotesRichTextBox.SelectionColor = Color.Black;
				ReleaseNotesRichTextBox.AppendText($"Latest release: {newVersion}\n\n");
			}

			// Add "Release Notes" header
			ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 11f, FontStyle.Bold);
			ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
			ReleaseNotesRichTextBox.AppendText("Release Notes:\n");

			// Handle missing release notes
			if (string.IsNullOrWhiteSpace(releaseNotes))
			{
				ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f, FontStyle.Italic);
				ReleaseNotesRichTextBox.SelectionColor = Color.Gray;
				ReleaseNotesRichTextBox.AppendText("No release notes available for this version.");
				ReleaseNotesRichTextBox.ResumeLayout();
				return;
			}

			// Process each line but with fixed link handling
			string[] lines = releaseNotes.Replace("\r\n", "\n").Split('\n');
			bool inCodeBlock = false;
			bool inBulletList = false;

			foreach (string line in lines)
			{
				string trimmedLine = line.TrimStart();

				// Handle code blocks
				if (trimmedLine.StartsWith("```"))
				{
					inCodeBlock = !inCodeBlock;

					// Add a gray background to code blocks
					if (inCodeBlock)
					{
						ReleaseNotesRichTextBox.SelectionBackColor = Color.FromArgb(245, 245, 245);
						ReleaseNotesRichTextBox.SelectionFont = new Font("Consolas", 9f);
					}
					else
					{
						ReleaseNotesRichTextBox.SelectionBackColor = Color.White;
						ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f);
						ReleaseNotesRichTextBox.AppendText("\n");
					}
					continue;
				}

				// Inside a code block
				if (inCodeBlock)
				{
					ReleaseNotesRichTextBox.SelectionFont = new Font("Consolas", 9f);
					ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
					ReleaseNotesRichTextBox.AppendText(line + "\n");
					continue;
				}

				// Handle headings (# Heading)
				if (trimmedLine.StartsWith("# "))
				{
					ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);
					ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
					ReleaseNotesRichTextBox.AppendText(trimmedLine.Substring(2) + "\n");
				}
				else if (trimmedLine.StartsWith("## "))
				{
					ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 11f, FontStyle.Bold);
					ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
					ReleaseNotesRichTextBox.AppendText(trimmedLine.Substring(3) + "\n");
				}
				else if (trimmedLine.StartsWith("### "))
				{
					ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Bold);
					ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
					ReleaseNotesRichTextBox.AppendText(trimmedLine.Substring(4) + "\n");
				}
				// Handle bullet points
				else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
				{
					inBulletList = true;
					ReleaseNotesRichTextBox.SelectionBullet = true;
					ReleaseNotesRichTextBox.BulletIndent = 15;
					ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f);
					ReleaseNotesRichTextBox.SelectionColor = Color.Black;

					// Process links in bullet points
					ProcessMarkdownText(trimmedLine.Substring(2));
					ReleaseNotesRichTextBox.AppendText("\n");
					ReleaseNotesRichTextBox.SelectionBullet = false;
				}
				// Handle blank lines
				else if (string.IsNullOrWhiteSpace(line))
				{
					inBulletList = false;
					ReleaseNotesRichTextBox.SelectionBullet = false;
					ReleaseNotesRichTextBox.AppendText("\n");
				}
				// Regular text
				else
				{
					// Reset bullet formatting if we were in a bullet list
					if (inBulletList)
					{
						inBulletList = false;
						ReleaseNotesRichTextBox.SelectionBullet = false;
					}

					// Set the base font and color for the line
					ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f);
					ReleaseNotesRichTextBox.SelectionColor = Color.Black;

					// Special handling for common patterns in release notes styling
					if (trimmedLine.StartsWith("Fixed:") || trimmedLine.StartsWith("Fixes:") ||
						trimmedLine.StartsWith("Bug fix:") || trimmedLine.Contains("FIXED"))
					{
						ReleaseNotesRichTextBox.SelectionColor = Color.Green;
					}
					else if (trimmedLine.StartsWith("Added:") || trimmedLine.StartsWith("New:") ||
							 trimmedLine.StartsWith("Feature:") || trimmedLine.Contains("NEW"))
					{
						ReleaseNotesRichTextBox.SelectionColor = Color.DarkBlue;
					}
					else if (trimmedLine.StartsWith("Changed:") || trimmedLine.StartsWith("Updated:") ||
							 trimmedLine.StartsWith("Improved:") || trimmedLine.Contains("CHANGED"))
					{
						ReleaseNotesRichTextBox.SelectionColor = Color.DarkOrange;
					}
					else if (trimmedLine.StartsWith("Warning:") || trimmedLine.StartsWith("Important:") ||
							 trimmedLine.StartsWith("Note:") || trimmedLine.Contains("WARNING"))
					{
						ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f, FontStyle.Bold);
						ReleaseNotesRichTextBox.SelectionColor = Color.Red;
					}

					ProcessMarkdownText(line);

					ReleaseNotesRichTextBox.AppendText("\n"); // Add newline after processing
				}
			}

			// Attach the click event
			ReleaseNotesRichTextBox.MouseClick -= ReleaseNotesRichTextBox_MouseClick;
			ReleaseNotesRichTextBox.MouseClick += ReleaseNotesRichTextBox_MouseClick;

			// Restore default selection styling
			ReleaseNotesRichTextBox.SelectionFont = new Font("Segoe UI", 9f);
			ReleaseNotesRichTextBox.SelectionColor = Color.Black;
			ReleaseNotesRichTextBox.SelectionBackColor = Color.White;
			ReleaseNotesRichTextBox.SelectionBullet = false;

			// Move to the beginning of the text
			ReleaseNotesRichTextBox.SelectionStart = 0;
			ReleaseNotesRichTextBox.ScrollToCaret();

			ReleaseNotesRichTextBox.ResumeLayout();
		}

		/// <summary>
		/// Processes markdown text including links, bold, italic and changelog links
		/// </summary>
		private void ProcessMarkdownText(string line)
		{
			// Check for full changelog line
			if (line.Contains("**Full Changelog**:") && line.Contains("compare/"))
			{
				// Extract the version comparison (e.g., v1.0.4...v1.0.5)
				Regex compareRegex = new Regex(@"compare/([^/\s\)]+)");
				Match compareMatch = compareRegex.Match(line);

				// Extract the URL
				Regex urlRegex = new Regex(@"(https?://[^\s\)]+)");
				Match urlMatch = urlRegex.Match(line);

				if (urlMatch.Success)
				{
					string url = urlMatch.Groups[1].Value;
					string versionCompare = compareMatch.Success ? compareMatch.Groups[1].Value : "versions";

					// Add "Full Changelog" in bold
					ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Bold);
					ReleaseNotesRichTextBox.AppendText("Full Changelog");

					// Add the colon
					ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Regular);
					ReleaseNotesRichTextBox.AppendText(": ");

					// Add the version comparison as a link
					int linkStart = ReleaseNotesRichTextBox.TextLength;
					ReleaseNotesRichTextBox.AppendText(versionCompare);
					int linkEnd = ReleaseNotesRichTextBox.TextLength;

					// Format the link text
					ReleaseNotesRichTextBox.Select(linkStart, linkEnd - linkStart);
					ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Underline);
					ReleaseNotesRichTextBox.SelectionColor = Color.Blue;

					// Store link info
					_links.Add(new LinkInfo
					{
						StartIndex = linkStart,
						EndIndex = linkEnd,
						Url = url
					});

					return;
				}
			}

			// Handle bold text (**bold**) and links ([text](url))
			// We'll need to parse the line character by character to handle nested formatting

			// First, find all the special markdown segments
			List<MarkdownSegment> segments = new List<MarkdownSegment>();

			// Find bold segments
			FindMarkdownPatterns(line, @"\*\*(.*?)\*\*", segments, MarkdownType.Bold);

			// Find italic segments
			FindMarkdownPatterns(line, @"\*(.*?)\*", segments, MarkdownType.Italic);

			// Find link segments
			FindMarkdownPatterns(line, @"\[(.*?)\]\((https?://[^\s\)]+)\)", segments, MarkdownType.Link);

			// Sort segments by their starting position
			segments = segments.OrderBy(s => s.StartIndex).ToList();

			// Detect overlapping segments and remove them
			for (int i = segments.Count - 1; i >= 1; i--)
			{
				for (int j = i - 1; j >= 0; j--)
				{
					if (segments[i].StartIndex < segments[j].EndIndex)
					{
						// Segments overlap, remove the later one
						segments.RemoveAt(i);
						break;
					}
				}
			}

			// Process the line with the segments
			int lastIndex = 0;
			foreach (var segment in segments)
			{
				// Add text before the segment
				if (segment.StartIndex > lastIndex)
				{
					string beforeText = line.Substring(lastIndex, segment.StartIndex - lastIndex);
					ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Regular);
					ReleaseNotesRichTextBox.SelectionColor = Color.Black;
					ReleaseNotesRichTextBox.AppendText(beforeText);
				}

				// Add the segment with appropriate formatting
				switch (segment.Type)
				{
					case MarkdownType.Bold:
						ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Bold);
						ReleaseNotesRichTextBox.SelectionColor = Color.Black;
						ReleaseNotesRichTextBox.AppendText(segment.Text);
						break;

					case MarkdownType.Italic:
						ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Italic);
						ReleaseNotesRichTextBox.SelectionColor = Color.Black;
						ReleaseNotesRichTextBox.AppendText(segment.Text);
						break;

					case MarkdownType.Link:
						int linkStart = ReleaseNotesRichTextBox.TextLength;
						ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Underline);
						ReleaseNotesRichTextBox.SelectionColor = Color.Blue;
						ReleaseNotesRichTextBox.AppendText(segment.Text);
						int linkEnd = ReleaseNotesRichTextBox.TextLength;

						_links.Add(new LinkInfo
						{
							StartIndex = linkStart,
							EndIndex = linkEnd,
							Url = segment.Url
						});
						break;
				}

				lastIndex = segment.EndIndex;
			}

			// Add any remaining text after the last segment
			if (lastIndex < line.Length)
			{
				ReleaseNotesRichTextBox.SelectionFont = new Font(ReleaseNotesRichTextBox.Font, FontStyle.Regular);
				ReleaseNotesRichTextBox.SelectionColor = Color.Black;
				ReleaseNotesRichTextBox.AppendText(line.Substring(lastIndex));
			}
		}

		/// <summary>
		/// Helper method to find markdown patterns in text
		/// </summary>
		private void FindMarkdownPatterns(string text, string pattern, List<MarkdownSegment> segments, MarkdownType type)
		{
			Regex regex = new Regex(pattern);
			foreach (Match match in regex.Matches(text))
			{
				if (type == MarkdownType.Link && match.Groups.Count >= 3)
				{
					segments.Add(new MarkdownSegment
					{
						StartIndex = match.Index,
						EndIndex = match.Index + match.Length,
						Text = match.Groups[1].Value,
						Url = match.Groups[2].Value,
						Type = type
					});
				}
				else if (match.Groups.Count >= 2)
				{
					segments.Add(new MarkdownSegment
					{
						StartIndex = match.Index,
						EndIndex = match.Index + match.Length,
						Text = match.Groups[1].Value,
						Type = type
					});
				}
			}
		}

		/// <summary>
		/// Class to store link information for click detection
		/// </summary>
		private class LinkInfo
		{
			public int StartIndex { get; set; }
			public int EndIndex { get; set; }
			public string Url { get; set; }
		}

		/// <summary>
		/// Types of markdown formatting
		/// </summary>
		private enum MarkdownType
		{
			Bold,
			Italic,
			Link
		}

		/// <summary>
		/// Class to store information about a markdown segment
		/// </summary>
		private class MarkdownSegment
		{
			public int StartIndex { get; set; }
			public int EndIndex { get; set; }
			public string Text { get; set; }
			public string Url { get; set; }
			public MarkdownType Type { get; set; }
		}

		/// <summary>
		/// Handle mouse clicks on links
		/// </summary>
		private void ReleaseNotesRichTextBox_MouseClick(object sender, MouseEventArgs e)
		{
			// Get the character index at the clicked position
			int index = ReleaseNotesRichTextBox.GetCharIndexFromPosition(e.Location);

			// Check if the click is on a link
			foreach (var link in _links)
			{
				if (index >= link.StartIndex && index < link.EndIndex)
				{
					try
					{
						// Open the URL
						ProcessStartInfo psi = new ProcessStartInfo
						{
							FileName = link.Url,
							UseShellExecute = true
						};
						Process.Start(psi);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					break;
				}
			}
		}

		private async void InstallLauncherUpdateButton_Click(object sender, EventArgs e)
		{
			if (_latestRelease == null || !_updateAvailable)
			{
				MessageBox.Show("No updates available to install.",
					"No Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Confirm the update
			var result = MessageBox.Show(
				$"Do you want to update to {_latestRelease.TagName}?\n\n" +
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

					// Determine the asset to download based on available assets
					string assetToDownload = null;
					GitHubAsset selectedAsset = null;

					// Look for .zip files first
					var zipAsset = _latestRelease.Assets.FirstOrDefault(a =>
						a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
						a.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));

					if (zipAsset != null)
					{
						selectedAsset = zipAsset;
						assetToDownload = zipAsset.BrowserDownloadUrl;
					}
					else
					{
						// If no zip found, use the zipball URL
						assetToDownload = _latestRelease.ZipballUrl;
					}

					if (string.IsNullOrEmpty(assetToDownload))
					{
						progressForm.Close();
						MessageBox.Show("No valid download found in the release.",
							"Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}

					// Prepare download paths
					string zipPath = Path.Combine(_updateTempPath, $"RTXLauncher_Update_{_latestRelease.TagName.Replace(".", "_")}.zip");
					string extractPath = Path.Combine(_updateTempPath, $"RTXLauncher_Update_{_latestRelease.TagName.Replace(".", "_")}");

					// Clean up any existing files
					if (File.Exists(zipPath))
						File.Delete(zipPath);
					if (Directory.Exists(extractPath))
						Directory.Delete(extractPath, true);

					Directory.CreateDirectory(extractPath);

					// Calculate total size for progress calculation
					long totalSize = selectedAsset?.Size ?? 0;
					if (totalSize == 0) totalSize = 1000000; // Default size estimate if unknown

					// Download the update file with progress reporting
					using (HttpClient client = new HttpClient())
					{
						client.DefaultRequestHeaders.Add("User-Agent", "RTXLauncherUpdater");

						using (var response = await client.GetAsync(assetToDownload, HttpCompletionOption.ResponseHeadersRead))
						{
							response.EnsureSuccessStatusCode();

							// Get content length if available
							if (response.Content.Headers.ContentLength.HasValue)
								totalSize = response.Content.Headers.ContentLength.Value;

							progressForm.UpdateProgress("Downloading update...", 0);

							using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
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

									progressForm.UpdateProgress($"Downloading update... {progressPercentage}%", progressPercentage);
								}
							}
						}
					}

					// Extract the update
					progressForm.UpdateProgress("Extracting update...", 50);
					ZipFile.ExtractToDirectory(zipPath, extractPath);

					// Find the extracted directory (GitHub zipballs have a folder inside with the repo name)
					string baseExtractPath = extractPath;
					var directories = Directory.GetDirectories(extractPath);
					if (directories.Length > 0)
					{
						baseExtractPath = directories[0];
					}

					// Create the updater batch script
					progressForm.UpdateProgress("Preparing update installer...", 90);
					string currentExePath = Assembly.GetExecutingAssembly().Location;
					string currentDirectory = Path.GetDirectoryName(currentExePath);

					// Create a batch file that waits for the current process to exit,
					// copies the new files, and then restarts the application
					string updaterBatchPath = Path.Combine(_updateTempPath, "RTXLauncherUpdater.bat");
					await CreateUpdaterBatchFile(updaterBatchPath, baseExtractPath, currentDirectory, currentExePath);

					progressForm.UpdateProgress("Installation prepared. Restarting launcher...", 100);

					// Run the batch file and exit the application
					Process.Start(new ProcessStartInfo
					{
						FileName = updaterBatchPath,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					});

					// Exit the application to let the updater do its work
					Application.Exit();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error installing update: {ex.Message}",
					"Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				ReleaseNotesRichTextBox.Text = $"Update installation failed: {ex.Message}\r\n\r\n" +
					"Please try again later or download the latest version manually from GitHub.";

				CheckForLauncherUpdatesButton.Enabled = true;
				InstallLauncherUpdateButton.Enabled = _updateAvailable;
			}
		}

		private async Task CreateUpdaterBatchFile(string batchFilePath, string sourceDir, string targetDir, string exePath)
		{
			// Script will:
			// 1. Wait for the launcher process to exit
			// 2. Copy all new files
			// 3. Restart the launcher

			string exeName = Path.GetFileName(exePath);

			string batchScript =
				"@echo off\r\n" +
				"echo Updating RTX Launcher...\r\n" +
				"\r\n" +
				":: Wait for original process to exit\r\n" +
				$"timeout /t 2 /nobreak > nul\r\n" +
				"\r\n" +
				":: Copy files\r\n" +
				$"xcopy \"{sourceDir}\\*\" \"{targetDir}\\\" /Y /E /I /Q\r\n" +
				"\r\n" +
				":: Launch the application\r\n" +
				$"start \"\" \"{targetDir}\\{exeName}\"\r\n" +
				"\r\n" +
				":: Clean up the temporary files\r\n" +
				$"timeout /t 2 /nobreak > nul\r\n" +
				$"rmdir \"{Path.GetDirectoryName(sourceDir)}\" /S /Q\r\n" +
				"\r\n" +
				":: Delete this batch file\r\n" +
				$"del \"%~f0\"\r\n";

			await File.WriteAllTextAsync(batchFilePath, batchScript);
		}

		private int CompareVersions(string version1, string version2)
		{
			// Clean up version strings
			version1 = version1.TrimStart('v');
			version2 = version2.TrimStart('v');

			// Try to parse as Version objects
			if (Version.TryParse(version1, out Version v1) && Version.TryParse(version2, out Version v2))
			{
				return v1.CompareTo(v2);
			}

			// Fallback to string comparison (not ideal for versioning)
			return string.Compare(version1, version2, StringComparison.Ordinal);
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