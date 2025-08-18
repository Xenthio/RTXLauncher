// Models/GitHubModels.cs

using System.Text.Json.Serialization;

namespace RTXLauncher.Core.Models;

// All your existing data models (GitHubRelease, GitHubAsset, GitHubRateLimit, etc.)
// Go here. I will include GitHubRelease and GitHubAsset for brevity.

public class GitHubRelease
{
	[JsonPropertyName("name")] public string Name { get; set; }
	[JsonPropertyName("tag_name")] public string TagName { get; set; }
	[JsonPropertyName("html_url")] public string HtmlUrl { get; set; }
	[JsonPropertyName("zipball_url")] public string ZipballUrl { get; set; }
	[JsonPropertyName("tarball_url")] public string TarballUrl { get; set; }
	[JsonPropertyName("published_at")] public DateTime PublishedAt { get; set; }
	[JsonPropertyName("body")] public string Body { get; set; }
	[JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
	[JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();

	public override string ToString() => Name;
}

public class GitHubAsset
{
	[JsonPropertyName("name")] public string Name { get; set; }
	[JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; }
	[JsonPropertyName("size")] public long Size { get; set; }
	[JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
}


public class GitHubRateLimit
{
	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("remaining")]
	public int Remaining { get; set; }

	[JsonPropertyName("reset")]
	public long Reset { get; set; }

	[JsonPropertyName("used")]
	public int Used { get; set; }

	// Convert Unix timestamp to DateTime
	public DateTime ResetTime => DateTimeOffset.FromUnixTimeSeconds(Reset).DateTime.ToLocalTime();

	// Time until reset
	public TimeSpan TimeUntilReset => ResetTime - DateTime.Now;
}

public class GitHubRateLimitResponse
{
	[JsonPropertyName("resources")]
	public GitHubRateLimitResources Resources { get; set; }
}

public class GitHubRateLimitResources
{
	[JsonPropertyName("core")]
	public GitHubRateLimit Core { get; set; }
}

#region GitHub Actions classes
public class GitHubActionsRunsResponse
{
	[JsonPropertyName("workflow_runs")]
	public List<GitHubActionsRun> WorkflowRuns { get; set; }
}

public class GitHubActionsRun
{
	[JsonPropertyName("run_number")]
	public int RunNumber { get; set; }

	[JsonPropertyName("artifacts_url")]
	public string ArtifactsUrl { get; set; }
}

public class GitHubActionsArtifactsResponse
{
	[JsonPropertyName("artifacts")]
	public List<GitHubArtifactInfo> Artifacts { get; set; }
}

public class GitHubArtifactInfo
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("archive_download_url")]
	public string ArchiveDownloadUrl { get; set; }
}

#endregion