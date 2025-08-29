using HtmlAgilityPack;
using PuppeteerSharp;
using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RTXLauncher.Core.Services;

public class ModDBModService : IModService
{
	private readonly AddonInstallService _addonInstallService;
	private readonly InstalledModsService _installedModsService;
	private IBrowser? _browser;
	private IPage? _page; // Reusable page instance for performance
	private bool _isDisposed;

	// Use a full browser User-Agent string consistently.
	public readonly static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 (RTXLauncher/1.0)";

	public ModDBModService(AddonInstallService addonInstallService, InstalledModsService installedModsService)
	{
		_addonInstallService = addonInstallService;
		_installedModsService = installedModsService;
	}

	private async Task EnsureBrowserAsync()
	{
		if (_browser != null && _browser.IsConnected) return;
		Debug.WriteLine("[ModDBModService] Creating new shared browser instance...");
		var browserFetcher = new BrowserFetcher();
		await browserFetcher.DownloadAsync();
		_browser = await Puppeteer.LaunchAsync(new LaunchOptions
		{
			Headless = true,
			Args = new[] { "--no-sandbox" }
		});
		Debug.WriteLine("[ModDBModService] Shared browser instance created.");
	}

	/// <summary>
	/// OPTIMIZATION: Ensures a shared, persistent page is created and configured for speed.
	/// </summary>
	private async Task EnsurePageAsync()
	{
		await EnsureBrowserAsync();
		if (_page != null && !_page.IsClosed) return;

		Debug.WriteLine("[ModDBModService] Creating new shared page instance...");
		_page = await _browser!.NewPageAsync();
		await _page.SetUserAgentAsync(UserAgent);

		// --- THE #1 PERFORMANCE OPTIMIZATION: REQUEST BLOCKING ---
		await _page.SetRequestInterceptionAsync(true);
		_page.Request += (sender, e) =>
		{
			var resourceType = e.Request.ResourceType;
			if (resourceType == ResourceType.Image ||
				resourceType == ResourceType.StyleSheet ||
				resourceType == ResourceType.Font ||
				resourceType == ResourceType.Media)
			{
				// Abort unnecessary requests to speed up page loading.
				_ = e.Request.AbortAsync();
			}
			else
			{
				// Allow essential requests (documents, scripts, etc.).
				_ = e.Request.ContinueAsync();
			}
		};
		Debug.WriteLine("[ModDBModService] Shared page created with request interception enabled.");
	}

