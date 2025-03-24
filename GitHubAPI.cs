using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTXLauncher
{

	public class GitHubRelease
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("tag_name")]
		public string TagName { get; set; }

		[JsonPropertyName("html_url")]
		public string HtmlUrl { get; set; }

		[JsonPropertyName("zipball_url")]
		public string ZipballUrl { get; set; }

		[JsonPropertyName("tarball_url")]
		public string TarballUrl { get; set; }

		[JsonPropertyName("published_at")]
		public DateTime PublishedAt { get; set; }

		[JsonPropertyName("body")]
		public string Body { get; set; }

		[JsonPropertyName("prerelease")]
		public bool Prerelease { get; set; }

		[JsonPropertyName("assets")]
		public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name) && Name != TagName)
				return $"{Name} ({TagName})";
			else
				return TagName ?? "[Unnamed Release]";
		}
	}

	public class GitHubAsset
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("browser_download_url")]
		public string BrowserDownloadUrl { get; set; }

		[JsonPropertyName("size")]
		public long Size { get; set; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; set; }
	}

	public static class GitHubAPI
	{
		public static async Task<List<GitHubRelease>> FetchReleasesAsync(string owner, string repo)
		{
			using (HttpClient client = new HttpClient())
			{
				// Set user agent (GitHub API requires this)
				client.DefaultRequestHeaders.Add("User-Agent", "RTXRemixUpdater");

				try
				{
					// Get GitHub releases
					string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
					string json = await client.GetStringAsync(url);

					// Deserialize the JSON response
					return JsonSerializer.Deserialize<List<GitHubRelease>>(json);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error fetching GitHub releases: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return new List<GitHubRelease>();
				}
			}
		}
	}
}
