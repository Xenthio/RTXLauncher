using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Robust download manager with retry logic, resume capability, and checksum verification
/// </summary>
public class DownloadManager
{
	private readonly DownloadConfiguration _config;

	public DownloadManager(DownloadConfiguration? config = null)
	{
		_config = config ?? DownloadConfiguration.Default;
	}

	/// <summary>
	/// Downloads a file with robust error handling, retry logic, and optional resume support
	/// </summary>
	public async Task<DownloadResult> DownloadFileAsync(
		string url,
		string destinationPath,
		DownloadOptions? options = null,
		IProgress<EnhancedDownloadProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		options ??= new DownloadOptions();
		var result = new DownloadResult { FilePath = destinationPath };
		var startTime = DateTime.UtcNow;
		var partialFilePath = destinationPath + ".partial";

		try
		{
			ReportProgress(progress, new EnhancedDownloadProgress
			{
				Message = "Initializing download...",
				Phase = DownloadPhase.Initializing,
				PercentComplete = 0
			});

			// Attempt download with retries
			bool downloaded = false;
			Exception? lastException = null;

			for (int attempt = 0; attempt <= options.MaxRetries && !downloaded; attempt++)
			{
				result.RetryAttempts = attempt;

				try
				{
					if (attempt > 0)
					{
						// Calculate backoff delay
						var delay = CalculateBackoffDelay(attempt, options);
						
						ReportProgress(progress, new EnhancedDownloadProgress
						{
							Message = $"Retrying in {delay.TotalSeconds:F0} seconds... (Attempt {attempt}/{options.MaxRetries})",
							Phase = DownloadPhase.Retrying,
							RetryAttempt = attempt,
							PercentComplete = 0
						});

						await Task.Delay(delay, cancellationToken);
					}

					// Attempt the download
					await AttemptDownloadWithResumeAsync(
						url,
						destinationPath,
						partialFilePath,
						options,
						result,
						progress,
						cancellationToken);

					downloaded = true;
				}
				catch (Exception ex) when (ShouldRetry(ex, attempt, options.MaxRetries))
				{
					lastException = ex;
					Debug.WriteLine($"[DownloadManager] Attempt {attempt + 1} failed: {ex.Message}");
					
					ReportProgress(progress, new EnhancedDownloadProgress
					{
						Message = $"Download failed: {ex.Message}",
						Phase = DownloadPhase.Retrying,
						RetryAttempt = attempt,
						Error = ex
					});
				}
				catch (Exception ex)
				{
					// Non-retryable error
					throw;
				}
			}

			if (!downloaded)
			{
				throw new Exception($"Download failed after {options.MaxRetries} attempts", lastException);
			}

			// Verify checksum if provided
			if (!string.IsNullOrEmpty(options.ExpectedHash) && options.HashAlgorithm != HashAlgorithmType.None)
			{
				ReportProgress(progress, new EnhancedDownloadProgress
				{
					Message = "Verifying file integrity...",
					Phase = DownloadPhase.Verifying,
					PercentComplete = 95
				});

				var hashResult = await VerifyChecksumAsync(destinationPath, options.ExpectedHash, options.HashAlgorithm);
				result.ActualHash = hashResult.ActualHash;
				result.HashVerified = hashResult.Verified;

				if (!hashResult.Verified)
				{
					// Delete the corrupted file
					if (File.Exists(destinationPath))
					{
						File.Delete(destinationPath);
					}

					throw new Exception($"Checksum verification failed. Expected: {options.ExpectedHash}, Got: {hashResult.ActualHash}");
				}
			}

			// Cleanup partial file if it still exists
			if (_config.CleanupPartialFiles && File.Exists(partialFilePath))
			{
				File.Delete(partialFilePath);
			}

			result.Success = true;
			result.Duration = DateTime.UtcNow - startTime;

			ReportProgress(progress, new EnhancedDownloadProgress
			{
				Message = "Download complete!",
				Phase = DownloadPhase.Complete,
				PercentComplete = 100,
				IsComplete = true,
				BytesDownloaded = result.BytesDownloaded,
				TotalBytes = result.BytesDownloaded
			});
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = ex.Message;
			result.Exception = ex;
			result.Duration = DateTime.UtcNow - startTime;

			ReportProgress(progress, new EnhancedDownloadProgress
			{
				Message = $"Download failed: {ex.Message}",
				Phase = DownloadPhase.Failed,
				Error = ex,
				IsComplete = true
			});

			throw;
		}

		return result;
	}

