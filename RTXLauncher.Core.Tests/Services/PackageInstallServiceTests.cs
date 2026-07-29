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
}
