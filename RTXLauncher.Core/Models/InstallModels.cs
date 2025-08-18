// Place these in a common folder like 'Models' or 'Services'
namespace RTXLauncher.Core.Models;

public class InstallProgressReport
{
	public string Message { get; init; } = string.Empty;
	public int Percentage { get; init; }
}

public class SymlinkFailedException : Exception
{
	public string TargetPath { get; }
	public SymlinkFailedException(string message, string targetPath) : base(message)
	{
		TargetPath = targetPath;
	}
	public SymlinkFailedException(string message, string targetPath, Exception inner) : base(message, inner)
	{
		TargetPath = targetPath;
	}
}

public class DownloadProgressReport : InstallProgressReport
{
	public long BytesDownloaded { get; init; }
	public long TotalBytes { get; init; }
}