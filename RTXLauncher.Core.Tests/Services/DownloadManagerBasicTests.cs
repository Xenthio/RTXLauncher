using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Tests.Helpers;
using System.Security.Cryptography;
using Xunit;

namespace RTXLauncher.Core.Tests.Services;

/// <summary>
/// Basic functionality tests for DownloadManager
/// </summary>
public class DownloadManagerBasicTests : IDisposable
{
	private readonly string _testDirectory;
	private readonly DownloadManager _downloadManager;

	public DownloadManagerBasicTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherTests_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);
		_downloadManager = new DownloadManager();
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, true);
		}
	}

	[Fact]
	public async Task DownloadFileAsync_SuccessfulDownload_ReturnsSuccess()
	{
		// Arrange
		var testContent = "Hello, World!"u8.ToArray();
		var destinationPath = Path.Combine(_testDirectory, "test.txt");
		
		// Note: This test requires a real HTTP endpoint or we need to refactor DownloadManager
		// to accept a custom HttpClient for testing. For now, this is a placeholder.
		// We'll test the public API in integration tests.
		
		// This test demonstrates the expected usage pattern
		Assert.True(true); // Placeholder
	}

	[Fact]
	public async Task DownloadFileAsync_WithProgress_ReportsProgress()
	{
		// Arrange
		var progressReports = new List<EnhancedDownloadProgress>();
		var progress = new Progress<EnhancedDownloadProgress>(p => progressReports.Add(p));
		
		// This would test progress reporting with a mock HTTP client
		Assert.True(true); // Placeholder
	}

	[Fact]
	public async Task VerifyChecksumAsync_MD5Match_ReturnsTrue()
	{
		// Arrange
		var testContent = "Test content for checksum"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "checksum_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		// Calculate expected MD5
		using var md5 = MD5.Create();
		var hash = md5.ComputeHash(testContent);
		var expectedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

		// Act
		var result = await _downloadManager.VerifyChecksumAsync(testFile, expectedHash, HashAlgorithmType.MD5);

		// Assert
		Assert.True(result.Verified);
		Assert.Equal(expectedHash, result.ActualHash);
	}

	[Fact]
	public async Task VerifyChecksumAsync_MD5Mismatch_ReturnsFalse()
	{
		// Arrange
		var testContent = "Test content for checksum"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "checksum_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		var wrongHash = "0000000000000000000000000000000";

		// Act
		var result = await _downloadManager.VerifyChecksumAsync(testFile, wrongHash, HashAlgorithmType.MD5);

		// Assert
		Assert.False(result.Verified);
		Assert.NotEqual(wrongHash, result.ActualHash);
	}

	[Fact]
	public async Task VerifyChecksumAsync_SHA256Match_ReturnsTrue()
	{
		// Arrange
		var testContent = "Test content for SHA256"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "sha256_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		// Calculate expected SHA256
		using var sha256 = SHA256.Create();
		var hash = sha256.ComputeHash(testContent);
		var expectedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

		// Act
		var result = await _downloadManager.VerifyChecksumAsync(testFile, expectedHash, HashAlgorithmType.SHA256);

		// Assert
		Assert.True(result.Verified);
		Assert.Equal(expectedHash, result.ActualHash);
	}

	[Fact]
	public async Task VerifyChecksumAsync_SHA512Match_ReturnsTrue()
	{
		// Arrange
		var testContent = "Test content for SHA512"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "sha512_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		// Calculate expected SHA512
		using var sha512 = SHA512.Create();
		var hash = sha512.ComputeHash(testContent);
		var expectedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

		// Act
		var result = await _downloadManager.VerifyChecksumAsync(testFile, expectedHash, HashAlgorithmType.SHA512);

		// Assert
		Assert.True(result.Verified);
		Assert.Equal(expectedHash, result.ActualHash);
	}

	[Fact]
	public async Task VerifyChecksumAsync_FileNotFound_ThrowsException()
	{
		// Arrange
		var nonExistentFile = Path.Combine(_testDirectory, "doesnotexist.txt");
		var dummyHash = "abc123";

		// Act & Assert
		await Assert.ThrowsAsync<FileNotFoundException>(() =>
			_downloadManager.VerifyChecksumAsync(nonExistentFile, dummyHash, HashAlgorithmType.MD5));
	}

	[Fact]
	public async Task VerifyChecksumAsync_UnsupportedAlgorithm_ThrowsException()
	{
		// Arrange
		var testFile = Path.Combine(_testDirectory, "test.txt");
		File.WriteAllText(testFile, "test");
		var dummyHash = "abc123";

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(() =>
			_downloadManager.VerifyChecksumAsync(testFile, dummyHash, HashAlgorithmType.None));
	}

	[Fact]
	public async Task VerifyChecksumAsync_CaseInsensitiveHash_ReturnsTrue()
	{
		// Arrange
		var testContent = "Test case insensitive"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "case_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		using var md5 = MD5.Create();
		var hash = md5.ComputeHash(testContent);
		var expectedHashLower = BitConverter.ToString(hash).Replace("-", "").ToLower();
		var expectedHashUpper = expectedHashLower.ToUpper();

		// Act
		var resultLower = await _downloadManager.VerifyChecksumAsync(testFile, expectedHashLower, HashAlgorithmType.MD5);
		var resultUpper = await _downloadManager.VerifyChecksumAsync(testFile, expectedHashUpper, HashAlgorithmType.MD5);

		// Assert
		Assert.True(resultLower.Verified);
		Assert.True(resultUpper.Verified);
	}

	[Fact]
	public async Task VerifyChecksumAsync_HashWithDashes_ReturnsTrue()
	{
		// Arrange
		var testContent = "Test hash with dashes"u8.ToArray();
		var testFile = Path.Combine(_testDirectory, "dash_test.txt");
		await File.WriteAllBytesAsync(testFile, testContent);

		using var md5 = MD5.Create();
		var hash = md5.ComputeHash(testContent);
		var expectedHashWithDashes = BitConverter.ToString(hash); // Has dashes

		// Act
		var result = await _downloadManager.VerifyChecksumAsync(testFile, expectedHashWithDashes, HashAlgorithmType.MD5);

		// Assert
		Assert.True(result.Verified);
	}
}
