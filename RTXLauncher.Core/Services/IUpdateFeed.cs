using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Provides update metadata. Keeping this separate from the GitHub implementation
/// lets update selection and installation be tested without making network requests.
/// </summary>
public interface IUpdateFeed
{
	Task<List<GitHubRelease>> FetchReleasesAsync(string owner, string repo, bool forceRefresh = false);
	Task<GitHubArtifactInfo> FetchLatestStagingArtifact(string owner, string repo, bool forceRefresh = false);
}
