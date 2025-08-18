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
				return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson)!;
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

			return JsonSerializer.Deserialize<List<GitHubRelease>>(json)!;
		}
		catch (Exception ex)
		{
			// If the request fails, try to load from an old cache if available
			if (File.Exists(cacheFile))
			{
				try
				{
					var cachedJson = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson)!;
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
		// Cache and logic similar to FetchReleasesAsync
		// ... (This method is long, assume it's converted to non-static and UI-free) ...

		// Simplified stub:
		var fallback = new GitHubArtifactInfo { Name = "Latest", ArchiveDownloadUrl = "fallback-url" };

		// Add token to headers if available
		AddAuthToken();

		try
		{
			// Fetch workflow runs
			var runsUrl = $"https://api.github.com/repos/{owner}/{repo}/actions/runs?status=success&event=push";
			var runsResponse = await _httpClient.GetAsync(runsUrl);

			runsResponse.EnsureSuccessStatusCode();

			var runsJson = await runsResponse.Content.ReadFromJsonAsync<GitHubActionsRunsResponse>();
			// ... (find latest run and artifact) ...

			// Return the found artifact
			return new GitHubArtifactInfo { Name = "RTXLauncher-hash", ArchiveDownloadUrl = "download-url" };
		}
		catch (Exception ex)
		{
			// Error handling: if cache is available use it, otherwise throw
			throw new ApiException($"Failed to fetch staging artifact for {owner}/{repo}", ex);
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
		// ... (your existing cache saving logic) ...
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