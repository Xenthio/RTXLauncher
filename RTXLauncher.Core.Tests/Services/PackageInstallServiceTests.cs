using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using System.IO.Compression;

namespace RTXLauncher.Core.Tests.Services;

public class PackageInstallServiceTests : IDisposable
{
	private readonly string _testDirectory;

	public PackageInstallServiceTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherPackageTests_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, true);
		}
	}

	[Fact]
	public async Task InstallStandardFromLocalZipAsync_NormalizesFutureEntryTimestamp()
	{
		// Arrange
		var zipPath = Path.Combine(_testDirectory, "package.zip");
		var installDirectory = Path.Combine(_testDirectory, "install");
		const string relativePath = "garrysmod/addons/remixbinary/lua/test.lua";
		const string expectedContents = "return true";

		using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
		{
			var entry = zip.CreateEntry(relativePath);
			entry.LastWriteTime = DateTimeOffset.Now.AddHours(5);

			using var writer = new StreamWriter(entry.Open());
			writer.Write(expectedContents);
		}

		var service = new PackageInstallService();
		var installStartedUtc = DateTime.UtcNow;

		// Act
		await service.InstallStandardFromLocalZipAsync(
			zipPath,
			installDirectory,
			string.Empty,
			new Progress<InstallProgressReport>());

		var installFinishedUtc = DateTime.UtcNow;
		var installedPath = Path.Combine(
			installDirectory,
			relativePath.Replace('/', Path.DirectorySeparatorChar));

		// Assert
		Assert.Equal(expectedContents, await File.ReadAllTextAsync(installedPath));
		Assert.InRange(
			File.GetLastWriteTimeUtc(installedPath),
			installStartedUtc,
			installFinishedUtc);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task LocalPackagesInstallStartupLoaderInEitherOrder(bool fixesFirst)
	{
		if (!OperatingSystem.IsWindows()) return;
		var install = Path.Combine(_testDirectory, "install");
		var binary = Path.Combine(install, "bin", "win64");
		Directory.CreateDirectory(binary);
		Directory.CreateDirectory(Path.Combine(install, "garrysmod"));
		File.WriteAllText(Path.Combine(binary, "gmod.exe"), "fixture");
		const string renderer = "renderer-fixture";
		var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(renderer)));
		var fixesZip = Path.Combine(_testDirectory, "fixes.zip");
		var remixZip = Path.Combine(_testDirectory, "remix.zip");
		using (var zip = ZipFile.Open(fixesZip, ZipArchiveMode.Create))
		{
			using (var writer = new StreamWriter(zip.CreateEntry("bin/win64/astra-startup/d3d9.dll").Open())) writer.Write("proxy-fixture");
			using (var writer = new StreamWriter(zip.CreateEntry("bin/win64/astra-startup/proxy_renderer_exports.json").Open()))
				writer.Write(System.Text.Json.JsonSerializer.Serialize(new { version = 1, renderer_sha256 = hash, renderer_bytes = renderer.Length, original_name = "d3d9_astra_renderer.dll" }));
			// Default ignore rules must still protect the real renderer while allowing the staged proxy.
			using (var writer = new StreamWriter(zip.CreateEntry("bin/win64/d3d9.dll").Open())) writer.Write("must-be-ignored");
		}
		using (var zip = ZipFile.Open(remixZip, ZipArchiveMode.Create))
		using (var writer = new StreamWriter(zip.CreateEntry("d3d9.dll").Open())) writer.Write(renderer);
		var service = new PackageInstallService();
		var progress = new Progress<InstallProgressReport>();
		async Task Fixes() => await service.InstallStandardFromLocalZipAsync(fixesZip, install, PackageInstallService.DefaultIgnorePatterns, progress);
		async Task Remix() => await service.InstallRemixFromLocalZipAsync(remixZip, install, progress);
		if (fixesFirst) { await Fixes(); await Remix(); }
		else { await Remix(); await Fixes(); }
		Assert.Equal("proxy-fixture", File.ReadAllText(Path.Combine(binary, "d3d9.dll")));
		Assert.Equal(renderer, File.ReadAllText(Path.Combine(binary, "d3d9_astra_renderer.dll")));
		await Remix(); // Reinstalling the renderer must not discard startup support.
		Assert.Equal("proxy-fixture", File.ReadAllText(Path.Combine(binary, "d3d9.dll")));
		Assert.Equal(renderer, File.ReadAllText(Path.Combine(binary, "d3d9_astra_renderer.dll")));
	}
}