	/// <summary>
	/// Attempts to download a file with resume support
	/// </summary>
	private async Task AttemptDownloadWithResumeAsync(
		string url,
		string destinationPath,
		string partialFilePath,
		DownloadOptions options,
		DownloadResult result,
		IProgress<EnhancedDownloadProgress>? progress,
		CancellationToken cancellationToken)
	{
		long startPosition = 0;
		bool isResuming = false;

		// Check if we can resume from a partial file
		if (options.AllowResume && File.Exists(partialFilePath))
		{
			var fileInfo = new FileInfo(partialFilePath);
			if (fileInfo.Length >= options.ResumeThresholdBytes)
			{
				// Check if partial file is not too old
				var fileAge = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
				if (fileAge.TotalHours < _config.PartialFileMaxAgeHours)
				{
					startPosition = fileInfo.Length;
					isResuming = true;
					result.WasResumed = true;

					Debug.WriteLine($"[DownloadManager] Resuming download from {startPosition} bytes");
				}
				else
				{
					Debug.WriteLine($"[DownloadManager] Partial file too old ({fileAge.TotalHours:F1} hours), starting fresh");
					File.Delete(partialFilePath);
				}
			}
		}

		ReportProgress(progress, new EnhancedDownloadProgress
		{
			Message = isResuming ? $"Resuming download from {FormatBytes(startPosition)}..." : "Connecting...",
			Phase = DownloadPhase.Connecting,
			IsResuming = isResuming,
			BytesDownloaded = startPosition
		});

		// Create a new HttpClient for this request with proper configuration
		using var httpClient = CreateConfiguredHttpClient(options);

		// Create request with range header if resuming
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (isResuming && startPosition > 0)
		{
			request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startPosition, null);
		}

		using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

		// Check for successful response
		if (isResuming && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
		{
			// Server doesn't support range requests or file is already complete
			Debug.WriteLine("[DownloadManager] Server doesn't support resume, starting fresh");
			startPosition = 0;
			isResuming = false;
			File.Delete(partialFilePath);
			
			// Retry without range header using the same httpClient
			using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
			using var retryResponse = await httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			retryResponse.EnsureSuccessStatusCode();
			
			await DownloadStreamAsync(retryResponse, destinationPath, partialFilePath, 0, options, result, progress, cancellationToken);
			return;
		}

		response.EnsureSuccessStatusCode();

		// Verify server supports resume if we're trying to resume
		if (isResuming && response.StatusCode != HttpStatusCode.PartialContent)
		{
			// Server doesn't support resume, start fresh
			Debug.WriteLine("[DownloadManager] Server returned full content, starting fresh");
			startPosition = 0;
			isResuming = false;
			File.Delete(partialFilePath);
		}

		await DownloadStreamAsync(response, destinationPath, partialFilePath, startPosition, options, result, progress, cancellationToken);
	}

