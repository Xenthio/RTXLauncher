using RTXLauncher.Core.Models;
using Xunit;

namespace RTXLauncher.Core.Tests.Models;

/// <summary>
/// Tests for download-related models
/// </summary>
public class DownloadModelsTests
{
	[Fact]
	public void DownloadOptions_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var options = new DownloadOptions();

		// Assert
		Assert.Equal(5, options.MaxRetries);
		Assert.Equal(5, options.TimeoutMinutes);
		Assert.True(options.AllowResume);
		Assert.Equal(8192, options.BufferSize);
		Assert.Equal(1024 * 1024, options.ResumeThresholdBytes);
		Assert.Equal(1, options.InitialBackoffSeconds);
		Assert.Equal(30, options.MaxBackoffSeconds);
		Assert.Equal(HashAlgorithmType.None, options.HashAlgorithm);
		Assert.Null(options.ExpectedHash);
		Assert.Null(options.UserAgent);
		Assert.Null(options.CustomHeaders);
	}

	[Fact]
	public void DownloadOptions_CustomValues_AreSet()
	{
		// Arrange & Act
		var options = new DownloadOptions
		{
			MaxRetries = 10,
			TimeoutMinutes = 15,
			AllowResume = false,
			ExpectedHash = "abc123",
			HashAlgorithm = HashAlgorithmType.SHA256,
			UserAgent = "TestAgent",
			CustomHeaders = new Dictionary<string, string> { { "X-Custom", "Value" } }
		};

		// Assert
		Assert.Equal(10, options.MaxRetries);
		Assert.Equal(15, options.TimeoutMinutes);
		Assert.False(options.AllowResume);
		Assert.Equal("abc123", options.ExpectedHash);
		Assert.Equal(HashAlgorithmType.SHA256, options.HashAlgorithm);
		Assert.Equal("TestAgent", options.UserAgent);
		Assert.NotNull(options.CustomHeaders);
		Assert.Equal("Value", options.CustomHeaders["X-Custom"]);
	}

	[Fact]
	public void DownloadResult_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var result = new DownloadResult();

		// Assert
		Assert.False(result.Success);
		Assert.Equal(string.Empty, result.FilePath);
		Assert.Equal(0, result.BytesDownloaded);
		Assert.Null(result.ActualHash);
		Assert.Null(result.HashVerified);
		Assert.Equal(0, result.RetryAttempts);
		Assert.False(result.WasResumed);
		Assert.Equal(TimeSpan.Zero, result.Duration);
		Assert.Null(result.ErrorMessage);
		Assert.Null(result.Exception);
	}

	[Fact]
	public void EnhancedDownloadProgress_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var progress = new EnhancedDownloadProgress();

		// Assert
		Assert.Equal(string.Empty, progress.Message);
		Assert.Equal(0, progress.PercentComplete);
		Assert.Equal(0, progress.BytesDownloaded);
		Assert.Equal(0, progress.TotalBytes);
		Assert.Equal(0, progress.BytesPerSecond);
		Assert.Null(progress.EstimatedTimeRemaining);
		Assert.Equal(0, progress.RetryAttempt);
		Assert.False(progress.IsResuming);
		Assert.Equal(DownloadPhase.Initializing, progress.Phase);
		Assert.False(progress.IsComplete);
		Assert.Null(progress.Error);
	}

	[Fact]
	public void DownloadConfiguration_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var config = new DownloadConfiguration();

		// Assert
		Assert.Equal(5, config.DefaultMaxRetries);
		Assert.Equal(5, config.DefaultTimeoutMinutes);
		Assert.Equal(1, config.InitialBackoffSeconds);
		Assert.Equal(30, config.MaxBackoffSeconds);
		Assert.Equal(8192, config.DefaultBufferSize);
		Assert.Equal(1024 * 1024, config.ResumeThresholdBytes);
		Assert.True(config.AllowResumeByDefault);
		Assert.Equal("RTXLauncher-DownloadManager/1.0", config.DefaultUserAgent);
		Assert.Equal(100, config.ProgressUpdateIntervalMs);
		Assert.Equal(10, config.SpeedCalculationSamples);
		Assert.True(config.AutoVerifyChecksums);
		Assert.True(config.CleanupPartialFiles);
		Assert.Equal(24, config.PartialFileMaxAgeHours);
	}

	[Fact]
	public void DownloadConfiguration_DefaultSingleton_IsNotNull()
	{
		// Arrange & Act
		var config = DownloadConfiguration.Default;

		// Assert
		Assert.NotNull(config);
		Assert.Equal(5, config.DefaultMaxRetries);
	}

	[Theory]
	[InlineData(DownloadPhase.Initializing)]
	[InlineData(DownloadPhase.Connecting)]
	[InlineData(DownloadPhase.Downloading)]
	[InlineData(DownloadPhase.Verifying)]
	[InlineData(DownloadPhase.Complete)]
	[InlineData(DownloadPhase.Failed)]
	[InlineData(DownloadPhase.Retrying)]
	public void DownloadPhase_AllValues_AreValid(DownloadPhase phase)
	{
		// Arrange & Act
		var progress = new EnhancedDownloadProgress { Phase = phase };

		// Assert
		Assert.Equal(phase, progress.Phase);
	}

	[Theory]
	[InlineData(HashAlgorithmType.None)]
	[InlineData(HashAlgorithmType.MD5)]
	[InlineData(HashAlgorithmType.SHA256)]
	[InlineData(HashAlgorithmType.SHA512)]
	public void HashAlgorithmType_AllValues_AreValid(HashAlgorithmType algorithm)
	{
		// Arrange & Act
		var options = new DownloadOptions { HashAlgorithm = algorithm };

		// Assert
		Assert.Equal(algorithm, options.HashAlgorithm);
	}
}
