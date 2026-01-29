// Services/PackageInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.IO.Compression;

namespace RTXLauncher.Core.Services;

public class PackageInstallService
{
	private readonly HttpClient _httpClient;
	private readonly DownloadManager _downloadManager;
	private static readonly string CacheDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"RTXLauncher",
		"Cache");

	public static Dictionary<string, (string Owner, string Repo)> RemixSources = new Dictionary<string, (string, string)>
	{
		{ "sambow23/dxvk-remix-gmod", ("sambow23", "dxvk-remix-gmod") },
	};

	public static Dictionary<string, (string Owner, string Repo, string InstallType)> PackageSources = new Dictionary<string, (string, string, string)>
	{
		{ "Xenthio/garrys-mod-rtx-remixed (Any)", ("Xenthio", "garrys-mod-rtx-remixed", "Any") },
		{ "sambow23/garrys-mod-rtx-remixed-perf (Any)", ("sambow23", "garrys-mod-rtx-remixed-perf", "main") }
	};

	public static Dictionary<string, (string Owner, string Repo, string FilePath, string Branch)> PatchSources = new Dictionary<string, (string, string, string, string)>
	{
		{ "BlueAmulet/SourceRTXTweaks", ("BlueAmulet", "SourceRTXTweaks", "applypatch.py", "master") },
		{ "sambow23/SourceRTXTweaks (for gmod-rtx-fixes-2)", ("sambow23", "SourceRTXTweaks", "applypatch.py", "main") },
		{ "sambow23/SourceRTXTweaks (for garrys-mod-rtx-remixed-perf)", ("sambow23", "SourceRTXTweaks", "applypatch.py", "perf") },
		{ "Xenthio/SourceRTXTweaks (outdated, here to test multiple repos)", ("Xenthio", "SourceRTXTweaks", "applypatch.py", "master") }
	};

	public static readonly string DefaultIgnorePatterns =
