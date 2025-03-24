using System.IO.Compression;

namespace RTXLauncher
{
	public static class RTXRemix
	{
		/// <summary>
		/// Installs RTX Remix from GitHub release
		/// </summary>
		/// <param name="selectedRelease">The GitHub release to install</param>
		/// <param name="installToMainFolder">The gmod type this is</param>
		/// <param name="basePath">The base application path</param>
		/// <param name="progressCallback">Callback for reporting progress</param>
		/// <returns>A task representing the installation operation</returns>
		public static async Task Install(
			GitHubRelease selectedRelease,
			string installType,
			string basePath,
			Action<string, int> progressCallback)
		{
			if (selectedRelease == null)
				throw new ArgumentNullException(nameof(selectedRelease), "No release selected");

			progressCallback?.Invoke("Starting RTX Remix installation...", 0);

			try
			{
				// Determine which asset to download based on the selected release
				// Find the release zip (prioritize release build, then debugoptimized, then debug)
				GitHubAsset assetToDownload = null;

				// Check file naming pattern based on version
				if (selectedRelease.Assets.Any(a => a.Name.Contains("-release.zip") && !a.Name.Contains("-symbols")))
				{
					// Newer release format (1.0.0+)
					assetToDownload = selectedRelease.Assets.FirstOrDefault(a =>
						a.Name.Contains("-release.zip") && !a.Name.Contains("-symbols"));
				}
				else if (selectedRelease.Assets.Any(a => a.Name.Contains("-debugoptimized.zip") && !a.Name.Contains("-symbols")))
				{
					assetToDownload = selectedRelease.Assets.FirstOrDefault(a =>
						a.Name.Contains("-debugoptimized.zip") && !a.Name.Contains("-symbols"));
				}
				else if (selectedRelease.Assets.Any(a => a.Name.Contains("-debug.zip") && !a.Name.Contains("-symbols")))
				{
					assetToDownload = selectedRelease.Assets.FirstOrDefault(a =>
						a.Name.Contains("-debug.zip") && !a.Name.Contains("-symbols"));
				}
				else
				{
					// Older release format (before 1.0.0)
					assetToDownload = selectedRelease.Assets.FirstOrDefault(a =>
						a.Name.EndsWith(".zip") && !a.Name.Contains("-symbols"));
				}

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

				progressCallback?.Invoke("Download complete. Extracting files...", 60);

				// Determine the destination path based on installation type
				string destPath = installType == "gmod_main"
					? Path.Combine(basePath, "bin")
					: Path.Combine(basePath, "bin", "win64");

				// Make sure the destination exists
				if (!Directory.Exists(destPath))
					Directory.CreateDirectory(destPath);

				// Extract files
				if (installType == "gmod_main")
				{
					// For gmod_main, extract the whole zip to bin folder
					progressCallback?.Invoke("Extracting to bin folder...", 70);
					await ExtractZipWithProgress(zipPath, destPath,
						(current, total) =>
						{
							int progressPercent = 70 + (int)((float)current / total * 25);
							progressCallback?.Invoke($"Extracting: {current} / {total}", progressPercent);
						});
				}
				else
				{
					// For gmod_x86-64, find and extract .trex folder contents to bin/win64
					progressCallback?.Invoke("Extracting .trex folder to bin/win64...", 70);
					string extractTempDir = Path.Combine(tempDir, "extracted");
					Directory.CreateDirectory(extractTempDir);

					// Extract the full zip first
					ZipFile.ExtractToDirectory(zipPath, extractTempDir);

					// Now find the .trex folder
					string[] trexFolders = Directory.GetDirectories(extractTempDir, "*.trex", SearchOption.AllDirectories);
					if (trexFolders.Length == 0)
					{
						throw new Exception("Could not find .trex folder in the extracted files.");
					}

					string trexFolder = trexFolders[0]; // Take the first .trex folder
					progressCallback?.Invoke($"Found .trex folder: {Path.GetFileName(trexFolder)}", 80);

					// Copy all contents from .trex folder to bin/win64
					await CopyDirectoryWithProgress(trexFolder, destPath, true,
						(current, total) =>
						{
							int progress = 80 + (int)((float)current / total * 15);
							progressCallback?.Invoke($"Copying files: {current} / {total}", progress);
						});
				}

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
