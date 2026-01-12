using System.Text.Json.Serialization;

namespace RTXLauncher.Core.Models;

/// <summary>
/// JSON serialization context for GitHub API models.
/// This ensures that all required types are preserved during trimming in AOT/standalone builds.
/// </summary>
[JsonSerializable(typeof(List<GitHubRelease>))]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubAsset))]
[JsonSerializable(typeof(GitHubRateLimit))]
[JsonSerializable(typeof(GitHubRateLimitResponse))]
[JsonSerializable(typeof(GitHubRateLimitResources))]
[JsonSerializable(typeof(GitHubActionsRunsResponse))]
[JsonSerializable(typeof(GitHubActionsRun))]
[JsonSerializable(typeof(GitHubActionsArtifactsResponse))]
[JsonSerializable(typeof(GitHubArtifactInfo))]
[JsonSerializable(typeof(List<GitHubActionsRun>))]
[JsonSerializable(typeof(List<GitHubArtifactInfo>))]
[JsonSerializable(typeof(List<InstalledModInfo>))]
[JsonSerializable(typeof(InstalledModInfo))]
[JsonSerializable(typeof(ModMetadata))]
[JsonSerializable(typeof(InstalledMod))]
[JsonSerializable(typeof(List<InstalledMod>))]
public partial class GitHubJsonContext : JsonSerializerContext
{
}
