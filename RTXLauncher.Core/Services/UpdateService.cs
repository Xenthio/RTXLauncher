using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Progress information for update operations
/// </summary>
public class UpdateProgress
{
	public string Message { get; set; } = string.Empty;
	public int PercentComplete { get; set; }
	public bool IsComplete { get; set; }
	public Exception? Error { get; set; }
}

/// <summary>
/// Result of checking for updates
/// </summary>
public class UpdateCheckResult
{
	public bool UpdateAvailable { get; set; }
	public List<UpdateSource> AvailableUpdates { get; set; } = new();
	public UpdateSource? LatestUpdate { get; set; }
	public string CurrentVersion { get; set; } = string.Empty;
	public Exception? Error { get; set; }
}

/// <summary>
/// Unified update service that works with both Avalonia and WinForms applications
/// </summary>
public class UpdateService
{
	private readonly GitHubService _gitHubService;
	private readonly string _owner = "Xenthio";
	private readonly string _repo = "RTXLauncher";

	public UpdateService(GitHubService gitHubService)
	{
		_gitHubService = gitHubService;
	}

	/// <summary>
	/// Gets the current application version
	/// </summary>
	public string GetCurrentVersion()
	{
		return VersionUtility.GetCurrentAssemblyVersion();
	}

	/// <summary>
	/// Checks for available updates
	/// </summary>
	public async Task<UpdateCheckResult> CheckForUpdatesAsync(bool forceRefresh = false)
	{
		var result = new UpdateCheckResult
		{
			CurrentVersion = GetCurrentVersion()
		};

		try
		{
			result.AvailableUpdates = await GetAvailableUpdateSourcesAsync(forceRefresh);
			
			// Find the latest non-staging release
			var latestRelease = result.AvailableUpdates
				.Where(u => !u.IsStaging)
				.OrderByDescending(u => u.Release?.PublishedAt ?? DateTime.MinValue)
				.FirstOrDefault();

			if (latestRelease != null)
			{
				result.LatestUpdate = latestRelease;
				result.UpdateAvailable = VersionUtility.CompareVersions(
					latestRelease.Version, result.CurrentVersion) > 0;
			}
		}
		catch (Exception ex)
		{
			result.Error = ex;
		}

		return result;
	}

	/// <summary>
	/// Gets all available update sources (releases and staging)
	/// </summary>
	public async Task<List<UpdateSource>> GetAvailableUpdateSourcesAsync(bool forceRefresh = false)
	{
		var updateSources = new List<UpdateSource>();

		try
		{
			// Add staging build
			await AddStagingBuildAsync(updateSources, forceRefresh);

			// Add releases
			await AddReleasesAsync(updateSources, forceRefresh);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error getting update sources: {ex.Message}");
			
			// Add fallback staging source if nothing else is available
			if (updateSources.Count == 0)
			{
				updateSources.Add(new UpdateSource
				{
					Name = "Development Build (Staging)",
					Version = "Latest",
					DownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/RTXLauncher/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe",
					IsStaging = true
				});
			}
		}

		return updateSources;
	}

	/// <summary>
	/// Downloads and prepares an update. Returns the path to the downloaded content.
	/// </summary>
	public async Task<string> DownloadUpdateAsync(UpdateSource updateSource, 
		IProgress<UpdateProgress>? progress = null)
	{
		if (updateSource == null)
			throw new ArgumentNullException(nameof(updateSource));

		var updateTempPath = Path.Combine(Path.GetTempPath(), "RTXLauncherUpdater");
		if (!Directory.Exists(updateTempPath))
		{
			Directory.CreateDirectory(updateTempPath);
		}

		progress?.Report(new UpdateProgress { Message = "Preparing download...", PercentComplete = 0 });

		try
		{
			string versionTag = updateSource.IsStaging ? "dev_build" : updateSource.Version.Replace(".", "_");
			string downloadFolder = Path.Combine(updateTempPath, $"RTXLauncher_Update_{versionTag}");
			
			// Clean up existing files
			if (Directory.Exists(downloadFolder))
			{
				Directory.Delete(downloadFolder, true);
			}
			Directory.CreateDirectory(downloadFolder);

			progress?.Report(new UpdateProgress { Message = "Starting download...", PercentComplete = 10 });

			// Download the update
			string downloadPath = await DownloadFileAsync(updateSource.DownloadUrl, downloadFolder, progress);

			progress?.Report(new UpdateProgress { Message = "Download complete", PercentComplete = 100, IsComplete = true });

			return downloadFolder;
		}
		catch (Exception ex)
		{
			progress?.Report(new UpdateProgress { Message = $"Download failed: {ex.Message}", Error = ex });
			throw;
		}
	}

