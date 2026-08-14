using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.Formats.Tar;
using System.IO.Compression;

namespace RTXLauncher.Core.Tests.Services;

public sealed class UpdateServiceTests : IDisposable
{
	private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherUpdaterTests_{Guid.NewGuid():N}");

	public UpdateServiceTests()
	{
		Directory.CreateDirectory(_testDirectory);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
			Directory.Delete(_testDirectory, recursive: true);
	}

	[Fact]
	public async Task GetAvailableUpdateSources_IncludesNightlyAndStablePlatformAsset()
	{
		var nightly = CreateRelease(
			"nightly",
			"abc1234 - Nightly",
			prerelease: true,
			new GitHubAsset
			{
				Name = "RTXLauncher-Linux-nightly.tar.gz",
				BrowserDownloadUrl = "https://example.test/RTXLauncher-Linux-nightly.tar.gz"
			});
		var stable = CreateRelease(
			"v1.4.0",
			"v1.4.0",
			prerelease: false,
			new GitHubAsset
			{
				Name = "RTXLauncher.Avalonia.Linux",
				BrowserDownloadUrl = "https://example.test/RTXLauncher.Avalonia.Linux"
			});

		var service = CreateService(new FakeUpdateFeed(new[] { nightly, stable }));

		var sources = await service.GetAvailableUpdateSourcesAsync();

		Assert.Equal(2, sources.Count);
		Assert.Equal("Nightly Build", sources[0].Name);
		Assert.True(sources[0].IsStaging);
		Assert.Equal("https://example.test/RTXLauncher-Linux-nightly.tar.gz", sources[0].DownloadUrl);
		Assert.Equal("v1.4.0", sources[1].Version);
		Assert.False(sources[1].IsStaging);
		Assert.Equal("https://example.test/RTXLauncher.Avalonia.Linux", sources[1].DownloadUrl);
	}

	[Fact]
	public async Task CheckForUpdates_StableInstallationUsesStableChannel()
	{
		var nightly = CreateRelease(
			"nightly",
			"newbuild - Nightly",
			prerelease: true,
			new GitHubAsset
			{
				Name = "RTXLauncher-Linux-nightly.tar.gz",
				BrowserDownloadUrl = "https://example.test/nightly.tar.gz"
			});
		var stable = CreateRelease(
			"v1.4.0",
			"v1.4.0",
			prerelease: false,
			new GitHubAsset
			{
				Name = "RTXLauncher.Avalonia.Linux",
				BrowserDownloadUrl = "https://example.test/stable"
			});
		var service = CreateService(
			new FakeUpdateFeed(new[] { nightly, stable }),
			currentVersion: "v1.0.0");

		var result = await service.CheckForUpdatesAsync();

		Assert.Null(result.Error);
		Assert.True(result.UpdateAvailable);
		Assert.NotNull(result.LatestUpdate);
		Assert.Equal("v1.4.0", result.LatestUpdate!.Version);
	}

	[Fact]
	public async Task CheckForUpdates_NightlyInstallationUsesNightlyChannel()
	{
		var nightly = CreateRelease(
			"nightly",
			"newbuild - Nightly",
			prerelease: true,
			new GitHubAsset
			{
				Name = "RTXLauncher-Linux-nightly.tar.gz",
				BrowserDownloadUrl = "https://example.test/nightly.tar.gz"
			});
		var stable = CreateRelease(
			"v1.4.0",
			"v1.4.0",
			prerelease: false,
			new GitHubAsset
			{
				Name = "RTXLauncher.Avalonia.Linux",
				BrowserDownloadUrl = "https://example.test/stable"
			});
		var service = CreateService(
			new FakeUpdateFeed(new[] { nightly, stable }),
			currentVersion: "dev-oldbuild");

		var result = await service.CheckForUpdatesAsync();

		Assert.True(result.UpdateAvailable);
		Assert.NotNull(result.LatestUpdate);
		Assert.True(result.LatestUpdate!.IsStaging);
	}

