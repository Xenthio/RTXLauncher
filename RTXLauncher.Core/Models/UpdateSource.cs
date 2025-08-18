// Models/UpdateSource.cs
namespace RTXLauncher.Core.Models;

public class UpdateSource
{
	public string Name { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public string DownloadUrl { get; set; } = string.Empty;
	public bool IsStaging { get; set; }
	public GitHubRelease? Release { get; set; }

	public override string ToString() => IsStaging ? Name : $"{Name} ({Version})";
}