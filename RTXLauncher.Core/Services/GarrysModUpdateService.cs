// Services/UpdateService.cs
namespace RTXLauncher.Core.Services;

// The FileUpdateInfo class can be moved to a Models file
public class FileUpdateInfo
{
	public string RelativePath { get; set; }
	public string SourcePath { get; set; }
	public string DestinationPath { get; set; }
	public bool IsDirectory { get; set; }
	public bool IsNew { get; set; }
	public bool IsChanged { get; set; }
	public long Size { get; set; }
	public DateTime SourceLastModified { get; set; }
	public DateTime? DestLastModified { get; set; }

	public string Status => IsNew ? "New" : (IsChanged ? "Changed" : "Same");
}

public class UpdateProgressReport
{
	public string Message { get; init; } = string.Empty;
	public int Percentage { get; init; }
}

public class GarrysModUpdateService
{
	/// <summary>
	/// Detects which files have changed between a source and destination directory.
	/// </summary>
	/// <returns>A list of files that are new or have been changed.</returns>
	public List<FileUpdateInfo> CheckForUpdates(string sourceDir, string destDir)
	{
		var result = new List<FileUpdateInfo>();

		// Exclude these directories from update checks
		var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"addons", "saves", "dupes", "demos", "settings", "cache",
				"materials", "models", "maps", "screenshots", "videos", "download"
			};

