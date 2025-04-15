using System.IO.Compression;

namespace RTXLauncher
{
    public partial class Form1
    {
        private Dictionary<string, (string Owner, string Repo)> _optiScalerSources = new Dictionary<string, (string, string)>
        {
            { "sambow23/OptiScaler-Releases", ("sambow23", "OptiScaler-Releases") }
        };

        private async Task PopulateOptiScalerSourceComboBox()
        {
            optiScalerSourceComboBox.Items.Clear();
            foreach (var source in _optiScalerSources.Keys)
            {
                optiScalerSourceComboBox.Items.Add(source);
            }

            // Select the default source
            string defaultSource = "sambow23/OptiScaler-Releases";
            int defaultIndex = optiScalerSourceComboBox.Items.IndexOf(defaultSource);

            if (defaultIndex >= 0)
            {
                optiScalerSourceComboBox.SelectedIndex = defaultIndex;
            }
            else if (optiScalerSourceComboBox.Items.Count > 0)
            {
                // Fallback if the default source is not found
                optiScalerSourceComboBox.SelectedIndex = 0;
            }
        }

        private async void OptiScalerSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (optiScalerSourceComboBox.SelectedItem == null)
                return;

            await PopulateOptiScalerVersionComboBoxAsync();
        }

        private async Task PopulateOptiScalerVersionComboBoxAsync()
        {
            // Get selected source
            string selectedSource = optiScalerSourceComboBox.SelectedItem?.ToString();
            if (selectedSource == null || !_optiScalerSources.TryGetValue(selectedSource, out var sourceInfo))
                return;

            // Show loading indicator
            optiScalerVersionComboBox.Enabled = false;
            optiScalerVersionComboBox.Items.Clear();
            optiScalerVersionComboBox.Items.Add("Loading releases...");
            optiScalerVersionComboBox.SelectedIndex = 0;

            try
            {
                // Fetch releases for the selected repository
                List<GitHubRelease> releases = await GitHubAPI.FetchReleasesAsync(sourceInfo.Owner, sourceInfo.Repo);

                // Clear the loading text
                optiScalerVersionComboBox.Items.Clear();

                if (releases.Count > 0)
                {
                    // Sort releases by published date (newest first)
                    releases = releases.OrderByDescending(r => r.PublishedAt).ToList();

                    // First find releases that have a -launcher.zip file
                    var launcherReleases = releases
                        .Where(r => r.Assets.Any(a => a.Name.Contains("-launcher") && a.Name.EndsWith(".zip")))
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
                            optiScalerVersionComboBox.Items.Add(release);
                        }

                        // Select the latest release
                        optiScalerVersionComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        optiScalerVersionComboBox.Items.Add("No compatible packages found");
                        optiScalerVersionComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    optiScalerVersionComboBox.Items.Add("No releases found");
                    optiScalerVersionComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                optiScalerVersionComboBox.Items.Clear();
                optiScalerVersionComboBox.Items.Add($"Error: {ex.Message}");
                optiScalerVersionComboBox.SelectedIndex = 0;
            }

            optiScalerVersionComboBox.Enabled = true;
        }