	/// <summary>
	/// Downloads the response stream to a file with progress tracking
	/// </summary>
	private async Task DownloadStreamAsync(
		HttpResponseMessage response,
		string destinationPath,
		string partialFilePath,
		long startPosition,
		DownloadOptions options,
		DownloadResult result,
		IProgress<EnhancedDownloadProgress>? progress,
		CancellationToken cancellationToken)
	{
		long totalBytes = (response.Content.Headers.ContentLength ?? 0) + startPosition;
		long totalBytesRead = startPosition;

		using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
		
		// Open file in append mode if resuming, otherwise create new
		var fileMode = startPosition > 0 ? FileMode.Append : FileMode.Create;
		using var fileStream = new FileStream(partialFilePath, fileMode, FileAccess.Write, FileShare.None, options.BufferSize);

		var buffer = new byte[options.BufferSize];
		int bytesRead;
		
		// Progress tracking variables
		var speedSamples = new Queue<(DateTime Time, long Bytes)>();
		var lastProgressUpdate = DateTime.UtcNow;
		var progressUpdateInterval = TimeSpan.FromMilliseconds(_config.ProgressUpdateIntervalMs);

		ReportProgress(progress, new EnhancedDownloadProgress
		{
			Message = "Downloading...",
			Phase = DownloadPhase.Downloading,
			BytesDownloaded = totalBytesRead,
			TotalBytes = totalBytes,
			PercentComplete = totalBytes > 0 ? (int)((totalBytesRead * 100) / totalBytes) : 0
		});

		while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
		{
			await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
			totalBytesRead += bytesRead;

			// Update speed calculation
			var now = DateTime.UtcNow;
			speedSamples.Enqueue((now, bytesRead));

			// Keep only recent samples
			while (speedSamples.Count > _config.SpeedCalculationSamples)
			{
				speedSamples.Dequeue();
			}

			// Report progress at intervals
			if (now - lastProgressUpdate >= progressUpdateInterval)
			{
				lastProgressUpdate = now;

				// Calculate speed
				double bytesPerSecond = 0;
				if (speedSamples.Count >= 2)
				{
					var oldest = speedSamples.First();
					var totalSampleBytes = speedSamples.Sum(s => s.Bytes);
					var totalSampleTime = (now - oldest.Time).TotalSeconds;
					
					if (totalSampleTime > 0)
					{
						bytesPerSecond = totalSampleBytes / totalSampleTime;
					}
				}

				// Calculate ETA
				TimeSpan? eta = null;
				if (totalBytes > 0 && bytesPerSecond > 0)
				{
					var remainingBytes = totalBytes - totalBytesRead;
					eta = TimeSpan.FromSeconds(remainingBytes / bytesPerSecond);
				}

				var percentComplete = totalBytes > 0 ? (int)((totalBytesRead * 100) / totalBytes) : 0;

				ReportProgress(progress, new EnhancedDownloadProgress
				{
					Message = $"Downloading... {FormatBytes(totalBytesRead)} / {FormatBytes(totalBytes)} ({FormatSpeed(bytesPerSecond)})",
					Phase = DownloadPhase.Downloading,
					BytesDownloaded = totalBytesRead,
					TotalBytes = totalBytes,
					BytesPerSecond = bytesPerSecond,
					EstimatedTimeRemaining = eta,
					PercentComplete = percentComplete
				});
			}
		}

		result.BytesDownloaded = totalBytesRead;

		// Move partial file to final destination
		if (File.Exists(destinationPath))
		{
			File.Delete(destinationPath);
		}
		fileStream.Close();
		File.Move(partialFilePath, destinationPath);
	}

