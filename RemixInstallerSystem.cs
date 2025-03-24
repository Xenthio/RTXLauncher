using System.IO.Compression;

namespace RTXLauncher
{
	public static class RTXRemix
	{
		/// <summary>
		/// Installs RTX Remix from GitHub release
		/// </summary>
		/// <param name="selectedRelease">The GitHub release to install</param>
		/// <param name="owner">The GitHub repository owner</param>
		/// <param name="repo">The GitHub repository name</param>
		/// <param name="installType">The Garry's Mod installation type</param>
		/// <param name="basePath">The base application path</param>
		/// <param name="progressCallback">Callback for reporting progress</param>
		/// <returns>A task representing the installation operation</returns>
		public static async Task Install(
			GitHubRelease selectedRelease,
			string owner,
			string repo,
			string installType,
			string basePath,
			Action<string, int> progressCallback)
		{
			if (selectedRelease == null)
				throw new ArgumentNullException(nameof(selectedRelease), "No release selected");

			progressCallback?.Invoke($"Starting RTX Remix installation from {owner}/{repo}...", 0);

			try
			{
				// Store repository info for debugging/logging
				progressCallback?.Invoke($"Repository: {owner}/{repo}, Release: {selectedRelease.Name}", 5);

				// Find the most appropriate asset to download using prioritized search
				GitHubAsset assetToDownload = FindBestReleaseAsset(selectedRelease);

				if (assetToDownload == null)
				{
					throw new Exception("Could not find a suitable release asset to download.");
				}

				progressCallback?.Invoke($"Found asset: {assetToDownload.Name}", 10);

				// Create a temporary directory for downloading
				string tempDir = Path.Combine(Path.GetTempPath(), "RTXRemixTemp");
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
				Directory.CreateDirectory(tempDir);

				// Download the zip file
				string zipPath = Path.Combine(tempDir, assetToDownload.Name);
				progressCallback?.Invoke($"Downloading {assetToDownload.Name}...", 15);

				await DownloadFileWithProgress(
					assetToDownload.BrowserDownloadUrl,
					zipPath,
					(progress, total) =>
					{
						int percentComplete = (int)((float)progress / total * 45) + 15;
						progressCallback?.Invoke(
							$"Downloading: {progress / 1048576} MB / {total / 1048576} MB",
							percentComplete);
					});

				progressCallback?.Invoke("Download complete. Analyzing package...", 60);

				// Determine the destination path based on installation type
				string destPath = installType == "gmod_main"
					? Path.Combine(basePath, "bin")
					: Path.Combine(basePath, "bin", "win64");

				// Make sure the destination exists
				if (!Directory.Exists(destPath))
					Directory.CreateDirectory(destPath);

				// Extract and install files based on what we find in the zip
				await InstallRTXRemixPackage(zipPath, tempDir, destPath, installType, progressCallback);

				// Clean up
				progressCallback?.Invoke("Cleaning up temporary files...", 95);
				try { Directory.Delete(tempDir, true); } catch { }

				progressCallback?.Invoke("RTX Remix installed successfully!", 100);
			}
			catch (Exception ex)
			{
				progressCallback?.Invoke($"Error: {ex.Message}", 100);
				throw; // Re-throw to let the caller handle it
			}
		}

		/// <summary>
		/// Finds the best release asset to download based on standardized priorities
		/// </summary>
		private static GitHubAsset FindBestReleaseAsset(GitHubRelease release)
		{
			// Prioritized list of asset naming patterns to look for
			string[] assetPatterns = new[]
			{
            // Official naming patterns (newer format)
            "-release.zip",
			"-debugoptimized.zip",
			"-debug.zip",
            
            // Community versions or older formats might just be .zip files
            ".zip"
		};

			// Try each pattern in order until we find a matching asset
			foreach (var pattern in assetPatterns)
			{
				// Skip any assets with "symbols" in the name
				var matchingAssets = release.Assets
					.Where(a => a.Name.Contains(pattern) && !a.Name.Contains("-symbols"))
					.ToList();

				if (matchingAssets.Count > 0)
					return matchingAssets[0];
			}

			// If no match found with our patterns, just return any zip file
			return release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip") && !a.Name.Contains("-symbols"));
		}