	public async Task<List<ModInfo>> GetAllModsAsync(ModQueryOptions options)
	{
		await EnsurePageAsync();
		var modList = new List<ModInfo>();
		var installedMods = await _installedModsService.GetInstalledModsAsync();
		var installedModsDict = installedMods.ToDictionary(m => m.ModPageUrl, m => m.FilePageUrl, StringComparer.OrdinalIgnoreCase);

		try
		{
			var query = options.ToUrlQuery();
			var modsUrl = $"https://www.moddb.com/games/garrys-mod-10/mods/page/{options.Page}?{query}";
			Debug.WriteLine($"[ModDBModService] Navigating to: {modsUrl}");

			// Use the shared page instance
			await _page!.GoToAsync(modsUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
			var html = await _page.GetContentAsync();

			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var modNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'rowcontent')]");
			if (modNodes != null)
			{
				foreach (var node in modNodes)
				{
					var titleNode = node.SelectSingleNode(".//div[@class='content']/h4/a");
					var summaryNode = node.SelectSingleNode(".//div[@class='content']/p");
					var thumbnailNode = node.SelectSingleNode("./a[@class='image']/img");
					var rankNode = node.SelectSingleNode("./a[@class='image']/div[@class='imageoverlay']");
					var visitsNode = node.SelectSingleNode(".//span[@class='date']/span");
					var timeNode = node.SelectSingleNode(".//span[@class='subheading']/time");
					var genreText = timeNode?.NextSibling?.InnerText.Trim();
					var thumbnailUrl = thumbnailNode?.GetAttributeValue("src", string.Empty);
					var modPageUrl = "https://www.moddb.com" + titleNode?.GetAttributeValue("href", string.Empty);

					var modInfo = new ModInfo
					{
						Title = titleNode?.InnerText.Trim() ?? "N/A",
						Summary = summaryNode?.InnerText.Trim() ?? "N/A",
						ModPageUrl = modPageUrl,
						Author = "N/A",
						Genre = string.IsNullOrEmpty(genreText) ? null : genreText,
						ThumbnailUrl = thumbnailUrl,
						IsInstalled = installedModsDict.ContainsKey(modPageUrl)
					};

					if (int.TryParse(rankNode?.InnerText, out int rank)) modInfo.Rank = rank;
					if (!string.IsNullOrEmpty(thumbnailUrl))
					{
						var match = Regex.Match(thumbnailUrl, @"/mods/\d+/\d+/(\d+)/");
						if (match.Success && int.TryParse(match.Groups[1].Value, out int modId)) modInfo.ModId = modId;
					}
					if (int.TryParse(visitsNode?.InnerText, out int totalVisits)) modInfo.TotalVisits = totalVisits;
					var dailyVisitsTitle = visitsNode?.GetAttributeValue("title", "");
					if (!string.IsNullOrEmpty(dailyVisitsTitle))
					{
						var match = Regex.Match(dailyVisitsTitle, @"(\d+)");
						if (match.Success && int.TryParse(match.Value, out int dailyVisits)) modInfo.DailyVisits = dailyVisits;
					}
					var dateString = timeNode?.GetAttributeValue("datetime", "");
					if (DateTime.TryParse(dateString, out DateTime releaseDate)) modInfo.ReleaseDate = releaseDate;
					modList.Add(modInfo);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] FATAL Error getting mod list: {ex}. Resetting page.");
			if (_page != null) { await _page.CloseAsync(); _page = null; }
		}
		return modList;
	}

