using System.Net;
using System.Net.Http.Json;
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

	public static class GitHubAPI
	{
		private static string _cacheDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"RTXLauncher",
			"GitHubCache");

		// Cache releases for 8 minutes by default
		private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(8);

		// Cache for Personal Access Token (if provided)
		private static string _personalAccessToken = null;

		// Track when we last checked the rate limit
		private static DateTime _lastRateLimitCheck = DateTime.MinValue;
		private static GitHubRateLimit _currentRateLimit = null;

		// Initialize cache directory
		static GitHubAPI()
		{
			try
			{
				if (!Directory.Exists(_cacheDirectory))
					Directory.CreateDirectory(_cacheDirectory);

				// Try to load PAT from local settings
				LoadPersonalAccessToken();
			}
			catch { /* Ignore errors during static initialization */ }
		}

		/// <summary>
		/// Set a personal access token to increase the rate limit
		/// </summary>
		public static void SetPersonalAccessToken(string token)
		{
			_personalAccessToken = token;
			SavePersonalAccessToken();
		}

		/// <summary>
		/// Save the personal access token to a local file
		/// </summary>
		private static void SavePersonalAccessToken()
		{
			try
			{
				string tokenFile = Path.Combine(_cacheDirectory, "github_token.dat");
				if (!string.IsNullOrEmpty(_personalAccessToken))
					File.WriteAllText(tokenFile, _personalAccessToken);
				else if (File.Exists(tokenFile))
					File.Delete(tokenFile);
			}
			catch { /* Ignore errors during token saving */ }
		}

		/// <summary>
		/// Load the personal access token from a local file
		/// </summary>
		private static void LoadPersonalAccessToken()
		{
			try
			{
				string tokenFile = Path.Combine(_cacheDirectory, "github_token.dat");
				if (File.Exists(tokenFile))
					_personalAccessToken = File.ReadAllText(tokenFile);
			}
			catch { /* Ignore errors during token loading */ }
		}

		/// <summary>
		/// Creates a properly configured HttpClient with GitHub headers
		/// </summary>
		private static HttpClient CreateHttpClient()
		{
			HttpClient client = new HttpClient();

			// Set user agent (GitHub API requires this)
			client.DefaultRequestHeaders.Add("User-Agent", "RTXRemixUpdater");

			// Add authentication token if available
			if (!string.IsNullOrEmpty(_personalAccessToken))
			{
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("token", _personalAccessToken);
			}

			// Add header to get API v3
			client.DefaultRequestHeaders.Accept.Add(
				new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

			return client;
		}

		/// <summary>
		/// Fetches releases from GitHub with caching and rate limit handling
		/// </summary>
		public static async Task<List<GitHubRelease>> FetchReleasesAsync(string owner, string repo, bool forceRefresh = false)
		{
			string cacheFile = Path.Combine(_cacheDirectory, $"{owner}_{repo}_releases.json");
			bool useCache = !forceRefresh && IsCacheValid(cacheFile);

			if (useCache)
			{
				try
				{
					string cachedJson = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson);
				}
				catch (Exception ex)
				{
					// If there's an error with the cache, we'll fetch from the API
					Console.WriteLine($"Cache error: {ex.Message}");
					useCache = false;
				}
			}

			// Check if we need to wait for rate limit reset
			if (!useCache && !await EnsureRateLimitAvailableAsync())
			{
				// If rate limited and no cache, return empty list
				if (!File.Exists(cacheFile))
					return new List<GitHubRelease>();

				// If we have an old cache, use it despite being expired
				try
				{
					string cachedJson = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson);
				}
				catch
				{
					return new List<GitHubRelease>();
				}
			}

			using (HttpClient client = CreateHttpClient())
			{
				try
				{
					// Get GitHub releases
					string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
					HttpResponseMessage response = await client.GetAsync(url);

					// Update rate limit info from headers
					UpdateRateLimitFromHeaders(response.Headers);

					if (response.IsSuccessStatusCode)
					{
						string json = await response.Content.ReadAsStringAsync();

						// Save to cache
						try
						{
							await File.WriteAllTextAsync(cacheFile, json);
							File.WriteAllText(cacheFile + ".timestamp", DateTime.Now.ToString("o"));
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to save cache: {ex.Message}");
						}

						// Deserialize and return
						return JsonSerializer.Deserialize<List<GitHubRelease>>(json);
					}
					else if (response.StatusCode == HttpStatusCode.Forbidden && IsRateLimited(response.Headers))
					{
						// Handle rate limit specifically
						TimeSpan resetTime = GetRateLimitResetTime(response.Headers);
						string message = $"GitHub API rate limit exceeded. Resets in {(int)resetTime.TotalMinutes} minutes.";
						if (string.IsNullOrEmpty(_personalAccessToken))
							message += "\n\nConsider adding a GitHub Personal Access Token to increase the rate limit.";

						MessageBox.Show(message, "GitHub API Rate Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);

						// Try to use cached data if available
						if (File.Exists(cacheFile))
						{
							try
							{
								string cachedJson = await File.ReadAllTextAsync(cacheFile);
								return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson);
							}
							catch
							{
								return new List<GitHubRelease>();
							}
						}
						return new List<GitHubRelease>();
					}
					else
					{
						// Handle other errors
						string errorMsg = $"GitHub API error: {(int)response.StatusCode} {response.ReasonPhrase}";
						MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

						// Try to use cached data if available
						if (File.Exists(cacheFile))
						{
							try
							{
								string cachedJson = await File.ReadAllTextAsync(cacheFile);
								return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson);
							}
							catch
							{
								return new List<GitHubRelease>();
							}
						}
						return new List<GitHubRelease>();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error fetching GitHub releases: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					// Try to use cached data if available
					if (File.Exists(cacheFile))
					{
						try
						{
							string cachedJson = await File.ReadAllTextAsync(cacheFile);
							return JsonSerializer.Deserialize<List<GitHubRelease>>(cachedJson);
						}
						catch
						{
							return new List<GitHubRelease>();
						}
					}
					return new List<GitHubRelease>();
				}
			}
		}

		/// <summary>
		/// Fetches the latest staging artifact URL from GitHub Actions or returns a fallback URL.
		/// Uses the same caching approach as FetchReleasesAsync, but stores artifacts in a separate file.
		/// </summary>
		public static async Task<GitHubArtifactInfo> FetchLatestStagingArtifact(string owner, string repo, bool forceRefresh = false)
		{
			// Cache file name for staging artifacts
			string cacheKey = $"{owner}_{repo}_staging_artifact.json";
			string cacheFile = Path.Combine(_cacheDirectory, cacheKey);
			bool useCache = !forceRefresh && IsCacheValid(cacheFile);

			var fallback = new GitHubArtifactInfo()
			{
				Name = "Latest",
				ArchiveDownloadUrl = "https://github.com/Xenthio/RTXLauncher/raw/refs/heads/master/bin/Release/net8.0-windows/win-x64/publish/RTXLauncher.exe"
			};

			// Attempt to load from cache
			if (useCache)
			{
				try
				{
					var json = await File.ReadAllTextAsync(cacheFile);
					return JsonSerializer.Deserialize<GitHubArtifactInfo>(json);
				}
				catch
				{
					// If cache is corrupted, we'll fetch fresh data
					useCache = false;
				}
			}

			// Ensure rate limit
			if (!useCache && !await EnsureRateLimitAvailableAsync())
			{
				// If rate-limited, try returning stale cache
				if (File.Exists(cacheFile))
				{
					try
					{
						var json = await File.ReadAllTextAsync(cacheFile);
						return JsonSerializer.Deserialize<GitHubArtifactInfo>(json);
					}
					catch
					{
						// If that fails, return fallback
						return fallback;
					}
				}
				// No valid cache
				return fallback;
			}

			// Fetch runs
			using (HttpClient client = CreateHttpClient())
			{
				try
				{
					var runsUrl = $"https://api.github.com/repos/{owner}/{repo}/actions/runs?status=success&event=push";

					// Replace GetFromJsonAsync with a manual request so we can get response headers
					var runsResponse = await client.GetAsync(runsUrl);
					UpdateRateLimitFromHeaders(runsResponse.Headers);

					if (!runsResponse.IsSuccessStatusCode)
						return fallback;

					var runsJson = await runsResponse.Content.ReadFromJsonAsync<GitHubActionsRunsResponse>();
					var latestRun = runsJson?.WorkflowRuns?.OrderByDescending(r => r.RunNumber).FirstOrDefault();
					if (latestRun == null)
						return fallback;

					// Fetch artifacts
					var artifactsUrl = latestRun.ArtifactsUrl;
					var artifactsResponse = await client.GetAsync(artifactsUrl);
					UpdateRateLimitFromHeaders(artifactsResponse.Headers);

					if (!artifactsResponse.IsSuccessStatusCode)
						return fallback;

					var artifactsJson = await artifactsResponse.Content.ReadFromJsonAsync<GitHubActionsArtifactsResponse>();
					var artifact = artifactsJson?.Artifacts?.FirstOrDefault(a =>
						a.Name.StartsWith("RTXLauncher-", StringComparison.OrdinalIgnoreCase));

					var certainArtifact = artifact ?? fallback;

					// Save to cache
					try
					{
						var serialized = JsonSerializer.Serialize<GitHubArtifactInfo>(certainArtifact);
						await File.WriteAllTextAsync(cacheFile, serialized);
						File.WriteAllText(cacheFile + ".timestamp", DateTime.Now.ToString("o"));
					}
					catch { /* ignore cache write errors */ }

					return certainArtifact;
				}
				catch
				{
					// Return fallback on error
					return fallback;
				}
			}
		}

		/// <summary>
		/// Checks if the cache file is valid (exists and is not expired)
		/// </summary>
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

		/// <summary>
		/// Updates our cached rate limit info from response headers
		/// </summary>
		private static void UpdateRateLimitFromHeaders(System.Net.Http.Headers.HttpResponseHeaders headers)
		{
			try
			{
				var rateLimit = new GitHubRateLimit();

				if (headers.TryGetValues("X-RateLimit-Limit", out var limitValues))
					rateLimit.Limit = int.Parse(limitValues.FirstOrDefault() ?? "60");

				if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
					rateLimit.Remaining = int.Parse(remainingValues.FirstOrDefault() ?? "0");

				if (headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
					rateLimit.Reset = long.Parse(resetValues.FirstOrDefault() ?? "0");

				if (headers.TryGetValues("X-RateLimit-Used", out var usedValues))
					rateLimit.Used = int.Parse(usedValues.FirstOrDefault() ?? "0");

				_currentRateLimit = rateLimit;
				_lastRateLimitCheck = DateTime.Now;
			}
			catch { /* Ignore errors parsing rate limit headers */ }
		}

		/// <summary>
		/// Checks if we have rate limit capacity available, or if we need to wait
		/// </summary>
		private static async Task<bool> EnsureRateLimitAvailableAsync()
		{
			// If we have recent rate limit info and still have capacity
			if (_currentRateLimit != null &&
				(DateTime.Now - _lastRateLimitCheck) < TimeSpan.FromMinutes(5) &&
				_currentRateLimit.Remaining > 1)
			{
				return true;
			}

			// If we have rate limit info but are out of capacity, check if we need to wait
			if (_currentRateLimit != null && _currentRateLimit.Remaining <= 1)
			{
				TimeSpan waitTime = _currentRateLimit.TimeUntilReset;
				if (waitTime.TotalSeconds > 0)
				{
					bool waitForReset = false;

					// If wait time is short (less than 1 minute), just wait automatically
					if (waitTime.TotalMinutes < 1)
					{
						waitForReset = true;
					}
					else
					{
						// Otherwise ask the user if they want to wait
						var result = MessageBox.Show(
							$"GitHub API rate limit reached. Would you like to wait {(int)waitTime.TotalMinutes} minutes for it to reset?",
							"Rate Limit Reached",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question);

						waitForReset = (result == DialogResult.Yes);
					}

					if (waitForReset)
					{
						// Wait for the rate limit to reset
						await Task.Delay(waitTime);

						// Clear rate limit info so we'll check again
						_currentRateLimit = null;
						_lastRateLimitCheck = DateTime.MinValue;

						return true;
					}
					else
					{
						return false; // User chose not to wait
					}
				}
			}

			// Check current rate limit directly from GitHub
			try
			{
				using (HttpClient client = CreateHttpClient())
				{
					string url = "https://api.github.com/rate_limit";
					HttpResponseMessage response = await client.GetAsync(url);

					if (response.IsSuccessStatusCode)
					{
						string json = await response.Content.ReadAsStringAsync();
						var rateLimitResponse = JsonSerializer.Deserialize<GitHubRateLimitResponse>(json);

						_currentRateLimit = rateLimitResponse.Resources.Core;
						_lastRateLimitCheck = DateTime.Now;

						// If we have capacity, return true
						if (_currentRateLimit.Remaining > 1)
						{
							return true;
						}
						else
						{
							TimeSpan waitTime = _currentRateLimit.TimeUntilReset;
							if (waitTime.TotalSeconds > 0)
							{
								bool waitForReset = false;

								// If wait time is short, just wait automatically
								if (waitTime.TotalMinutes < 1)
								{
									waitForReset = true;
								}
								else
								{
									// Otherwise ask the user
									var result = MessageBox.Show(
										$"GitHub API rate limit reached. Would you like to wait {(int)waitTime.TotalMinutes} minutes for it to reset?",
										"Rate Limit Reached",
										MessageBoxButtons.YesNo,
										MessageBoxIcon.Question);

									waitForReset = (result == DialogResult.Yes);
								}

								if (waitForReset)
								{
									await Task.Delay(waitTime);
									_currentRateLimit = null;
									_lastRateLimitCheck = DateTime.MinValue;
									return true;
								}
								else
								{
									return false; // User chose not to wait
								}
							}
						}
					}
					else
					{
						// If we can't check rate limit, assume we have capacity
						return true;
					}
				}
			}
			catch
			{
				// If there's an error checking rate limit, assume we have capacity
				return true;
			}

			// Default to allowing the request
			return true;
		}
	}
}