	/// <summary>
	/// Determines the correct executable name based on the current application type
	/// </summary>
	public string GetTargetExecutableName()
	{
		// Check if we're running the Avalonia or WinForms version
		var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
		var assemblyName = entryAssembly?.GetName().Name ?? "";
		
		if (assemblyName.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
		{
			return "RTXLauncher.Avalonia.exe";
		}
		else if (assemblyName.Contains("WinForms", StringComparison.OrdinalIgnoreCase))
		{
			return "RTXLauncher.WinForms.exe";
		}
		
		// Fallback - try to determine from current executable path
		var currentExe = Process.GetCurrentProcess().ProcessName;
		if (currentExe.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
		{
			return "RTXLauncher.Avalonia.exe";
		}
		else if (currentExe.Contains("WinForms", StringComparison.OrdinalIgnoreCase))
		{
			return "RTXLauncher.WinForms.exe";
		}
		
		// Default fallback
		return "RTXLauncher.exe";
	}

	private async Task AddStagingBuildAsync(List<UpdateSource> updateSources, bool forceRefresh)
	{
		try
		{
			var devDownload = await _gitHubService.FetchLatestStagingArtifact(_owner, _repo, forceRefresh);
			string artifactCommitHash = devDownload.Name.StartsWith("RTXLauncher-") 
				? devDownload.Name.Substring("RTXLauncher-".Length)
				: "latest";

			updateSources.Add(new UpdateSource
			{
				Name = "Development Build (Staging)",
				Version = $"dev-{artifactCommitHash}",
				DownloadUrl = devDownload.ArchiveDownloadUrl,
				IsStaging = true
			});
		}
		catch
		{
			// If staging fails, add fallback
			updateSources.Add(new UpdateSource
			{
				Name = "Development Build (Staging)",
				Version = "dev-latest",
				DownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/RTXLauncher/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe",
				IsStaging = true
			});
		}
	}

	private async Task AddReleasesAsync(List<UpdateSource> updateSources, bool forceRefresh)
	{
		var releases = await _gitHubService.FetchReleasesAsync(_owner, _repo, forceRefresh);
		
		// Filter out pre-releases, but use them if no stable releases are available
		var stableReleases = releases.Where(r => !r.Prerelease).ToList();
		var releasesToUse = stableReleases.Count > 0 ? stableReleases : releases;

		foreach (var release in releasesToUse.OrderByDescending(r => r.PublishedAt))
		{
			// Look for appropriate download asset
			string downloadUrl = GetReleaseDownloadUrl(release);

			updateSources.Add(new UpdateSource
			{
				Name = $"Version {release.TagName}",
				Version = release.TagName,
				DownloadUrl = downloadUrl,
				IsStaging = false,
				Release = release
			});
		}
	}

	private string GetReleaseDownloadUrl(GitHubRelease release)
	{
		// First try to find a direct executable
		var exeAsset = release.Assets.FirstOrDefault(a =>
			a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
			a.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));

		if (exeAsset != null)
		{
			return exeAsset.BrowserDownloadUrl;
		}

		// Then try to find a zip containing both executables
		var zipAsset = release.Assets.FirstOrDefault(a =>
			a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
			a.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));

		if (zipAsset != null)
		{
			return zipAsset.BrowserDownloadUrl;
		}

		// Fallback to source zipball
		return release.ZipballUrl;
	}

	private async Task<string> DownloadFileAsync(string url, string downloadFolder, 
		IProgress<UpdateProgress>? progress = null)
	{
		using var client = new HttpClient();
		client.DefaultRequestHeaders.Add("User-Agent", "RTXLauncherUpdater");
		client.Timeout = TimeSpan.FromMinutes(5);

		// Determine file name and path
		bool isExe = url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
		string fileName = isExe ? Path.GetFileName(url) : "update.zip";
		if (string.IsNullOrEmpty(fileName) || fileName == "update.zip")
		{
			fileName = isExe ? "RTXLauncher.exe" : "update.zip";
		}

		string downloadPath = Path.Combine(downloadFolder, fileName);

		try
		{
			using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();

			long totalSize = response.Content.Headers.ContentLength ?? 1000000;
			progress?.Report(new UpdateProgress { 
				Message = $"Downloading {totalSize / 1024} KB...", 
				PercentComplete = 20 
			});

			using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
			using var downloadStream = await response.Content.ReadAsStreamAsync();

			byte[] buffer = new byte[8192];
			long totalBytesRead = 0;
			int bytesRead;

			while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				await fileStream.WriteAsync(buffer, 0, bytesRead);
				totalBytesRead += bytesRead;
				
				int progressPercentage = (int)((totalBytesRead * 100) / totalSize);
				int overallProgress = 20 + (int)(progressPercentage * 0.6); // 20% to 80%
				
				progress?.Report(new UpdateProgress { 
					Message = $"Downloading... {progressPercentage}%", 
					PercentComplete = overallProgress 
				});
			}

			progress?.Report(new UpdateProgress { 
				Message = "Download completed", 
				PercentComplete = 90 
			});

			return downloadPath;
		}
		catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
		{
			// Fallback URL for staging builds
			string fallbackUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/RTXLauncher/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe";
			progress?.Report(new UpdateProgress { 
				Message = "Access denied, trying fallback URL...", 
				PercentComplete = 15 
			});
			
			return await DownloadFileAsync(fallbackUrl, downloadFolder, progress);
		}
	}
}