		// Exclude these file extensions
		var excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".dem", ".log", ".vpk"
			};

		// Scan directories recursively
		CheckDirectory(sourceDir, destDir, "", result, excludedDirs, excludedExtensions);

		return result;
	}
	private static void CheckDirectory(string sourceDir, string destDir, string relativePath,
			List<FileUpdateInfo> results, HashSet<string> excludedDirs, HashSet<string> excludedExtensions)
	{
		// Get source directory info
		var sourceDirInfo = new DirectoryInfo(Path.Combine(sourceDir, relativePath));
		if (!sourceDirInfo.Exists) return;

		// Additional directories to exclude at the root level
		var additionalRootExcludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"crashes", "logs", "temp", "update", "xenmod"
			};


		// Check if this directory should be excluded
		if (string.IsNullOrEmpty(relativePath) || !excludedDirs.Contains(relativePath.Split('\\', '/').First()))
		{
			// Check files in this directory
			foreach (var sourceFile in sourceDirInfo.GetFiles())
			{
				// Skip excluded file extensions
				if (excludedExtensions.Contains(sourceFile.Extension))
					continue;

				var fileRelativePath = string.IsNullOrEmpty(relativePath)
					? sourceFile.Name
					: Path.Combine(relativePath, sourceFile.Name);

				// For root directory, only include gmod.exe
				if (string.IsNullOrEmpty(relativePath) &&
					!string.Equals(sourceFile.Name, "gmod.exe", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(sourceFile.Name, "hl2.exe", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var destFilePath = Path.Combine(destDir, fileRelativePath);
				var fileInfo = new FileInfo(destFilePath);

				bool isNew = !fileInfo.Exists;

				// Compare files if the destination exists
				var comparisonResult = isNew
					? new FileComparisonResult { IsNew = true }
					: CompareFiles(sourceFile, fileInfo);

				// Add any new or changed file to results
				if (comparisonResult.IsNew || comparisonResult.IsChanged)
				{
					results.Add(new FileUpdateInfo
					{
						RelativePath = fileRelativePath,
						SourcePath = sourceFile.FullName,
						DestinationPath = destFilePath,
						IsDirectory = false,
						IsNew = comparisonResult.IsNew,
						IsChanged = comparisonResult.IsChanged,
						Size = sourceFile.Length,
						SourceLastModified = sourceFile.LastWriteTime,
						DestLastModified = isNew ? null : (DateTime?)fileInfo.LastWriteTime
					});
				}
			}

			// Recursively check subdirectories
			foreach (var sourceSubDir in sourceDirInfo.GetDirectories())
			{
				var subDirRelativePath = string.IsNullOrEmpty(relativePath)
					? sourceSubDir.Name
					: Path.Combine(relativePath, sourceSubDir.Name);

				// Skip excluded directories
				if (excludedDirs.Contains(sourceSubDir.Name))
					continue;

				// Skip additional root-level excluded directories
				if (string.IsNullOrEmpty(relativePath) && additionalRootExcludes.Contains(sourceSubDir.Name))
					continue;

				var destSubDir = Path.Combine(destDir, subDirRelativePath);

				// Check if directory is new
				if (!Directory.Exists(destSubDir))
				{
					results.Add(new FileUpdateInfo
					{
						RelativePath = subDirRelativePath,
						SourcePath = sourceSubDir.FullName,
						DestinationPath = destSubDir,
						IsDirectory = true,
						IsNew = true,
						IsChanged = false,
						Size = 0,
						SourceLastModified = sourceSubDir.LastWriteTime
					});
				}

				// Recursively check this subdirectory
				CheckDirectory(sourceDir, destDir, subDirRelativePath, results, excludedDirs, excludedExtensions);
			}
		}
	}

	// Class to hold file comparison results
	private class FileComparisonResult
	{
		public bool IsNew { get; set; }
		public bool IsChanged { get; set; }
	}

	// Compares two files and determines if they're different
	private static FileComparisonResult CompareFiles(FileInfo sourceFile, FileInfo destFile)
	{
		var result = new FileComparisonResult();

		// lmao my symlinked gwater 2 install was causing issues so I added this debug code
		if (false && destFile.Name.Equals("gmcl_gwater2_win64.dll", StringComparison.OrdinalIgnoreCase))
		{
			string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_comparison_log.txt");
			using (StreamWriter writer = new StreamWriter(logPath, true))
			{
				writer.WriteLine("=== DEBUG INFO FOR PROBLEM FILE ===");
				writer.WriteLine($"Time: {DateTime.Now}");
				writer.WriteLine($"Path: {destFile.FullName}");
				writer.WriteLine($"Exists: {destFile.Exists}");
				writer.WriteLine($"Length: {destFile.Length}");
				writer.WriteLine($"Source Length: {sourceFile.Length}");
				writer.WriteLine($"Dest Last Write: {destFile.LastWriteTime}");
				writer.WriteLine($"Source Last Write: {sourceFile.LastWriteTime}");
				writer.WriteLine($"Time Diff (seconds): {Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds)}");

				try
				{
					writer.WriteLine($"Attributes: {destFile.Attributes}");
					writer.WriteLine($"Is ReparsePoint: {destFile.Attributes.HasFlag(FileAttributes.ReparsePoint)}");
				}
				catch (Exception ex)
				{
					writer.WriteLine($"Error accessing attributes: {ex.Message}");
				}

				// Let's also check if the file has the same content
				try
				{
					byte[] sourceBytes = new byte[Math.Min(100, sourceFile.Length)];
					byte[] destBytes = new byte[Math.Min(100, destFile.Length)];

					using (var sourceStream = sourceFile.OpenRead())
					{
						sourceStream.Read(sourceBytes, 0, sourceBytes.Length);
					}

					using (var destStream = destFile.OpenRead())
					{
						destStream.Read(destBytes, 0, destBytes.Length);
					}

					bool contentSame = sourceBytes.SequenceEqual(destBytes);
					writer.WriteLine($"First 100 bytes match: {contentSame}");
				}
				catch (Exception ex)
				{
					writer.WriteLine($"Error comparing content: {ex.Message}");
				}

				writer.WriteLine("=====================================");
			}
		}

		try
		{
			// Special case for zero-size source files that might be symlinks/junctions
			if (sourceFile.Length == 0 && destFile.Length > 0)
			{
				// If the source is zero bytes but destination is not,
				// we'll assume it's a special file and skip updating it
				result.IsChanged = false;
				return result;
			}

			// If it's a symlink, we'll generally assume it's not changed
			if (IsSymbolicLink(destFile.FullName))
			{
				// For symlinks, we'll ignore size differences and just keep them as-is
				//result.IsChanged = false;

				// You can uncomment this if you specifically want to check timestamp differences, but I found it doesn't work for some reason.
				// But generally, it's better to leave symlinks alone
				result.IsChanged = Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds) > 60;
			}
			else
			{
				// Normal file comparison
				// We check both file size and last modified time
				result.IsChanged = sourceFile.Length != destFile.Length ||
							   Math.Abs((sourceFile.LastWriteTime - destFile.LastWriteTime).TotalSeconds) > 1;
			}
		}
		catch (Exception)
		{
			// If there's any error checking symlink status, assume not changed to be safe
			// This prevents unnecessary updating of special files (symlinks, junctions, etc.)
			result.IsChanged = false;
		}

		return result;
	}

	// Helper method to check if a file is a symbolic link
	private static bool IsSymbolicLink(string path)
	{
		try
		{
			FileInfo pathInfo = new FileInfo(path);

			// Check if it has the ReparsePoint attribute (which includes symlinks, junctions, etc.)
			bool hasReparsePoint = pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);

			// Many symlinks also have zero size
			bool hasZeroSize = pathInfo.Length == 0;

			// Check if it's a known symlink path (the specific one you're having trouble with)
			bool isKnownSymlinkPath = path.Contains("your_problematic_symlink_name");

			return hasReparsePoint || (hasZeroSize && isKnownSymlinkPath);
		}
		catch
		{
			// If we can't access the file for some reason, err on the side of caution
			return true;  // Assume it's a special file like a symlink
		}
	}

	/// <summary>
	/// Performs the file copy operation for a given list of updates.
	/// </summary>
	public async Task PerformUpdateAsync(List<FileUpdateInfo> updates, IProgress<UpdateProgressReport> progress)
	{
		await Task.Run(() =>
		{
			int total = updates.Count;
			int current = 0;

			foreach (var update in updates)
			{
				current++;
				int percentage = (current * 100) / total;

				try
				{
					if (update.IsDirectory)
					{
						progress.Report(new UpdateProgressReport { Message = $"Creating directory: {update.RelativePath}", Percentage = percentage });
						Directory.CreateDirectory(update.DestinationPath);
					}
					else
					{
						progress.Report(new UpdateProgressReport { Message = $"Updating: {update.RelativePath}", Percentage = percentage });

						// Ensure the directory exists
						Directory.CreateDirectory(Path.GetDirectoryName(update.DestinationPath));

						// Copy the file with overwrite
						File.Copy(update.SourcePath, update.DestinationPath, true);
					}
				}
				catch (Exception ex)
				{
					// Report errors, but continue with the next file
					progress.Report(new UpdateProgressReport { Message = $"ERROR updating {update.RelativePath}: {ex.Message}", Percentage = percentage });
				}
			}
			progress.Report(new UpdateProgressReport { Message = "Update complete!", Percentage = 100 });
		});
	}
}