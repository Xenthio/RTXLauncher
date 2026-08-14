using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Progress information for update operations.
/// </summary>
public class UpdateProgress
{
	public string Message { get; set; } = string.Empty;
	public int PercentComplete { get; set; }
	public bool IsComplete { get; set; }
	public Exception? Error { get; set; }
}

/// <summary>
/// Result of checking for updates.
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
/// Shared updater for the Avalonia and WinForms launchers.
/// </summary>
public class UpdateService
{
	private readonly IUpdateFeed _updateFeed;
	private readonly IFileDownloadService _downloadManager;
	private readonly LauncherUpdateInstaller _installer;
	private readonly string _owner = "Xenthio";
	private readonly string _repo = "RTXLauncher";
	private readonly string _updateTempPath;
	private readonly Func<string> _currentVersionProvider;
	private readonly Func<string?> _currentExecutablePathProvider;
	private readonly string? _targetExecutableName;

	public UpdateService(GitHubService gitHubService, DownloadManager? downloadManager = null)
		: this((IUpdateFeed)gitHubService, (IFileDownloadService?)downloadManager)
	{
	}

	public UpdateService(
		IUpdateFeed updateFeed,
		IFileDownloadService? downloadManager = null,
		Func<string>? currentVersionProvider = null,
		string? updateTempPath = null,
		string? targetExecutableName = null,
		LauncherUpdateInstaller? installer = null,
		Func<string?>? currentExecutablePathProvider = null)
	{
		_updateFeed = updateFeed ?? throw new ArgumentNullException(nameof(updateFeed));
		_downloadManager = downloadManager ?? new DownloadManager();
		_currentVersionProvider = currentVersionProvider ?? VersionUtility.GetCurrentAssemblyVersion;
		_updateTempPath = updateTempPath ?? Path.Combine(Path.GetTempPath(), "RTXLauncherUpdater");
		_targetExecutableName = targetExecutableName;
		_installer = installer ?? new LauncherUpdateInstaller();
		_currentExecutablePathProvider = currentExecutablePathProvider ?? GetCurrentExecutablePath;
	}

	/// <summary>
	/// Gets the current application version.
	/// </summary>
	public string GetCurrentVersion() => _currentVersionProvider();

	/// <summary>
	/// Checks the stable channel for stable builds and the nightly channel for
	/// development builds. Nightly is selectable, but does not replace a stable
	/// update notification for a stable installation.
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
			var latestStable = result.AvailableUpdates
				.Where(source => !source.IsStaging)
				.OrderByDescending(source => source.Release?.PublishedAt ?? DateTime.MinValue)
				.FirstOrDefault();
			var latestNightly = result.AvailableUpdates
				.Where(source => source.IsStaging)
				.OrderByDescending(source => source.Release?.PublishedAt ?? DateTime.MinValue)
				.FirstOrDefault();

			result.LatestUpdate = result.CurrentVersion.StartsWith("dev-", StringComparison.OrdinalIgnoreCase)
				? latestNightly ?? latestStable
				: latestStable ?? latestNightly;