		/// <summary>
		/// Installs RTX Remix package by examining its content and adapting accordingly
		/// </summary>
		private static async Task InstallRTXRemixPackage(
			string zipPath,
			string tempDir,
			string destPath,
			string installType,
			Action<string, int> progressCallback)
		{
			// Create a temporary extraction directory
			string extractTempDir = Path.Combine(tempDir, "extracted");
			Directory.CreateDirectory(extractTempDir);

			// First, examine the zip contents without extracting fully
			progressCallback?.Invoke("Analyzing package contents...", 65);
			bool containsTrexFolder = false;
			bool containsD3D9Dll = false;

			using (var zip = ZipFile.OpenRead(zipPath))
			{
				// Check for .trex folder
				containsTrexFolder = zip.Entries.Any(e =>
					e.FullName.Contains(".trex/") || e.FullName.Contains(".trex\\"));

				// Check for d3d9.dll
				containsD3D9Dll = zip.Entries.Any(e =>
					Path.GetFileName(e.FullName).Equals("d3d9.dll", StringComparison.OrdinalIgnoreCase));
			}

			if (installType == "gmod_main")
			{
				// For gmod_main (32-bit) installation, we usually extract everything directly
				progressCallback?.Invoke("Installing to 32-bit Garry's Mod (bin folder)...", 70);
				await ExtractZipWithProgress(zipPath, destPath,
					(current, total) =>
					{
						int progressPercent = 70 + (int)((float)current / total * 25);
						progressCallback?.Invoke($"Extracting: {current} / {total}", progressPercent);
					});
			}
			else if (containsTrexFolder)
			{
				// For gmod_x86-64 with .trex folder, extract selectively
				progressCallback?.Invoke("Found .trex folder, extracting to win64...", 70);

				// Extract the full zip first
				ZipFile.ExtractToDirectory(zipPath, extractTempDir);

				// Find the .trex folder
				string[] trexFolders = Directory.GetDirectories(extractTempDir, "*.trex", SearchOption.AllDirectories);

				if (trexFolders.Length > 0)
				{
					string trexFolder = trexFolders[0];
					progressCallback?.Invoke($"Found .trex folder: {Path.GetFileName(trexFolder)}", 80);

					// Copy all contents from .trex folder to destination
					await CopyDirectoryWithProgress(trexFolder, destPath, true,
						(current, total) =>
						{
							int progress = 80 + (int)((float)current / total * 15);
							progressCallback?.Invoke($"Copying files: {current} / {total}", progress);
						});
				}
				else
				{
					throw new Exception("Unexpected error: .trex folder not found after extraction");
				}
			}
			else if (containsD3D9Dll)
			{
				// For packages with d3d9.dll but no .trex folder
				progressCallback?.Invoke("Found d3d9.dll, extracting to destination...", 70);

				// Extract everything to the destination
				await ExtractZipWithProgress(zipPath, destPath,
					(current, total) =>
					{
						int progressPercent = 70 + (int)((float)current / total * 25);
						progressCallback?.Invoke($"Extracting: {current} / {total}", progressPercent);
					});
			}
			else
			{
				// Fallback: extract everything and hope for the best
				progressCallback?.Invoke("No specific structure detected, extracting all files...", 70);
				await ExtractZipWithProgress(zipPath, destPath,
					(current, total) =>
					{
						int progressPercent = 70 + (int)((float)current / total * 25);
						progressCallback?.Invoke($"Extracting: {current} / {total}", progressPercent);
					});
			}
		}

		#region Helper Methods

		// Track the last reported progress
		private static long _lastReportedMB = 0;
		private static readonly int _reportThresholdMB = 5; // Report every 5MB
		private static async Task DownloadFileWithProgress(
		string url,
		string destinationPath,
		Action<long, long> progressCallback)
		{
			// Reset the last reported progress
			_lastReportedMB = 0;

			using (var client = new HttpClient())
			{
				using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
				{
					if (!response.IsSuccessStatusCode)
					{
						throw new Exception($"Failed to download: {response.StatusCode}");
					}

					long totalBytes = response.Content.Headers.ContentLength ?? -1;
					using (var downloadStream = await response.Content.ReadAsStreamAsync())
					using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
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
								// Only report progress if we've downloaded at least _reportThresholdMB more
								long currentMB = totalBytesRead / 1048576;
								if (currentMB >= _lastReportedMB + _reportThresholdMB || totalBytesRead == totalBytes)
								{
									_lastReportedMB = currentMB;
									progressCallback?.Invoke(totalBytesRead, totalBytes);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Extracts a zip file with progress reporting
		/// </summary>
		private static async Task ExtractZipWithProgress(
			string zipPath,
			string destinationPath,
			Action<int, int> progressCallback)
		{
			await Task.Run(() =>
			{
				using (var zip = ZipFile.OpenRead(zipPath))
				{
					int totalEntries = zip.Entries.Count;
					int entriesProcessed = 0;

					foreach (var entry in zip.Entries)
					{
						string entryDestPath = Path.Combine(destinationPath, entry.FullName);

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
						progressCallback?.Invoke(entriesProcessed, totalEntries);
					}
				}
			});
		}

		/// <summary>
		/// Copies a directory with progress reporting
		/// </summary>
		private static async Task CopyDirectoryWithProgress(
			string sourceDir,
			string destDir,
			bool overwrite,
			Action<int, int> progressCallback)
		{
			await Task.Run(() =>
			{
				// Create the destination directory if it doesn't exist
				Directory.CreateDirectory(destDir);

				// Get total files to copy
				int totalFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length;
				int filesCopied = 0;

				// Get all files in the source directory and its subdirectories
				string[] sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

				// Copy each file
				foreach (string sourceFile in sourceFiles)
				{
					// Calculate the relative path and create the destination path
					string relativePath = sourceFile.Substring(sourceDir.Length + 1);
					string destFile = Path.Combine(destDir, relativePath);

					// Create the destination directory if it doesn't exist
					string destFileDir = Path.GetDirectoryName(destFile);
					if (!Directory.Exists(destFileDir))
						Directory.CreateDirectory(destFileDir);

					// Copy the file
					File.Copy(sourceFile, destFile, overwrite);

					filesCopied++;
					progressCallback?.Invoke(filesCopied, totalFiles);
				}
			});
		}

		#endregion
	}
}