        private async void InstallOptiScalerButton_ClickAsync(object sender, EventArgs e)
        {
            // Validate selections
            if (!(optiScalerSourceComboBox.SelectedItem is string selectedSource) ||
                !(optiScalerVersionComboBox.SelectedItem is GitHubRelease selectedRelease))
            {
                MessageBox.Show("Please select a source and version for OptiScaler.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_optiScalerSources.TryGetValue(selectedSource, out var sourceInfo))
            {
                MessageBox.Show("Invalid source selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Find a suitable asset to download (prioritize -launcher.zip)
            GitHubAsset assetToDownload = selectedRelease.Assets.FirstOrDefault(a => a.Name.Contains("-launcher") && a.Name.EndsWith(".zip"));

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

                await InstallOptiScalerPackage(assetToDownload, installDir, progressForm.UpdateProgress);
                MessageBox.Show("OptiScaler package installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing OptiScaler package: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task InstallOptiScalerPackage(GitHubAsset asset, string installDir, Action<string, int> progressCallback)
        {
            progressCallback?.Invoke("Starting OptiScaler installation...", 0);

            // Create a temporary directory for downloading
            string tempDir = Path.Combine(Path.GetTempPath(), "OptiScalerTemp");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

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

                // Create a temporary extraction directory to examine the contents
                string extractTempDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractTempDir);

                // Extract the ZIP to examine contents
                progressCallback?.Invoke("Extracting package for inspection...", 55);
                ZipFile.ExtractToDirectory(zipPath, extractTempDir);

                // Determine if we have the expected .trex folder
                string trexFolderPath = Path.Combine(extractTempDir, "bin", ".trex");
                bool hasTrexFolder = Directory.Exists(trexFolderPath);

                if (!hasTrexFolder)
                {
                    throw new Exception("Package does not contain the expected bin/.trex folder structure.");
                }

                // Determine installation paths based on Garry's Mod version (x32 or x64)
                string installType = GarrysModInstallSystem.GetInstallType(installDir);
                bool isX64 = installType == "gmod_x86-64";

                string destPath;
                if (isX64)
                {
                    // For x64, extract the contents of bin/.trex to bin/win64
                    destPath = Path.Combine(installDir, "bin", "win64");
                    progressCallback?.Invoke("Detected x64 installation. Installing to bin/win64...", 60);
                }
                else
                {
                    // For x32, maintain the original path structure
                    destPath = Path.Combine(installDir, "bin");
                    progressCallback?.Invoke("Detected x32 installation. Installing to bin/.trex...", 60);
                }

                // Make sure the destination exists
                Directory.CreateDirectory(destPath);

                // Install based on architecture
                if (isX64)
                {
                    // For x64: Copy contents from bin/.trex to bin/win64
                    progressCallback?.Invoke("Copying files to bin/win64...", 65);
                    
                    // Get all files in the .trex folder
                    string[] trexFiles = Directory.GetFiles(trexFolderPath, "*.*", SearchOption.AllDirectories);
                    int totalFiles = trexFiles.Length;
                    int filesCopied = 0;

                    foreach (string srcFilePath in trexFiles)
                    {
                        // Calculate relative path from .trex folder
                        string relativePath = srcFilePath.Substring(trexFolderPath.Length + 1);
                        string destFilePath = Path.Combine(destPath, relativePath);

                        // Create directory if needed
                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));

                        // Copy the file
                        File.Copy(srcFilePath, destFilePath, true);

                        filesCopied++;
                        int progressPercent = 65 + (int)((float)filesCopied / totalFiles * 30);
                        progressCallback?.Invoke($"Copying file {filesCopied} of {totalFiles}: {relativePath}", progressPercent);
                    }
                }
                else
                {
                    // For x32: Copy the entire bin folder structure as is
                    progressCallback?.Invoke("Copying directory structure to bin...", 65);
                    
                    // Source directory is the 'bin' folder that contains .trex
                    string binFolder = Path.Combine(extractTempDir, "bin");
                    
                    // Get all files in the bin folder
                    string[] binFiles = Directory.GetFiles(binFolder, "*.*", SearchOption.AllDirectories);
                    int totalFiles = binFiles.Length;
                    int filesCopied = 0;

                    foreach (string srcFilePath in binFiles)
                    {
                        // Calculate relative path from bin folder
                        string relativePath = srcFilePath.Substring(binFolder.Length + 1);
                        string destFilePath = Path.Combine(Path.Combine(installDir, "bin"), relativePath);

                        // Create directory if needed
                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));

                        // Copy the file
                        File.Copy(srcFilePath, destFilePath, true);

                        filesCopied++;
                        int progressPercent = 65 + (int)((float)filesCopied / totalFiles * 30);
                        progressCallback?.Invoke($"Copying file {filesCopied} of {totalFiles}: {relativePath}", progressPercent);
                    }
                }

                // Clean up
                progressCallback?.Invoke("Cleaning up temporary files...", 95);
                try { Directory.Delete(tempDir, true); } catch { }

                progressCallback?.Invoke("OptiScaler installed successfully!", 100);
            }
            catch
            {
                // Clean up on error
                try { Directory.Delete(tempDir, true); } catch { }
                throw;
            }
        }
    }
}