	public async Task<List<ModFile>> GetFilesForModAsync(ModInfo mod)
	{
		var files = new List<ModFile>();
		if (string.IsNullOrEmpty(mod.ModPageUrl)) return files;
		var installedMods = await _installedModsService.GetInstalledModsAsync();
		var installedFileForThisMod = installedMods
			.FirstOrDefault(m => m.ModPageUrl.Equals(mod.ModPageUrl, StringComparison.OrdinalIgnoreCase))?
			.FilePageUrl;
		var modSlug = mod.ModPageUrl.Split('/').LastOrDefault();
		if (string.IsNullOrEmpty(modSlug)) return files;
		var rssUrl = $"https://rss.moddb.com/mods/{modSlug}/downloads/feed/rss.xml";
		try
		{
			using var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncherApp/1.0");
			var xmlString = await httpClient.GetStringAsync(rssUrl);
			var doc = XDocument.Parse(xmlString);
			foreach (var item in doc.Descendants("item"))
			{
				var filePageUrl = item.Element("link")?.Value ?? string.Empty;
				files.Add(new ModFile
				{
					Title = item.Element("title")?.Value ?? "N/A",
					FilePageUrl = filePageUrl,
					PublishDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var date) ? date : DateTime.MinValue,
					IsInstalled = !string.IsNullOrEmpty(installedFileForThisMod) && filePageUrl.Equals(installedFileForThisMod, StringComparison.OrdinalIgnoreCase)
				});
			}
		}
		catch (Exception ex) { Debug.WriteLine($"[ModDBModService] Error fetching RSS feed: {ex.Message}"); }
		return files.OrderByDescending(f => f.PublishDate).ToList();
	}

	public async Task<ModFile> GetFileDetailsAndUrlAsync(ModFile file)
	{
		await EnsurePageAsync();
		try
		{
			Debug.WriteLine($"[ModDBModService] Navigating to file page: {file.FilePageUrl}");
			await _page!.GoToAsync(file.FilePageUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 25000 });

			async Task<string?> GetElementText(string selector)
			{
				try
				{
					await _page.WaitForSelectorAsync(selector, new WaitForSelectorOptions { Timeout = 7000 });
					return await _page.EvaluateExpressionAsync<string>($"document.querySelector('{selector}').textContent");
				}
				catch (WaitTaskTimeoutException) { Debug.WriteLine($"[ModDBModService] Timeout: Selector '{selector}' not found."); return null; }
			}

			Debug.WriteLine("[ModDBModService] Scraping file details...");
			file.Filename = (await GetElementText("#downloadsinfo .row:nth-child(2) .summary"))?.Trim();
			file.Uploader = (await GetElementText("#downloadsinfo .row:nth-child(4) .summary a"))?.Trim();
			var sizeText = await GetElementText("#downloadsinfo .row:nth-child(6) .summary");
			file.Md5Hash = (await GetElementText("#downloadsinfo .row:nth-child(8) .summary"))?.Trim();
			if (!string.IsNullOrEmpty(sizeText))
			{
				var match = Regex.Match(sizeText, @"\(([\d,]+) bytes\)");
				if (match.Success) file.SizeInBytes = long.Parse(match.Groups[1].Value.Replace(",", ""));
			}

			Debug.WriteLine("[ModDBModService] Finding start link...");
			const string startLinkSelector = "a#downloadmirrorstoggle";
			await _page.WaitForSelectorAsync(startLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
			var startLink = await _page.EvaluateExpressionAsync<string>($"document.querySelector('{startLinkSelector}').href");

			if (!string.IsNullOrEmpty(startLink))
			{
				Debug.WriteLine($"[ModDBModService] Navigating to intermediate page: {startLink}");
				await _page.GoToAsync(startLink, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 15000 });

				Debug.WriteLine("[ModDBModService] Finding final mirror link...");
				const string mirrorLinkSelector = @"p > a[href*='/downloads/mirror/']";
				await _page.WaitForSelectorAsync(mirrorLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
				var mirrorLink = await _page.EvaluateExpressionAsync<string>($"document.querySelector(`{mirrorLinkSelector}`).href");

				file.DirectDownloadUrl = mirrorLink;
				Debug.WriteLine($"[ModDBModService] SUCCESS: Found direct URL: {file.DirectDownloadUrl}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] FATAL Error getting file details: {ex}. Resetting page.");
			if (_page != null) { await _page.CloseAsync(); _page = null; }
		}
		return file;
	}

	public async Task DownloadFileAsync(ModFile file, string destinationPath, IProgress<double> progress)
	{
		if (string.IsNullOrEmpty(file.DirectDownloadUrl))
			throw new InvalidOperationException("Direct download URL is not available.");

		await EnsurePageAsync(); // Ensure page and browser are ready

		// Use the temporary directory of the final destination file
		var tempDownloadPath = Path.GetDirectoryName(destinationPath);
		var finalFileName = Path.GetFileName(destinationPath);
		Directory.CreateDirectory(tempDownloadPath!);

		try
		{
			progress.Report(0);
			Debug.WriteLine($"[ModDBModService] Preparing to download to temp path: {tempDownloadPath}");

			var client = _page!.Client;
			var downloadCompletionSource = new TaskCompletionSource<string>();
			string? downloadGuid = null;
			string? suggestedFilename = null;

			void MessageReceivedHandler(object? sender, MessageEventArgs e)
			{
				try
				{
					switch (e.MessageID)
					{
						case "Browser.downloadWillBegin":
							var beginData = e.MessageData.Deserialize<DownloadWillBeginEventArgs>();
							if (beginData != null)
							{
								downloadGuid = beginData.Guid;
								suggestedFilename = beginData.SuggestedFilename;
								Debug.WriteLine($"[ModDBModService] Browser.downloadWillBegin: guid={downloadGuid}, filename={suggestedFilename}");
							}
							break;
						case "Browser.downloadProgress":
							var progressData = e.MessageData.Deserialize<DownloadProgressEventArgs>();
							if (progressData != null && progressData.Guid == downloadGuid)
							{
								if (progressData.State == "completed")
								{
									Debug.WriteLine($"[ModDBModService] Browser.downloadProgress: COMPLETED guid={progressData.Guid}");
									if (string.IsNullOrEmpty(suggestedFilename))
									{
										downloadCompletionSource.TrySetException(new Exception("Download completed but the final filename was not captured."));
									}
									else
									{
										downloadCompletionSource.TrySetResult(Path.Combine(tempDownloadPath!, suggestedFilename));
									}
								}
								else if (progressData.State == "canceled")
								{
									downloadCompletionSource.TrySetException(new Exception("Browser download was canceled."));
								}
								else if (progressData.TotalBytes > 0)
								{
									// Scale the progress from 10% to 90%
									var percentage = 10 + ((double)progressData.ReceivedBytes / progressData.TotalBytes * 80);
									progress.Report(percentage);
								}
							}
							break;
					}
				}
				catch (Exception ex) { Debug.WriteLine($"[ModDBModService] Error processing CDP message: {ex.Message}"); }
			}

			client.MessageReceived += MessageReceivedHandler;

			try
			{
				await client.SendAsync("Browser.setDownloadBehavior", new
				{
					behavior = "allow",
					downloadPath = tempDownloadPath,
					eventsEnabled = true
				});
				progress.Report(10);
				try
				{
					Debug.WriteLine($"[ModDBModService] Triggering navigation to: {file.DirectDownloadUrl}");
					await _page.GoToAsync(file.DirectDownloadUrl, new NavigationOptions { Timeout = 60000 });
				}
				catch (NavigationException ex) when (ex.Message.Contains("net::ERR_ABORTED"))
				{
					Debug.WriteLine("[ModDBModService] Caught expected ERR_ABORTED. The browser's download manager has taken over.");
				}

				Debug.WriteLine("[ModDBModService] Waiting for the browser download to complete...");
				var downloadedFilePath = await downloadCompletionSource.Task.WaitAsync(TimeSpan.FromMinutes(30));
				Debug.WriteLine($"[ModDBModService] Download complete. File located at: {downloadedFilePath}");

				// If the downloaded filename is different, move/rename it. Otherwise, it's already in the right place.
				if (!string.Equals(Path.GetFileName(downloadedFilePath), finalFileName, StringComparison.Ordinal))
				{
					File.Move(downloadedFilePath, destinationPath, true);
					Debug.WriteLine($"[ModDBModService] File successfully moved to: {destinationPath}");
				}
				else
				{
					Debug.WriteLine($"[ModDBModService] File already has correct name and location: {destinationPath}");
				}
				progress.Report(100);
			}
			finally
			{
				client.MessageReceived -= MessageReceivedHandler;
				Debug.WriteLine("[ModDBModService] Event handler unsubscribed.");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] FATAL Error during file download process: {ex}");
			progress.Report(100);
			throw;
		}
	}

	public async Task InstallModFileAsync(ModInfo mod, ModFile file, Func<string, Task<bool>> confirmationProvider, IProgress<InstallProgressReport> progress)
	{
		var tempFilePath = string.Empty;
		try
		{
			progress.Report(new InstallProgressReport { Message = "Fetching file details...", Percentage = 2 });
			var detailedFile = await GetFileDetailsAndUrlAsync(file);
			if (string.IsNullOrEmpty(detailedFile.DirectDownloadUrl) || string.IsNullOrEmpty(detailedFile.Filename))
			{
				throw new Exception("Could not retrieve file details or download URL.");
			}

			tempFilePath = Path.Combine(Path.GetTempPath(), detailedFile.Filename);
			var downloadProgress = new Progress<double>(percentage =>
			{
				var scaled = 5 + (percentage * 0.85); // Scale 0-100 to 5-90
				progress.Report(new InstallProgressReport { Message = $"Downloading... {percentage:F1}%", Percentage = (int)scaled });
			});
			await DownloadFileAsync(detailedFile, tempFilePath, downloadProgress);
			progress.Report(new InstallProgressReport { Message = "Download complete!", Percentage = 90 });

			var installProgress = new Progress<string>(message =>
			{
				progress.Report(new InstallProgressReport { Message = message, Percentage = 95 });
			});
			var installedPaths = await _addonInstallService.InstallAddonAsync(tempFilePath, file.Title, confirmationProvider, installProgress);
			progress.Report(new InstallProgressReport { Message = "Finalizing installation...", Percentage = 98 });

			var installedInfo = new InstalledModInfo
			{
				ModPageUrl = mod.ModPageUrl ?? string.Empty,
				FilePageUrl = file.FilePageUrl,
				InstallDate = DateTime.UtcNow,
				InstalledPaths = installedPaths
			};
			await _installedModsService.AddInstalledModAsync(installedInfo);

			mod.IsInstalled = true;
			progress.Report(new InstallProgressReport { Message = "Installation successful!", Percentage = 100 });
		}
		finally
		{
			if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
			{
				try { File.Delete(tempFilePath); }
				catch (Exception ex) { Debug.WriteLine($"[ModDBModService] Failed to clean up temp file: {ex.Message}"); }
			}
		}
	}

	public async Task UninstallModAsync(ModInfo mod, IProgress<InstallProgressReport> progress)
	{
		if (mod.ModPageUrl == null)
			throw new InvalidOperationException("Mod does not have a valid page URL.");

		progress.Report(new InstallProgressReport { Message = $"Uninstalling {mod.Title}...", Percentage = 10 });
		var installedMods = await _installedModsService.GetInstalledModsAsync();
		var modToUninstall = installedMods.FirstOrDefault(m => m.ModPageUrl.Equals(mod.ModPageUrl, StringComparison.OrdinalIgnoreCase));
		if (modToUninstall == null)
		{
			progress.Report(new InstallProgressReport { Message = "Mod not found in installed list.", Percentage = 100 });
			return;
		}
		var uninstallProgress = new Progress<string>(message =>
		{
			progress.Report(new InstallProgressReport { Message = message, Percentage = 50 });
		});
		await _addonInstallService.UninstallAddonAsync(modToUninstall.InstalledPaths, uninstallProgress);
		progress.Report(new InstallProgressReport { Message = "Removing installation record...", Percentage = 90 });
		await _installedModsService.RemoveInstalledModAsync(mod.ModPageUrl);
		mod.IsInstalled = false;
		progress.Report(new InstallProgressReport { Message = "Uninstallation complete!", Percentage = 100 });
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		Debug.WriteLine("[ModDBModService] Disposing service and closing browser...");
		_browser?.CloseAsync().GetAwaiter().GetResult();
		_browser?.Dispose();
		_isDisposed = true;
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Helper class to deserialize the Browser.downloadWillBegin event data.
/// </summary>
internal class DownloadWillBeginEventArgs
{
	[System.Text.Json.Serialization.JsonPropertyName("guid")]
	public string Guid { get; set; } = string.Empty;
	[System.Text.Json.Serialization.JsonPropertyName("suggestedFilename")]
	public string SuggestedFilename { get; set; } = string.Empty;
}

/// <summary>
/// Helper class to deserialize the Browser.downloadProgress event data.
/// </summary>
internal class DownloadProgressEventArgs
{
	[System.Text.Json.Serialization.JsonPropertyName("guid")]
	public string Guid { get; set; } = string.Empty;
	[System.Text.Json.Serialization.JsonPropertyName("state")]
	public string State { get; set; } = string.Empty;
	[System.Text.Json.Serialization.JsonPropertyName("receivedBytes")]
	public long ReceivedBytes { get; set; }
	[System.Text.Json.Serialization.JsonPropertyName("totalBytes")]
	public long TotalBytes { get; set; }
}