@"
# We ignore these because the installer does rtx remix installations for us, 
# but some packages might contain them for easy manual installation.

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
	public PackageInstallService(DownloadManager? downloadManager = null)
	{
		_httpClient = new HttpClient();
		// GitHub API can sometimes reject requests without a User-Agent
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher");
		
		_downloadManager = downloadManager ?? new DownloadManager();
		
		// Ensure cache directory exists
		Directory.CreateDirectory(CacheDirectory);
	}

	/// <summary>
	/// Checks if rtx.conf exists in the installation directory and backs it up if requested.
	/// This should be called before any installation that might overwrite rtx.conf.
	/// </summary>
	/// <param name="installDir">The installation directory to check</param>
	/// <param name="shouldBackup">Whether to create a backup (typically true if user confirmed)</param>
	/// <returns>True if no config exists or backup succeeded, false if backup was requested but failed</returns>
	public static bool CheckAndBackupRtxConfig(string installDir, bool shouldBackup = true)
	{
		if (!RemixUtility.RtxConfigExists(installDir))
		{
			return true; // No config exists, safe to proceed
		}

		if (!shouldBackup)
		{
			return true; // User chose not to backup
		}

		var backupPath = RemixUtility.BackupRtxConfig(installDir);
		return backupPath != null; // Return true if backup succeeded
	}
	/// <summary>
	/// Installs RTX Remix, which has special logic for finding .trex/bin folders
	/// and placing them in the correct 32-bit or 64-bit game directory.
	/// </summary>
	public async Task InstallRemixPackageAsync(GitHubRelease release, string installDir, IProgress<InstallProgressReport> progress)
	{
		var asset = FindBestReleaseAsset(release);
		if (asset == null) throw new Exception("Could not find a suitable zip package in the release.");

		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncherRemix_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			// --- 1. Download ---
			string zipPath = Path.Combine(tempDir, asset.Name);
			var downloadProgress = new Progress<DownloadProgressReport>(report =>
			{
				int percentage = report.TotalBytes > 0 ? (int)((double)report.BytesDownloaded / report.TotalBytes * 40) : 20;
				progress.Report(new InstallProgressReport { Message = $"Downloading: {report.BytesDownloaded / 1048576}MB", Percentage = percentage });
			});
			await DownloadFileAsync(asset.BrowserDownloadUrl, zipPath, downloadProgress, asset.CreatedAt);

			// --- 2. Extract to a temporary location for inspection ---
			string extractTempDir = Path.Combine(tempDir, "extracted");
			Directory.CreateDirectory(extractTempDir);
			progress.Report(new InstallProgressReport { Message = "Extracting package for inspection...", Percentage = 45 });
			ZipFile.ExtractToDirectory(zipPath, extractTempDir);

		// --- 3. Determine correct source and destination paths ---
		progress.Report(new InstallProgressReport { Message = "Analyzing package structure...", Percentage = 55 });
		var installType = GarrysModUtility.GetInstallType(installDir);

		string? sourcePath = null;
		string? destPath = null;

	// Find the deepest .trex or bin folder in the extracted contents
	var trexFolder = Directory.GetDirectories(extractTempDir, "*.trex", SearchOption.AllDirectories).FirstOrDefault();
	var binFolder = Directory.GetDirectories(extractTempDir, "bin", SearchOption.AllDirectories).FirstOrDefault();

	if (installType == "gmod_x86-64")
	{
		// 64-bit: Copy contents of .trex (or bin) to bin/win64
		if (trexFolder != null)
		{
			// Check for nested win64 folder
			var nestedWin64 = Path.Combine(trexFolder, "win64");
			if (Directory.Exists(nestedWin64))
			{
				sourcePath = nestedWin64;
				progress.Report(new InstallProgressReport { Message = "Found nested win64 folder inside .trex for 64-bit install.", Percentage = 60 });
			}
			else
			{
				sourcePath = trexFolder;
				progress.Report(new InstallProgressReport { Message = "Found .trex folder for 64-bit install.", Percentage = 60 });
			}
			destPath = Path.Combine(installDir, "bin", "win64");
		}
		else if (binFolder != null)
		{
			sourcePath = binFolder;
			destPath = Path.Combine(installDir, "bin", "win64");
			progress.Report(new InstallProgressReport { Message = "Found bin folder for 64-bit install.", Percentage = 60 });
		}
		else
		{
			var allDirs = Directory.GetDirectories(extractTempDir, "*", SearchOption.AllDirectories)
				.Select(d => Path.GetRelativePath(extractTempDir, d))
				.Take(20);
			var dirList = string.Join(", ", allDirs);
			throw new Exception($"Remix package does not have a recognizable structure (missing 'bin' or '.trex' folder).\n\nFound directories: {dirList}");
		}

		Directory.CreateDirectory(destPath);

		// Copy the contents
		var copyProgress = new Progress<InstallProgressReport>(report =>
		{
			progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 60 + (int)(report.Percentage * 0.4) });
		});
		await CopyDirectoryWithProgress(sourcePath, destPath, true, copyProgress);
	}
	else
	{
		// 32-bit: Copy all archive contents (including .trex folder itself) to bin
		progress.Report(new InstallProgressReport { Message = "Found package for 32-bit install.", Percentage = 60 });
		
		destPath = Path.Combine(installDir, "bin");
		Directory.CreateDirectory(destPath);

		// Copy all files and folders from the extracted archive to bin/
		var copyProgress = new Progress<InstallProgressReport>(report =>
		{
			progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 60 + (int)(report.Percentage * 0.4) });
		});
		
		// Copy all contents of extractTempDir (which includes .trex folder and any loose files)
		await CopyDirectoryWithProgress(extractTempDir, destPath, true, copyProgress);
	}

			progress.Report(new InstallProgressReport { Message = "Remix installed successfully!", Percentage = 100 });
		}
		finally
		{
			if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
		}
	}

	/// <summary>
	/// Installs a standard package (like Fixes) by extracting its contents directly into the install folder.
	/// </summary>
	public async Task InstallStandardPackageAsync(GitHubRelease release, string installDir, string defaultIgnorePatterns, IProgress<InstallProgressReport> progress)
	{
		// This method was previously named InstallGenericPackageAsync
		var asset = FindBestReleaseAsset(release);
		if (asset == null) throw new Exception("Could not find a suitable zip package in the release.");

		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncherPkg_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			string zipPath = Path.Combine(tempDir, asset.Name);

			var downloadProgress = new Progress<DownloadProgressReport>(report =>
			{
				int percentage = report.TotalBytes > 0 ? (int)((double)report.BytesDownloaded / report.TotalBytes * 40) : 20;
				progress.Report(new InstallProgressReport { Message = $"Downloading: {report.BytesDownloaded / 1048576}MB", Percentage = percentage });
			});
			await DownloadFileAsync(asset.BrowserDownloadUrl, zipPath, downloadProgress, asset.CreatedAt);

			// Perform pre-install cleanup (remove outdated folders before extraction)
			progress.Report(new InstallProgressReport { Message = "Removing outdated folders...", Percentage = 45 });
			var preCleanupMessages = CleanupUtility.PerformPreInstallCleanup(installDir);
			foreach (var message in preCleanupMessages)
			{
				progress.Report(new InstallProgressReport { Message = $"Pre-cleanup: {message}", Percentage = 45 });
			}

			var extractProgress = new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 50 + (int)(report.Percentage * 0.5) });
			});
			await ExtractZipWithIgnoreAsync(zipPath, installDir, defaultIgnorePatterns, extractProgress);

			progress.Report(new InstallProgressReport { Message = "Installation complete!", Percentage = 100 });
		}
		finally
		{
			if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
		}
	}

	// --- Private Helper Methods ---

	private async Task DownloadFileAsync(string url, string destinationPath, IProgress<DownloadProgressReport> progress, DateTime? assetTimestamp = null)
	{
		// Generate cache filename from URL hash + optional timestamp
		// Including the timestamp ensures that even if the URL is the same (e.g. nightly builds),
		// we download the new version if the asset creation time has changed.
		string cacheKeyInput = url + (assetTimestamp.HasValue ? assetTimestamp.Value.Ticks.ToString() : "");
		string urlHash = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(cacheKeyInput)));
		string cacheFileName = Path.GetFileName(url.Split('?')[0]); // Get filename from URL without query params
		string cachedFilePath = Path.Combine(CacheDirectory, $"{urlHash}_{cacheFileName}");

		// Check if file is already cached
		if (File.Exists(cachedFilePath))
		{
			progress.Report(new DownloadProgressReport
			{
				BytesDownloaded = new FileInfo(cachedFilePath).Length,
				TotalBytes = new FileInfo(cachedFilePath).Length,
				Percentage = 100
			});
			
			// Copy from cache to destination
			File.Copy(cachedFilePath, destinationPath, overwrite: true);
			return;
		}

		// Configure download options
		var downloadOptions = new DownloadOptions
		{
			MaxRetries = 5,
			TimeoutMinutes = 10,
			AllowResume = true,
			UserAgent = "RTXLauncher"
		};

		// Map EnhancedDownloadProgress to DownloadProgressReport
		var mappedProgress = new Progress<EnhancedDownloadProgress>(enhancedProgress =>
		{
			progress.Report(new DownloadProgressReport
			{
				BytesDownloaded = enhancedProgress.BytesDownloaded,
				TotalBytes = enhancedProgress.TotalBytes,
				Percentage = enhancedProgress.PercentComplete
			});
		});

		// Download to cache using DownloadManager
		var result = await _downloadManager.DownloadFileAsync(
			url,
			cachedFilePath,
			downloadOptions,
			mappedProgress);

		if (!result.Success)
		{
			// Clean up failed cache file
			if (File.Exists(cachedFilePath))
			{
				File.Delete(cachedFilePath);
			}
			throw new Exception($"Download failed: {result.ErrorMessage}", result.Exception);
		}

		// Copy from cache to destination
		File.Copy(cachedFilePath, destinationPath, overwrite: true);
	}

	private async Task ExtractZipWithIgnoreAsync(string zipPath, string installDir, string defaultIgnore, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			var ignoredPaths = ParseIgnorePatterns(defaultIgnore);

			using var zip = ZipFile.OpenRead(zipPath);
			var launcherIgnoreEntry = zip.Entries.FirstOrDefault(e => e.Name.Equals(".launcherignore", StringComparison.OrdinalIgnoreCase));
			if (launcherIgnoreEntry != null)
			{
				// This logic requires a temporary file write/read.
				// For simplicity in a single file, you could extract to a MemoryStream.
				using var reader = new StreamReader(launcherIgnoreEntry.Open());
				var customPatterns = ParseIgnorePatterns(reader.ReadToEnd());
				ignoredPaths.UnionWith(customPatterns);
			}

			var entriesToExtract = zip.Entries.Where(e => !ShouldIgnore(e.FullName, ignoredPaths)).ToList();
			int total = entriesToExtract.Count;
			int processed = 0;

			foreach (var entry in entriesToExtract)
			{
				string destPath = Path.Combine(installDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

				if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
				{
					Directory.CreateDirectory(destPath);
				}
				else
				{
					Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
					entry.ExtractToFile(destPath, true);
				}
				processed++;
				progress.Report(new InstallProgressReport { Message = $"Extracting: {entry.Name}", Percentage = (processed * 100) / total });
			}
		});
	}

	private async Task CopyDirectoryWithProgress(string sourceDir, string destDir, bool overwrite, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			Directory.CreateDirectory(destDir);
			var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
			int total = allFiles.Length;
			int copied = 0;

			foreach (string sourceFile in allFiles)
			{
				string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
				string destFile = Path.Combine(destDir, relativePath);
				Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
				File.Copy(sourceFile, destFile, overwrite);
				copied++;
				progress.Report(new InstallProgressReport { Message = $"Copying: {Path.GetFileName(sourceFile)}", Percentage = (copied * 100) / total });
			}
		});
	}

	private GitHubAsset? FindBestReleaseAsset(GitHubRelease release)
	{
		string[] patterns = { "-gmod.zip", "-release.zip", ".zip" };
		foreach (var pattern in patterns)
		{
			var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) && !a.Name.Contains("-symbols"));
			if (asset != null) return asset;
		}
		return null;
	}

	private HashSet<string> ParseIgnorePatterns(string ignorePatterns)
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(ignorePatterns)) return result;

		foreach (var line in ignorePatterns.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var trimmedLine = line.Trim();
			if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("#"))
			{
				result.Add(trimmedLine.Replace('\\', '/').TrimStart('/'));
			}
		}
		return result;
	}

	private bool ShouldIgnore(string entryPath, HashSet<string> ignoredPaths)
	{
		string normalizedPath = entryPath.Replace('\\', '/').TrimStart('/');
		if (ignoredPaths.Contains(normalizedPath)) return true;

		foreach (var pattern in ignoredPaths.Where(p => p.EndsWith("/*")))
		{
			if (normalizedPath.StartsWith(pattern.Substring(0, pattern.Length - 1)))
			{
				return true;
			}
		}
		return false;
	}
}