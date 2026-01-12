using System;

namespace RTXLauncher.Core.Models;

/// <summary>
/// Hash algorithm types supported for checksum verification
/// </summary>
public enum HashAlgorithmType
{
	None,
	MD5,
	SHA256,
	SHA512
}

/// <summary>
/// Options for configuring download behavior
/// </summary>
public class DownloadOptions
{
	/// <summary>
	/// Maximum number of retry attempts on failure (default: 5)
	/// </summary>
	public int MaxRetries { get; set; } = 5;

	/// <summary>
	/// Timeout in minutes for the entire download operation (default: 5)
	/// </summary>
	public int TimeoutMinutes { get; set; } = 5;

	/// <summary>
	/// Whether to allow resuming partial downloads (default: true)
	/// </summary>
	public bool AllowResume { get; set; } = true;

	/// <summary>
	/// Expected hash value for verification (optional)
	/// </summary>
	public string? ExpectedHash { get; set; }

	/// <summary>
	/// Hash algorithm to use for verification (default: None)
	/// </summary>
	public HashAlgorithmType HashAlgorithm { get; set; } = HashAlgorithmType.None;

	/// <summary>
	/// Buffer size for reading/writing streams in bytes (default: 8192)
	/// </summary>
	public int BufferSize { get; set; } = 8192;

	/// <summary>
	/// Minimum bytes that must be downloaded before resume is attempted (default: 1MB)
	/// </summary>
	public long ResumeThresholdBytes { get; set; } = 1024 * 1024; // 1MB

	/// <summary>
	/// Initial backoff delay in seconds (default: 1)
	/// </summary>
	public int InitialBackoffSeconds { get; set; } = 1;

	/// <summary>
	/// Maximum backoff delay in seconds (default: 30)
	/// </summary>
	public int MaxBackoffSeconds { get; set; } = 30;

	/// <summary>
	/// Custom user agent string (optional)
	/// </summary>
	public string? UserAgent { get; set; }

	/// <summary>
	/// Additional HTTP headers to include in the request
	/// </summary>
	public Dictionary<string, string>? CustomHeaders { get; set; }
}

/// <summary>
/// Result of a download operation
/// </summary>
public class DownloadResult
{
	/// <summary>
	/// Whether the download completed successfully
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Path to the downloaded file
	/// </summary>
	public string FilePath { get; set; } = string.Empty;

	/// <summary>
	/// Total bytes downloaded
	/// </summary>
	public long BytesDownloaded { get; set; }

	/// <summary>
	/// Actual hash of the downloaded file (if computed)
	/// </summary>
	public string? ActualHash { get; set; }

	/// <summary>
	/// Whether the hash verification passed (if applicable)
	/// </summary>
	public bool? HashVerified { get; set; }

	/// <summary>
	/// Number of retry attempts made
	/// </summary>
	public int RetryAttempts { get; set; }

	/// <summary>
	/// Whether the download was resumed from a partial file
	/// </summary>
	public bool WasResumed { get; set; }

	/// <summary>
	/// Total time taken for the download
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// Error message if the download failed
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// Exception that caused the failure (if any)
	/// </summary>
	public Exception? Exception { get; set; }
}

/// <summary>
/// Enhanced progress information for download operations
/// </summary>
public class EnhancedDownloadProgress
{
	/// <summary>
	/// Current operation message
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Overall progress percentage (0-100)
	/// </summary>
	public int PercentComplete { get; set; }

	/// <summary>
	/// Bytes downloaded so far
	/// </summary>
	public long BytesDownloaded { get; set; }

	/// <summary>
	/// Total bytes to download (if known)
	/// </summary>
	public long TotalBytes { get; set; }

	/// <summary>
	/// Current download speed in bytes per second
	/// </summary>
	public double BytesPerSecond { get; set; }

	/// <summary>
	/// Estimated time remaining
	/// </summary>
	public TimeSpan? EstimatedTimeRemaining { get; set; }

	/// <summary>
	/// Current retry attempt (0 if not retrying)
	/// </summary>
	public int RetryAttempt { get; set; }

	/// <summary>
	/// Whether this is a resumed download
	/// </summary>
	public bool IsResuming { get; set; }

	/// <summary>
	/// Current operation phase (Connecting, Downloading, Verifying, etc.)
	/// </summary>
	public DownloadPhase Phase { get; set; } = DownloadPhase.Initializing;

	/// <summary>
	/// Whether the download is complete
	/// </summary>
	public bool IsComplete { get; set; }

	/// <summary>
	/// Error information if something went wrong
	/// </summary>
	public Exception? Error { get; set; }
}

/// <summary>
/// Phases of a download operation
/// </summary>
public enum DownloadPhase
{
	Initializing,
	Connecting,
	Downloading,
	Verifying,
	Complete,
	Failed,
	Retrying
}