	[Fact]
	public async Task DownloadUpdate_ExtractsLinuxNightlyTarGz()
	{
		const string url = "https://example.test/RTXLauncher-Linux-nightly.tar.gz";
		string archive = CreateTarGz("RTXLauncher.Avalonia", "nightly-binary");
		var source = new UpdateSource
		{
			Name = "Nightly Build",
			Version = "dev-abc1234",
			DownloadUrl = url,
			IsStaging = true
		};
		var service = CreateService(
			new FakeUpdateFeed(Array.Empty<GitHubRelease>()),
			new FakeDownloadService(new Dictionary<string, byte[]> { [url] = File.ReadAllBytes(archive) }));

		string preparedFolder = await service.DownloadUpdateAsync(source);

		Assert.Equal("nightly-binary", await File.ReadAllTextAsync(Path.Combine(preparedFolder, "RTXLauncher.Avalonia")));
		Assert.DoesNotContain(Directory.EnumerateFiles(preparedFolder), path => path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task DownloadUpdate_NormalizesStableLinuxExecutableAssetName()
	{
		const string url = "https://example.test/RTXLauncher.Avalonia.Linux";
		var source = new UpdateSource
		{
			Name = "Version v1.4.0",
			Version = "v1.4.0",
			DownloadUrl = url
		};
		var service = CreateService(
			new FakeUpdateFeed(Array.Empty<GitHubRelease>()),
			new FakeDownloadService(new Dictionary<string, byte[]> { [url] = "stable-binary"u8.ToArray() }));

		string preparedFolder = await service.DownloadUpdateAsync(source);

		Assert.Equal("stable-binary", await File.ReadAllTextAsync(Path.Combine(preparedFolder, "RTXLauncher.Avalonia")));
		Assert.DoesNotContain(Directory.EnumerateFiles(preparedFolder), path => path.EndsWith(".Linux", StringComparison.OrdinalIgnoreCase));
	}

	private UpdateService CreateService(
		IUpdateFeed feed,
		IFileDownloadService? downloader = null,
		string currentVersion = "v1.0.0")
	{
		return new UpdateService(
			feed,
			downloader,
			currentVersionProvider: () => currentVersion,
			updateTempPath: _testDirectory,
			targetExecutableName: "RTXLauncher.Avalonia");
	}

	private string CreateTarGz(string fileName, string content)
	{
		string sourceDirectory = Path.Combine(_testDirectory, $"source-{Guid.NewGuid():N}");
		Directory.CreateDirectory(sourceDirectory);
		File.WriteAllText(Path.Combine(sourceDirectory, fileName), content);

		string tarPath = Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.tar");
		TarFile.CreateFromDirectory(sourceDirectory, tarPath, includeBaseDirectory: false);
		string archivePath = Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.tar.gz");
		using (var output = File.Create(archivePath))
		using (var gzip = new GZipStream(output, CompressionMode.Compress))
		using (var tar = File.OpenRead(tarPath))
			tar.CopyTo(gzip);

		return archivePath;
	}

	private static GitHubRelease CreateRelease(string tag, string name, bool prerelease, GitHubAsset asset) => new()
	{
		TagName = tag,
		Name = name,
		Prerelease = prerelease,
		PublishedAt = DateTime.UtcNow,
		Assets = new List<GitHubAsset> { asset }
	};

	private sealed class FakeUpdateFeed : IUpdateFeed
	{
		private readonly List<GitHubRelease> _releases;

		public FakeUpdateFeed(IEnumerable<GitHubRelease> releases)
		{
			_releases = releases.ToList();
		}

		public Task<List<GitHubRelease>> FetchReleasesAsync(string owner, string repo, bool forceRefresh = false) =>
			Task.FromResult(_releases);

		public Task<GitHubArtifactInfo> FetchLatestStagingArtifact(string owner, string repo, bool forceRefresh = false) =>
			Task.FromException<GitHubArtifactInfo>(new InvalidOperationException("No compatibility artifact expected."));
	}

	private sealed class FakeDownloadService : IFileDownloadService
	{
		private readonly IReadOnlyDictionary<string, byte[]> _payloads;

		public FakeDownloadService(IReadOnlyDictionary<string, byte[]> payloads)
		{
			_payloads = payloads;
		}

		public async Task<DownloadResult> DownloadFileAsync(
			string url,
			string destinationPath,
			DownloadOptions? options = null,
			IProgress<EnhancedDownloadProgress>? progress = null,
			CancellationToken cancellationToken = default)
		{
			if (!_payloads.TryGetValue(url, out var payload))
				throw new InvalidOperationException($"No fake payload configured for {url}");

			await File.WriteAllBytesAsync(destinationPath, payload, cancellationToken);
			return new DownloadResult { Success = true, FilePath = destinationPath, BytesDownloaded = payload.Length };
		}
	}
}
