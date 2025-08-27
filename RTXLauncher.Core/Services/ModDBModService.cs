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
	private IBrowser? _browser;
	private bool _isDisposed;

	// Use a full browser User-Agent string consistently.
	public readonly static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 (RTXLauncher/1.0)";

	private async Task EnsureBrowserAsync()
	{
		if (_browser != null && _browser.IsConnected) return;
		Debug.WriteLine("[ModDBModService] Creating new shared browser instance...");
		var browserFetcher = new BrowserFetcher();
		await browserFetcher.DownloadAsync();
		_browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = new[] { "--no-sandbox" } });
		Debug.WriteLine("[ModDBModService] Shared browser instance created.");
	}

	public async Task<List<ModInfo>> GetAllModsAsync()
	{
		await EnsureBrowserAsync();
		var modList = new List<ModInfo>();
		try
		{
			await using var page = await _browser!.NewPageAsync();
			await page.SetUserAgentAsync(UserAgent);
			const string modsUrl = "https://www.moddb.com/games/garrys-mod-10/mods/page/1?filter=t&rtx=1&sort=visitstotal-desc";
			await page.GoToAsync(modsUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
			var html = await page.GetContentAsync();

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

					var modInfo = new ModInfo
					{
						Title = titleNode?.InnerText.Trim() ?? "N/A",
						Summary = summaryNode?.InnerText.Trim() ?? "N/A",
						ModPageUrl = "https://www.moddb.com" + titleNode?.GetAttributeValue("href", string.Empty),
						Author = "N/A",
						Genre = string.IsNullOrEmpty(genreText) ? null : genreText,
						ThumbnailUrl = thumbnailUrl
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
		catch (Exception ex) { Debug.WriteLine($"[ModDBModService] FATAL Error getting mod list: {ex}"); }
		return modList;
	}

	public async Task<List<ModFile>> GetFilesForModAsync(ModInfo mod)
	{
		var files = new List<ModFile>();
		if (string.IsNullOrEmpty(mod.ModPageUrl)) return files;
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
				files.Add(new ModFile
				{
					Title = item.Element("title")?.Value ?? "N/A",
					FilePageUrl = item.Element("link")?.Value ?? string.Empty,
					PublishDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var date) ? date : DateTime.MinValue
				});
			}
		}
		catch (Exception ex) { Debug.WriteLine($"[ModDBModService] Error fetching RSS feed: {ex.Message}"); }
		return files.OrderByDescending(f => f.PublishDate).ToList();
	}

	public async Task<ModFile> GetFileDetailsAndUrlAsync(ModFile file)
	{
		await EnsureBrowserAsync();
		try
		{
			await using var page = await _browser!.NewPageAsync();
			await page.SetUserAgentAsync(UserAgent);

			Debug.WriteLine($"[ModDBModService] Navigating to file page: {file.FilePageUrl}");
			await page.GoToAsync(file.FilePageUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 25000 });

			async Task<string?> GetElementText(string selector)
			{
				try
				{
					await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions { Timeout = 7000 });
					return await page.EvaluateExpressionAsync<string>($"document.querySelector('{selector}').textContent");
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
			await page.WaitForSelectorAsync(startLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
			var startLink = await page.EvaluateExpressionAsync<string>($"document.querySelector('{startLinkSelector}').href");

			if (!string.IsNullOrEmpty(startLink))
			{
				Debug.WriteLine($"[ModDBModService] Navigating to intermediate page: {startLink}");
				await page.GoToAsync(startLink, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 15000 });

				Debug.WriteLine("[ModDBModService] Finding final mirror link...");
				const string mirrorLinkSelector = @"p > a[href*='/downloads/mirror/']";
				await page.WaitForSelectorAsync(mirrorLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
				var mirrorLink = await page.EvaluateExpressionAsync<string>($"document.querySelector(`{mirrorLinkSelector}`).href");

				file.DirectDownloadUrl = mirrorLink;
				Debug.WriteLine($"[ModDBModService] SUCCESS: Found direct URL: {file.DirectDownloadUrl}");
			}
		}
		catch (Exception ex) { Debug.WriteLine($"[ModDBModService] FATAL Error getting file details: {ex}"); }
		return file;
	}

	/// <summary>
	/// Downloads a file by listening to the browser's low-level CDP messages for download events,
	/// and correctly identifies the final file using the 'suggestedFilename' from the event data.
	/// This is the definitive and most robust method for handling file downloads.
	/// </summary>
	public async Task DownloadFileAsync(ModFile file, string destinationPath, IProgress<double> progress)
	{
		if (string.IsNullOrEmpty(file.DirectDownloadUrl))
			throw new InvalidOperationException("Direct download URL is not available.");

		await EnsureBrowserAsync();

		var tempDownloadPath = Path.Combine(destinationPath, "temp", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDownloadPath);

		try
		{
			progress.Report(0);
			Debug.WriteLine($"[ModDBModService] Preparing to download to temp path: {tempDownloadPath}");

			await using var page = await _browser!.NewPageAsync();
			await page.SetUserAgentAsync(UserAgent);

			var client = page.Client;
			var downloadCompletionSource = new TaskCompletionSource<string>();
			string? downloadGuid = null;
			string? finalFilename = null; // Variable to store the real filename

			// Step 1: Set up the raw message handler.
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
								finalFilename = beginData.SuggestedFilename; // Capture the real filename!
								Debug.WriteLine($"[ModDBModService] Browser.downloadWillBegin: guid={downloadGuid}, filename={finalFilename}");
							}
							break;

						case "Browser.downloadProgress":
							var progressData = e.MessageData.Deserialize<DownloadProgressEventArgs>();
							if (progressData != null && progressData.Guid == downloadGuid)
							{
								if (progressData.State == "completed")
								{
									Debug.WriteLine($"[ModDBModService] Browser.downloadProgress: COMPLETED guid={progressData.Guid}");
									if (string.IsNullOrEmpty(finalFilename))
									{
										downloadCompletionSource.TrySetException(new Exception("Download completed but the final filename was not captured."));
									}
									else
									{
										// Complete the task with the CORRECT file path
										downloadCompletionSource.TrySetResult(Path.Combine(tempDownloadPath, finalFilename));
									}
								}
								else if (progressData.State == "canceled")
								{
									downloadCompletionSource.TrySetException(new Exception("Browser download was canceled."));
								}
							}
							break;
					}
				}
				catch (Exception ex) { Debug.WriteLine($"[ModDBModService] Error processing CDP message: {ex.Message}"); }
			}

			client.MessageReceived += MessageReceivedHandler;
			progress.Report(10);

			try
			{
				await client.SendAsync("Browser.setDownloadBehavior", new
				{
					behavior = "allow",
					downloadPath = tempDownloadPath,
					eventsEnabled = true
				});

				try
				{
					Debug.WriteLine($"[ModDBModService] Triggering navigation to: {file.DirectDownloadUrl}");
					await page.GoToAsync(file.DirectDownloadUrl, new NavigationOptions { Timeout = 60000 });
				}
				catch (NavigationException ex) when (ex.Message.Contains("net::ERR_ABORTED"))
				{
					Debug.WriteLine("[ModDBModService] Caught expected ERR_ABORTED. The browser's download manager has taken over.");
				}

				Debug.WriteLine("[ModDBModService] Waiting for the browser download to complete...");
				var downloadedFilePath = await downloadCompletionSource.Task.WaitAsync(TimeSpan.FromMinutes(30));

				progress.Report(90);
				Debug.WriteLine($"[ModDBModService] Download complete. File located at: {downloadedFilePath}");

				File.Move(downloadedFilePath, destinationPath, true);

				Debug.WriteLine($"[ModDBModService] File successfully moved to: {destinationPath}");
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
		finally
		{
			if (Directory.Exists(tempDownloadPath))
			{
				Directory.Delete(tempDownloadPath, true);
			}
		}
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
}