	/// <summary>
	/// Creates a configured HttpClient for download operations
	/// </summary>
	private HttpClient CreateConfiguredHttpClient(DownloadOptions options)
	{
		var httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes)
		};

		// Set user agent
		var userAgent = options.UserAgent ?? _config.DefaultUserAgent;
		httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

		// Add custom headers if provided
		if (options.CustomHeaders != null)
		{
			foreach (var header in options.CustomHeaders)
			{
				httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
			}
		}

		return httpClient;
	}

	/// <summary>
	/// Verifies the checksum of a downloaded file
	/// </summary>
	public async Task<(bool Verified, string ActualHash)> VerifyChecksumAsync(
		string filePath,
		string expectedHash,
		HashAlgorithmType algorithmType)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException("File not found for checksum verification", filePath);
		}

		HashAlgorithm? algorithm = algorithmType switch
		{
			HashAlgorithmType.MD5 => MD5.Create(),
			HashAlgorithmType.SHA256 => SHA256.Create(),
			HashAlgorithmType.SHA512 => SHA512.Create(),
			_ => throw new ArgumentException($"Unsupported hash algorithm: {algorithmType}")
		};

		using (algorithm)
		using (var stream = File.OpenRead(filePath))
		{
			var hashBytes = await algorithm.ComputeHashAsync(stream);
			var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
			var normalizedExpectedHash = expectedHash.Replace("-", "").ToLowerInvariant();

			return (actualHash == normalizedExpectedHash, actualHash);
		}
	}

	/// <summary>
	/// Calculates the backoff delay for retry attempts using exponential backoff
	/// </summary>
	private TimeSpan CalculateBackoffDelay(int attempt, DownloadOptions options)
	{
		var baseDelay = options.InitialBackoffSeconds;
		var maxDelay = options.MaxBackoffSeconds;

		// Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s (capped at maxDelay)
		var delaySeconds = Math.Min(baseDelay * Math.Pow(2, attempt - 1), maxDelay);
		
		// Add a small random jitter to prevent thundering herd
		var jitter = new Random().NextDouble() * 0.1 * delaySeconds;
		
		return TimeSpan.FromSeconds(delaySeconds + jitter);
	}

	/// <summary>
	/// Determines if an exception should trigger a retry
	/// </summary>
	private bool ShouldRetry(Exception ex, int currentAttempt, int maxRetries)
	{
		if (currentAttempt >= maxRetries)
		{
			return false;
		}

		// Network errors - always retry
		if (IsNetworkError(ex))
		{
			return true;
		}

		// HTTP errors
		if (ex is HttpRequestException httpEx)
		{
			var statusCode = GetStatusCode(httpEx);
			
			// Server errors (5xx) - retry
			if (statusCode.HasValue && (int)statusCode >= 500 && (int)statusCode < 600)
			{
				return true;
			}

			// Rate limiting - retry
			if (statusCode == HttpStatusCode.TooManyRequests)
			{
				return true;
			}

			// Client errors (4xx) - don't retry
			if (statusCode.HasValue && (int)statusCode >= 400 && (int)statusCode < 500)
			{
				return false;
			}
		}

		// Timeout errors - retry
		if (ex is TaskCanceledException or TimeoutException)
		{
			return true;
		}

		// Default: retry
		return true;
	}

	/// <summary>
	/// Checks if an exception is a network-related error
	/// </summary>
	private bool IsNetworkError(Exception ex)
	{
		var message = ex.Message.ToLower();
		return message.Contains("network") ||
		       message.Contains("connection") ||
		       message.Contains("timeout") ||
		       message.Contains("dns") ||
		       message.Contains("host") ||
		       message.Contains("unreachable") ||
		       ex is SocketException ||
		       ex.InnerException is SocketException;
	}

	/// <summary>
	/// Extracts HTTP status code from HttpRequestException
	/// </summary>
	private HttpStatusCode? GetStatusCode(HttpRequestException ex)
	{
		// Try to extract status code from exception
		if (ex.StatusCode.HasValue)
		{
			return ex.StatusCode;
		}

		// Try to parse from message
		var message = ex.Message;
		if (message.Contains("403")) return HttpStatusCode.Forbidden;
		if (message.Contains("404")) return HttpStatusCode.NotFound;
		if (message.Contains("429")) return HttpStatusCode.TooManyRequests;
		if (message.Contains("500")) return HttpStatusCode.InternalServerError;
		if (message.Contains("502")) return HttpStatusCode.BadGateway;
		if (message.Contains("503")) return HttpStatusCode.ServiceUnavailable;

		return null;
	}

	/// <summary>
	/// Reports progress to the provided progress reporter
	/// </summary>
	private void ReportProgress(IProgress<EnhancedDownloadProgress>? progress, EnhancedDownloadProgress progressData)
	{
		progress?.Report(progressData);
	}

	/// <summary>
	/// Formats bytes to human-readable string
	/// </summary>
	private string FormatBytes(long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		double len = bytes;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len /= 1024;
		}
		return $"{len:0.##} {sizes[order]}";
	}

	/// <summary>
	/// Formats download speed to human-readable string
	/// </summary>
	private string FormatSpeed(double bytesPerSecond)
	{
		return $"{FormatBytes((long)bytesPerSecond)}/s";
	}
}