			if (result.LatestUpdate != null)
			{
				result.UpdateAvailable = result.CurrentVersion.StartsWith("dev-", StringComparison.OrdinalIgnoreCase) &&
					latestNightly != null
					? !string.Equals(latestNightly.Version, result.CurrentVersion, StringComparison.OrdinalIgnoreCase)
					: VersionUtility.CompareVersions(result.LatestUpdate.Version, result.CurrentVersion) > 0;
			}
		}
		catch (Exception ex)
		{
			result.Error = ex;
		}

		return result;
	}

	/// <summary>
	/// Returns both stable releases and the current nightly release. The nightly
	/// workflow publishes a GitHub prerelease, so it must not be filtered out when
	/// stable releases are present.
	/// </summary>
	public async Task<List<UpdateSource>> GetAvailableUpdateSourcesAsync(bool forceRefresh = false)
	{
		var updateSources = new List<UpdateSource>();
		List<GitHubRelease> releases = new();

		try
		{
			releases = await _updateFeed.FetchReleasesAsync(_owner, _repo, forceRefresh);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error getting GitHub releases: {ex.Message}");
		}

		var nightlyRelease = releases
			.Where(IsNightlyRelease)
			.OrderByDescending(release => release.PublishedAt)
			.FirstOrDefault();

		if (nightlyRelease != null)
		{
			var nightlyUrl = GetReleaseDownloadUrl(nightlyRelease);
			if (!string.IsNullOrEmpty(nightlyUrl))
			{
				updateSources.Add(new UpdateSource
				{
					Name = "Nightly Build",
					Version = $"dev-{GetNightlyIdentifier(nightlyRelease)}",
					DownloadUrl = nightlyUrl,
					IsStaging = true,
					Release = nightlyRelease
				});
			}
		}
		else
		{
			// Keep the Actions artifact path as a compatibility fallback for older
			// repositories that did not publish a nightly release yet.
			try
			{
				var artifact = await _updateFeed.FetchLatestStagingArtifact(_owner, _repo, forceRefresh);
				if (!string.IsNullOrWhiteSpace(artifact.ArchiveDownloadUrl))
				{
					string identifier = artifact.Name.StartsWith("RTXLauncher-", StringComparison.OrdinalIgnoreCase)
						? artifact.Name["RTXLauncher-".Length..]
						: "latest";

					updateSources.Add(new UpdateSource
					{
						Name = "Nightly Build",
						Version = $"dev-{identifier}",
						DownloadUrl = artifact.ArchiveDownloadUrl,
						IsStaging = true
					});
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error getting Actions nightly artifact: {ex.Message}");
			}
		}

		foreach (var release in releases
			.Where(release => !release.Prerelease && !IsNightlyRelease(release))
			.OrderByDescending(release => release.PublishedAt))
		{
			var downloadUrl = GetReleaseDownloadUrl(release);
			if (string.IsNullOrEmpty(downloadUrl))
				continue;

			updateSources.Add(new UpdateSource
			{
				Name = $"Version {release.TagName}",
				Version = release.TagName,
				DownloadUrl = downloadUrl,
				IsStaging = false,
				Release = release
			});
		}

		return updateSources;
	}

	/// <summary>
	/// Downloads and prepares an update. The returned folder contains the launcher
	/// executable under the name used by the current platform.
	/// </summary>
	public async Task<string> DownloadUpdateAsync(
		UpdateSource updateSource,
		IProgress<UpdateProgress>? progress = null)
	{
		if (updateSource == null)
			throw new ArgumentNullException(nameof(updateSource));
		if (string.IsNullOrWhiteSpace(updateSource.DownloadUrl))
			throw new ArgumentException("The update source has no download URL.", nameof(updateSource));

		Directory.CreateDirectory(_updateTempPath);
		progress?.Report(new UpdateProgress { Message = "Preparing download...", PercentComplete = 0 });

		string versionTag = MakeSafePathPart(updateSource.IsStaging ? "nightly" : updateSource.Version);
		string downloadFolder = Path.Combine(_updateTempPath, $"RTXLauncher_Update_{versionTag}");
		if (Directory.Exists(downloadFolder))
			Directory.Delete(downloadFolder, recursive: true);
		Directory.CreateDirectory(downloadFolder);

		try
		{
			progress?.Report(new UpdateProgress { Message = "Starting download...", PercentComplete = 10 });
			string downloadPath = await DownloadFileAsync(updateSource.DownloadUrl, downloadFolder, progress);
			string targetExecutableName = GetTargetExecutableName();

			if (IsZip(downloadPath))
			{
				progress?.Report(new UpdateProgress { Message = "Extracting ZIP package...", PercentComplete = 75 });
				string extractPath = Path.Combine(downloadFolder, "extracted");
				Directory.CreateDirectory(extractPath);
				ZipFile.ExtractToDirectory(downloadPath, extractPath, overwriteFiles: true);
				PrepareExtractedPackage(downloadFolder, extractPath, targetExecutableName);
			}
			else if (IsTarGz(downloadPath))
			{
				progress?.Report(new UpdateProgress { Message = "Extracting TAR.GZ package...", PercentComplete = 75 });
				string extractPath = Path.Combine(downloadFolder, "extracted");
				Directory.CreateDirectory(extractPath);
				using var archiveStream = File.OpenRead(downloadPath);
				using var gzipStream = new GZipStream(archiveStream, CompressionMode.Decompress);
				TarFile.ExtractToDirectory(gzipStream, extractPath, overwriteFiles: true);
				PrepareExtractedPackage(downloadFolder, extractPath, targetExecutableName);
			}
			else
			{
				// Stable Linux releases are published as a direct executable without
				// an extension. Normalize platform-suffixed asset names here.
				string targetPath = Path.Combine(downloadFolder, targetExecutableName);
				if (!string.Equals(downloadPath, targetPath, StringComparison.OrdinalIgnoreCase))
				{
					File.Copy(downloadPath, targetPath, overwrite: true);
					File.Delete(downloadPath);
				}
			}

			string preparedExecutable = Path.Combine(downloadFolder, targetExecutableName);
			if (!File.Exists(preparedExecutable))
				throw new FileNotFoundException($"The update package does not contain {targetExecutableName}.", preparedExecutable);

			if (!OperatingSystem.IsWindows())
			{
				File.SetUnixFileMode(
					preparedExecutable,
					UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
					UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
					UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
			}

			progress?.Report(new UpdateProgress { Message = "Update ready to install", PercentComplete = 100, IsComplete = true });
			return downloadFolder;
		}
		catch (Exception ex)
		{
			progress?.Report(new UpdateProgress { Message = $"Download failed: {ex.Message}", Error = ex });
			throw;
		}
	}

	/// <summary>
	/// Starts a helper process that waits for this launcher to exit, copies the
	/// prepared files over the current installation, and starts the new launcher.
	/// </summary>
	public Task InstallUpdateAsync(
		string preparedUpdateFolder,
		IProgress<UpdateProgress>? progress = null,
		bool restart = true)
	{
		string? currentExecutablePath = _currentExecutablePathProvider();
		if (string.IsNullOrWhiteSpace(currentExecutablePath))
			throw new InvalidOperationException("Could not determine the current launcher executable path.");

		if (!restart)
		{
			_installer.ApplyUpdate(preparedUpdateFolder, currentExecutablePath);
			progress?.Report(new UpdateProgress { Message = "Update installed", PercentComplete = 100, IsComplete = true });
			return Task.CompletedTask;
		}

		progress?.Report(new UpdateProgress { Message = "Launching updater...", PercentComplete = 100 });
		string scriptPath = _installer.CreateUpdaterScript(preparedUpdateFolder, currentExecutablePath);
		_installer.Launch(scriptPath);
		Environment.Exit(0);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Determines the executable name for the current launcher and operating system.
	/// </summary>
	public string GetTargetExecutableName()
	{
		if (!string.IsNullOrWhiteSpace(_targetExecutableName))
			return _targetExecutableName;

		string suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
		var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
		if (assemblyName.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
			return $"RTXLauncher.Avalonia{suffix}";
		if (assemblyName.Contains("WinForms", StringComparison.OrdinalIgnoreCase))
			return $"RTXLauncher.WinForms{suffix}";

		string currentProcessName = Process.GetCurrentProcess().ProcessName;
		if (currentProcessName.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
			return $"RTXLauncher.Avalonia{suffix}";
		if (currentProcessName.Contains("WinForms", StringComparison.OrdinalIgnoreCase))
			return $"RTXLauncher.WinForms{suffix}";

		return $"RTXLauncher{suffix}";
	}

	private void PrepareExtractedPackage(string downloadFolder, string extractPath, string targetExecutableName)
	{
		string? extractedExecutable = FindExecutableInDirectory(extractPath, targetExecutableName);
		if (string.IsNullOrEmpty(extractedExecutable))
			throw new FileNotFoundException($"The update package does not contain {targetExecutableName}.");

		string packageRoot = Path.GetDirectoryName(extractedExecutable)!;
		CopyDirectoryContents(packageRoot, downloadFolder);
		Directory.Delete(extractPath, recursive: true);

		string archivePath = Directory.EnumerateFiles(downloadFolder)
			.FirstOrDefault(path => IsZip(path) || IsTarGz(path)) ?? string.Empty;
		if (!string.IsNullOrEmpty(archivePath))
			File.Delete(archivePath);

		string preparedExecutable = Path.Combine(downloadFolder, targetExecutableName);
		if (!File.Exists(preparedExecutable) && !string.Equals(extractedExecutable, preparedExecutable, StringComparison.OrdinalIgnoreCase))
		{
			string copiedCandidate = Path.Combine(downloadFolder, Path.GetFileName(extractedExecutable));
			if (File.Exists(copiedCandidate))
			{
				File.Copy(copiedCandidate, preparedExecutable, overwrite: true);
				File.Delete(copiedCandidate);
			}
		}
	}

	private static void CopyDirectoryContents(string sourceDirectory, string destinationDirectory)
	{
		foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
			string destinationPath = Path.Combine(destinationDirectory, relativePath);
			string? destinationParent = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrEmpty(destinationParent))
				Directory.CreateDirectory(destinationParent);
			File.Copy(sourceFile, destinationPath, overwrite: true);
		}
	}

	private string FindExecutableInDirectory(string directory, string targetExecutableName)
	{
		string directMatch = Path.Combine(directory, targetExecutableName);
		if (File.Exists(directMatch))
			return directMatch;

		var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
		var exactNameMatch = files.FirstOrDefault(file =>
			string.Equals(Path.GetFileName(file), targetExecutableName, StringComparison.OrdinalIgnoreCase));
		if (exactNameMatch != null)
			return exactNameMatch;

		var launcherFiles = files.Where(IsLauncherExecutableCandidate).ToList();
		return launcherFiles.FirstOrDefault() ?? string.Empty;
	}

	private bool IsLauncherExecutableCandidate(string path)
	{
		string name = Path.GetFileName(path);
		if (!name.StartsWith("RTXLauncher", StringComparison.OrdinalIgnoreCase))
			return false;
		if (name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
			name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
			name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
			name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
			name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
			return false;

		return OperatingSystem.IsWindows()
			? name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
			: !name.Contains('.', StringComparison.Ordinal);
	}

	private string GetReleaseDownloadUrl(GitHubRelease release)
	{
		var assets = release.Assets ?? new List<GitHubAsset>();
		string platformToken = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : string.Empty;
		var platformAssets = string.IsNullOrEmpty(platformToken)
			? assets
			: assets.Where(asset => asset.Name.Contains(platformToken, StringComparison.OrdinalIgnoreCase)).ToList();
		string launcherFamily = GetTargetExecutableName().Contains("WinForms", StringComparison.OrdinalIgnoreCase)
			? "WinForms"
			: "Avalonia";
		var appSpecificAssets = platformAssets
			.Where(asset => asset.Name.Contains(launcherFamily, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (appSpecificAssets.Count > 0)
			platformAssets = appSpecificAssets;
		else if (launcherFamily.Equals("WinForms", StringComparison.Ordinal))
			return string.Empty;

		var directExecutable = platformAssets.FirstOrDefault(asset =>
			(asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
			asset.Name.Equals("RTXLauncher.Avalonia.Linux", StringComparison.OrdinalIgnoreCase) ||
			asset.Name.Equals("RTXLauncher.WinForms.Linux", StringComparison.OrdinalIgnoreCase)) &&
			asset.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));
		if (directExecutable != null)
			return directExecutable.BrowserDownloadUrl;

		var archive = platformAssets.FirstOrDefault(asset =>
			(IsZip(asset.Name) || IsTarGz(asset.Name)) &&
			asset.Name.Contains("RTXLauncher", StringComparison.OrdinalIgnoreCase));
		if (archive != null)
			return archive.BrowserDownloadUrl;

		return string.Empty;
	}

	private static bool IsNightlyRelease(GitHubRelease release) =>
		release.TagName.Equals("nightly", StringComparison.OrdinalIgnoreCase) ||
		(release.Prerelease && release.Name.Contains("nightly", StringComparison.OrdinalIgnoreCase));

	private static string GetNightlyIdentifier(GitHubRelease release)
	{
		string identifier = (release.Name ?? string.Empty)
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.FirstOrDefault() ?? string.Empty;
		return string.IsNullOrWhiteSpace(identifier) || identifier.Equals("nightly", StringComparison.OrdinalIgnoreCase)
			? "latest"
			: identifier;
	}

	private async Task<string> DownloadFileAsync(string url, string downloadFolder, IProgress<UpdateProgress>? progress)
	{
		string fileName = GetFileNameFromUrl(url);
		if (string.IsNullOrWhiteSpace(fileName))
			fileName = "update.bin";

		string downloadPath = Path.Combine(downloadFolder, fileName);
		var downloadOptions = new DownloadOptions
		{
			MaxRetries = 5,
			TimeoutMinutes = 10,
			AllowResume = true,
			UserAgent = "RTXLauncherUpdater"
		};

		var mappedProgress = progress == null ? null : new Progress<EnhancedDownloadProgress>(enhancedProgress =>
		{
			progress.Report(new UpdateProgress
			{
				Message = enhancedProgress.Message,
				PercentComplete = 10 + (int)(enhancedProgress.PercentComplete * 0.65),
				IsComplete = enhancedProgress.IsComplete,
				Error = enhancedProgress.Error
			});
		});

		var result = await _downloadManager.DownloadFileAsync(url, downloadPath, downloadOptions, mappedProgress);
		if (!result.Success)
			throw new InvalidOperationException($"Download failed: {result.ErrorMessage}", result.Exception);

		return downloadPath;
	}

	private static string GetFileNameFromUrl(string url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			return Path.GetFileName(uri.AbsolutePath);
		return Path.GetFileName(url.Split('?', '#')[0]);
	}

	private static bool IsZip(string path) => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

	private static bool IsTarGz(string path) =>
		path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
		path.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);

	private static string MakeSafePathPart(string value)
	{
		foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
			value = value.Replace(invalidCharacter, '_');
		return string.IsNullOrWhiteSpace(value) ? "latest" : value;
	}

	private static string? GetCurrentExecutablePath()
	{
		return Process.GetCurrentProcess().MainModule?.FileName ??
			System.Reflection.Assembly.GetEntryAssembly()?.Location;
	}
}
