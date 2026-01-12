namespace RTXLauncher.Core.Models;

/// <summary>
/// Global configuration for download operations
/// </summary>
public class DownloadConfiguration
{
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static DownloadConfiguration Default { get; } = new();

	/// <summary>
	/// Default maximum number of retry attempts (default: 5)
	/// </summary>
	public int DefaultMaxRetries { get; set; } = 5;

	/// <summary>
	/// Default timeout in minutes (default: 5)
	/// </summary>
	public int DefaultTimeoutMinutes { get; set; } = 5;

	/// <summary>
	/// Initial backoff delay in seconds for retry logic (default: 1)
	/// </summary>
	public int InitialBackoffSeconds { get; set; } = 1;

	/// <summary>
	/// Maximum backoff delay in seconds (default: 30)
	/// </summary>
	public int MaxBackoffSeconds { get; set; } = 30;

	/// <summary>
	/// Default buffer size for stream operations in bytes (default: 8192)
	/// </summary>
	public int DefaultBufferSize { get; set; } = 8192;

	/// <summary>
	/// Minimum bytes required before attempting to resume a download (default: 1MB)
	/// </summary>
	public long ResumeThresholdBytes { get; set; } = 1024 * 1024; // 1MB

	/// <summary>
	/// Whether resume is enabled by default (default: true)
	/// </summary>
	public bool AllowResumeByDefault { get; set; } = true;

	/// <summary>
	/// Default user agent string for HTTP requests
	/// </summary>
	public string DefaultUserAgent { get; set; } = "RTXLauncher-DownloadManager/1.0";

	/// <summary>
	/// How often to update progress reporting in milliseconds (default: 100ms)
	/// </summary>
	public int ProgressUpdateIntervalMs { get; set; } = 100;

	/// <summary>
	/// Number of samples to use for speed calculation (default: 10)
	/// </summary>
	public int SpeedCalculationSamples { get; set; } = 10;

	/// <summary>
	/// Whether to automatically verify checksums if provided (default: true)
	/// </summary>
	public bool AutoVerifyChecksums { get; set; } = true;

	/// <summary>
	/// Whether to clean up partial files on successful download (default: true)
	/// </summary>
	public bool CleanupPartialFiles { get; set; } = true;

	/// <summary>
	/// Maximum age of partial files in hours before they are considered stale (default: 24)
	/// </summary>
	public int PartialFileMaxAgeHours { get; set; } = 24;
}
