// Services/GitHubService.cs

using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RTXLauncher.Core.Services;

// A custom exception to report rate limit issues
public class GitHubRateLimitExceededException : Exception
{
	public TimeSpan ResetTime { get; }
	public GitHubRateLimitExceededException(TimeSpan resetTime)
		: base("GitHub API rate limit exceeded.")
	{
		ResetTime = resetTime;
	}
}

// A generic exception for API errors
public class ApiException : Exception
{
	public ApiException(string message, Exception innerException) : base(message, innerException) { }
}

public class GitHubService
{
	private readonly HttpClient _httpClient;
	private readonly string _cacheDirectory;
	private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(8);

	// TODO: manage this securely, perhaps in a settings service
	private string? _personalAccessToken = null;

	private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(8);

	public GitHubService()
	{
		// Set up cache directory and HttpClient
		_cacheDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"RTXLauncher",
			"GitHubCache");

		if (!Directory.Exists(_cacheDirectory))
		{
			Directory.CreateDirectory(_cacheDirectory);
		}

		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXRemixLauncher");
		_httpClient.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

		// You'd also load the access token here if you have one saved locally
	}

	/// <summary>
	/// Fetches releases from a GitHub repository.
	/// </summary>
	public async Task<List<GitHubRelease>> FetchReleasesAsync(string owner, string repo, bool forceRefresh = false)
	{
		string cacheFile = Path.Combine(_cacheDirectory, $"{owner}_{repo}_releases.json");

		// Caching logic remains mostly the same, but without MessageBoxes
		if (!forceRefresh && IsCacheValid(cacheFile))
		{
			try
			{
				var cachedJson = await File.ReadAllTextAsync(cacheFile);
				return JsonSerializer.Deserialize(cachedJson, GitHubJsonContext.Default.ListGitHubRelease)!;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Cache error: {ex.Message}");
			}
		}

		// Add token to headers if available
		AddAuthToken();

		try
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/releases";
			var response = await _httpClient.GetAsync(url);

			// Check for rate limit and throw specific exception
			if (response.StatusCode == HttpStatusCode.Forbidden && IsRateLimited(response.Headers))
			{
				throw new GitHubRateLimitExceededException(GetRateLimitResetTime(response.Headers));
			}

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			// Cache the response
			SaveCache(cacheFile, json);

			return JsonSerializer.Deserialize(json, GitHubJsonContext.Default.ListGitHubRelease)!;
		}
		catch (Exception ex)
		{
			// If the request fails, try to load from an old cache if available
			if (File.Exists(cacheFile))
			{
				try
				{
					var cachedJson = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize(cachedJson, GitHubJsonContext.Default.ListGitHubRelease)!;
				}
				catch { /* ignore */ }
			}
			// If we can't load from cache, rethrow the exception
			throw new ApiException($"Failed to fetch releases for {owner}/{repo}", ex);
		}
	}

	/// <summary>
	/// Fetches the latest staging artifact URL from GitHub Actions
	/// </summary>
	public async Task<GitHubArtifactInfo> FetchLatestStagingArtifact(string owner, string repo, bool forceRefresh = false)
	{
		string cacheFile = Path.Combine(_cacheDirectory, $"{owner}_{repo}_staging.json");

		// Check cache first
		if (!forceRefresh && IsCacheValid(cacheFile))
		{
			try
			{
				var cachedJson = await File.ReadAllTextAsync(cacheFile);
				return JsonSerializer.Deserialize(cachedJson, GitHubJsonContext.Default.GitHubArtifactInfo)!;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Cache error: {ex.Message}");
			}
		}

		// Add token to headers if available
		AddAuthToken();

		try
		{
			// Fetch workflow runs
			var runsUrl = $"https://api.github.com/repos/{owner}/{repo}/actions/runs?status=success&event=push&per_page=10";
			var runsResponse = await _httpClient.GetAsync(runsUrl);

			if (runsResponse.StatusCode == HttpStatusCode.Forbidden && IsRateLimited(runsResponse.Headers))
			{
				throw new GitHubRateLimitExceededException(GetRateLimitResetTime(runsResponse.Headers));
			}

			runsResponse.EnsureSuccessStatusCode();

			var runsJson = await runsResponse.Content.ReadAsStringAsync();
			var runs = JsonSerializer.Deserialize(runsJson, GitHubJsonContext.Default.GitHubActionsRunsResponse);

			if (runs?.WorkflowRuns?.Count > 0)
			{
				// Get the latest successful run
				var latestRun = runs.WorkflowRuns.First();

				// Fetch artifacts for this run
				var artifactsUrl = latestRun.ArtifactsUrl;
				var artifactsResponse = await _httpClient.GetAsync(artifactsUrl);
				artifactsResponse.EnsureSuccessStatusCode();

				var artifactsJson = await artifactsResponse.Content.ReadAsStringAsync();
				var artifacts = JsonSerializer.Deserialize(artifactsJson, GitHubJsonContext.Default.GitHubActionsArtifactsResponse);

				// Find RTXLauncher artifact
				var rtxArtifact = artifacts?.Artifacts?.FirstOrDefault(a => 
					a.Name.StartsWith("RTXLauncher", StringComparison.OrdinalIgnoreCase));

				if (rtxArtifact != null)
				{
					// Cache the result
					var resultJson = JsonSerializer.Serialize(rtxArtifact, GitHubJsonContext.Default.GitHubArtifactInfo);
					SaveCache(cacheFile, resultJson);

					return rtxArtifact;
				}
			}

			// Fallback artifact
			var fallback = new GitHubArtifactInfo 
			{ 
				Name = "RTXLauncher-latest", 
				ArchiveDownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/RTXLauncher/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe"
			};

			return fallback;
		}
		catch (Exception ex)
		{
			// Try to load from cache if available
			if (File.Exists(cacheFile))
			{
				try
				{
					var cachedJson = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize(cachedJson, GitHubJsonContext.Default.GitHubArtifactInfo)!;
				}
				catch { /* ignore */ }
			}

			// Return fallback
			return new GitHubArtifactInfo 
			{ 
				Name = "RTXLauncher-latest", 
				ArchiveDownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/RTXLauncher/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe"
			};
		}
	}

	// --- Private Helper Methods ---

	private void AddAuthToken()
	{
		if (!string.IsNullOrEmpty(_personalAccessToken))
		{
			_httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("token", _personalAccessToken);
		}
	}

	private static bool IsCacheValid(string cacheFile)
	{
		try
		{
			if (!File.Exists(cacheFile) || !File.Exists(cacheFile + ".timestamp"))
				return false;

			string timestampStr = File.ReadAllText(cacheFile + ".timestamp");
			DateTime timestamp = DateTime.Parse(timestampStr);

			// Check if cache has expired
			return (DateTime.Now - timestamp) < DefaultCacheExpiration;
		}
		catch
		{
			return false;
		}
	}

	private void SaveCache(string cacheFile, string json)
	{
		try
		{
			File.WriteAllText(cacheFile, json);
			File.WriteAllText(cacheFile + ".timestamp", DateTime.Now.ToString());
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to save cache: {ex.Message}");
		}
	}

	/// <summary>
	/// Determines if a response indicates we've been rate limited
	/// </summary>
	private static bool IsRateLimited(System.Net.Http.Headers.HttpResponseHeaders headers)
	{
		if (headers.TryGetValues("X-RateLimit-Remaining", out var values))
		{
			string remaining = values.FirstOrDefault();
			return remaining == "0";
		}
		return false;
	}

	/// <summary>
	/// Gets the time until the rate limit resets from response headers
	/// </summary>
	private static TimeSpan GetRateLimitResetTime(System.Net.Http.Headers.HttpResponseHeaders headers)
	{
		if (headers.TryGetValues("X-RateLimit-Reset", out var values))
		{
			string resetTimestamp = values.FirstOrDefault();
			if (long.TryParse(resetTimestamp, out long timestamp))
			{
				var resetTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
				return resetTime - DateTime.Now;
			}
		}
		return TimeSpan.FromMinutes(60); // Default to 1 hour if we can't determine
	}
}