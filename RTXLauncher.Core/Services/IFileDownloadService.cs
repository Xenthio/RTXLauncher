using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Downloads a file for an update. This abstraction keeps the updater's archive
/// preparation tests independent from HTTP and GitHub.
/// </summary>
public interface IFileDownloadService
{
	Task<DownloadResult> DownloadFileAsync(
		string url,
		string destinationPath,
		DownloadOptions? options = null,
		IProgress<EnhancedDownloadProgress>? progress = null,
		CancellationToken cancellationToken = default);
}
