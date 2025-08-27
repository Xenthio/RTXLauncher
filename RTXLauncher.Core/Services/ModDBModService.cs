using HtmlAgilityPack;
using PuppeteerSharp;
using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RTXLauncher.Core.Services;

public class ModDBModService : IModService
{
	private const string ModsUrl = "https://www.moddb.com/games/garrys-mod-10/mods/page/1?filter=t&rtx=1&sort=visitstotal-desc";

	public async Task<List<ModInfo>> GetAllModsAsync()
	{
		Debug.WriteLine("[ModDBModService] Starting to fetch mods with Puppeteer...");
		var modList = new List<ModInfo>();
		IBrowser browser = null;

		try
		{
			var browserFetcher = new BrowserFetcher();
			await browserFetcher.DownloadAsync();

			browser = await Puppeteer.LaunchAsync(new LaunchOptions
			{
				Headless = true,
				Args = new[] { "--no-sandbox" }
			});

			await using var page = await browser.NewPageAsync();
			await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 (RTXLauncher/1.0)");

			var response = await page.GoToAsync(ModsUrl);
			var html = await page.GetContentAsync();

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var modNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'rowcontent')]");

			if (modNodes != null)
			{
				Debug.WriteLine($"[ModDBModService] Found {modNodes.Count} mod nodes. Parsing them now...");
				foreach (var node in modNodes)
				{
					var titleNode = node.SelectSingleNode(".//div[@class='content']/h4/a");
					var summaryNode = node.SelectSingleNode(".//div[@class='content']/p");
					var thumbnailNode = node.SelectSingleNode("./a[@class='image']/img");

					// 1. Get Rank from the 'imageoverlay' div
					var rankNode = node.SelectSingleNode("./a[@class='image']/div[@class='imageoverlay']");

					var visitsNode = node.SelectSingleNode(".//span[@class='date']/span");
					var timeNode = node.SelectSingleNode(".//span[@class='subheading']/time");
					var genreText = timeNode?.NextSibling?.InnerText.Trim();

					var modInfo = new ModInfo
					{
						Title = titleNode?.InnerText.Trim() ?? "N/A",
						Summary = summaryNode?.InnerText.Trim() ?? "N/A",
						ModPageUrl = "https://www.moddb.com" + titleNode?.GetAttributeValue("href", string.Empty),
						Author = "N/A",
						Genre = string.IsNullOrEmpty(genreText) ? null : genreText
					};

					// Safely parse Rank
					if (int.TryParse(rankNode?.InnerText, out int rank))
						modInfo.Rank = rank;

					// 2. Get ModId from the Thumbnail URL using Regex
					var thumbnailUrl = thumbnailNode?.GetAttributeValue("src", string.Empty);
					modInfo.ThumbnailUrl = thumbnailUrl; // Assign the full URL to the property

					if (!string.IsNullOrEmpty(thumbnailUrl))
					{
						// Regex to find the number pattern like /mods/1/62/61775/
						var match = Regex.Match(thumbnailUrl, @"/mods/\d+/\d+/(\d+)/");
						if (match.Success && int.TryParse(match.Groups[1].Value, out int modId))
						{
							modInfo.ModId = modId;
						}
					}

					// --- (Rest of the parsing logic is the same) ---
					if (int.TryParse(visitsNode?.InnerText, out int totalVisits))
						modInfo.TotalVisits = totalVisits;

					var dailyVisitsTitle = visitsNode?.GetAttributeValue("title", "");
					if (!string.IsNullOrEmpty(dailyVisitsTitle))
					{
						var match = Regex.Match(dailyVisitsTitle, @"(\d+)");
						if (match.Success && int.TryParse(match.Value, out int dailyVisits))
							modInfo.DailyVisits = dailyVisits;
					}

					var dateString = timeNode?.GetAttributeValue("datetime", "");
					if (DateTime.TryParse(dateString, out DateTime releaseDate))
						modInfo.ReleaseDate = releaseDate;

					modList.Add(modInfo);
					Debug.WriteLine($"--> Parsed Mod: {modInfo.Title} (ID: {modInfo.ModId}, Rank: {modInfo.Rank})");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] An unexpected error occurred: {ex.Message}");
		}
		finally
		{
			if (browser != null) await browser.CloseAsync();
		}

		Debug.WriteLine($"[ModDBModService] Finished fetching. Returning {modList.Count} mods.");
		return modList;
	}

	// Method 1: Get file list from RSS (uses HttpClient for speed)
	public async Task<List<ModFile>> GetFilesForModAsync(ModInfo mod)
	{
		var files = new List<ModFile>();
		if (mod.ModPageUrl == null) return files;

		// Extract the mod's "slug" (e.g., "gm-bigcity-rtx") from its URL
		var modSlug = mod.ModPageUrl.Split('/').Last();
		var rssUrl = $"https://rss.moddb.com/mods/{modSlug}/downloads/feed/rss.xml";

		try
		{
			using var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncherApp/1.0");
			var xmlString = await httpClient.GetStringAsync(rssUrl);

			var doc = XDocument.Parse(xmlString);
			foreach (var item in doc.Descendants("item"))
			{
				var file = new ModFile
				{
					Title = item.Element("title")?.Value ?? "N/A",
					FilePageUrl = item.Element("link")?.Value ?? string.Empty,
					PublishDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var date) ? date : DateTime.MinValue
				};
				files.Add(file);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] Error fetching RSS feed: {ex.Message}");
		}

		// Return the newest files first
		return files.OrderByDescending(f => f.PublishDate).ToList();
	}

	// Method 2: Get details and final URL (uses Puppeteer for navigation)
	public async Task<ModFile> GetFileDetailsAndUrlAsync(ModFile file)
	{
		IBrowser browser = null;
		try
		{
			var browserFetcher = new BrowserFetcher();
			await browserFetcher.DownloadAsync();
			browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = new[] { "--no-sandbox" } });
			await using var page = await browser.NewPageAsync();

			// 1. Go to the file's detail page
			await page.GoToAsync(file.FilePageUrl);

			// 2. Scrape all details from this page
			var filenameNode = await page.QuerySelectorAsync("#downloadsinfo .row:nth-child(2) .summary");
			file.Filename = (await filenameNode?.GetPropertyAsync("textContent"))?.RemoteObject.Value?.ToString()?.Trim();

			var uploaderNode = await page.QuerySelectorAsync("#downloadsinfo .row:nth-child(4) .summary a");
			file.Uploader = (await uploaderNode?.GetPropertyAsync("textContent"))?.RemoteObject.Value?.ToString()?.Trim();

			var sizeNode = await page.QuerySelectorAsync("#downloadsinfo .row:nth-child(6) .summary");
			var sizeText = (await sizeNode?.GetPropertyAsync("textContent"))?.RemoteObject.Value?.ToString()?.Trim() ?? "";
			var match = Regex.Match(sizeText, @"\(([\d,]+) bytes\)");
			if (match.Success)
				file.SizeInBytes = long.Parse(match.Groups[1].Value.Replace(",", ""));

			var hashNode = await page.QuerySelectorAsync("#downloadsinfo .row:nth-child(8) .summary");
			file.Md5Hash = (await hashNode?.GetPropertyAsync("textContent"))?.RemoteObject.Value?.ToString()?.Trim();

			// 3. Get the intermediate "/downloads/start/..." link
			var startLinkNode = await page.QuerySelectorAsync("a#downloadmirrorstoggle");
			var startLink = "https://www.moddb.com" + (await startLinkNode?.GetPropertyAsync("href"))?.RemoteObject.Value?.ToString();

			// 4. Go to the intermediate page
			if (!string.IsNullOrEmpty(startLink))
			{
				await page.GoToAsync(startLink);

				// 5. Find the final mirror link on the new page
				var mirrorLinkNode = await page.QuerySelectorAsync("p > a");
				var mirrorLink = (await mirrorLinkNode?.GetPropertyAsync("href"))?.RemoteObject.Value?.ToString();
				file.DirectDownloadUrl = mirrorLink;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] Error getting file details: {ex.Message}");
		}
		finally
		{
			if (browser != null) await browser.CloseAsync();
		}

		return file;
	}

	// Method 3: Download the file (uses HttpClient for performance)
	public async Task DownloadFileAsync(ModFile file, string destinationPath, IProgress<double> progress)
	{
		if (string.IsNullOrEmpty(file.DirectDownloadUrl))
			throw new InvalidOperationException("Direct download URL is not available.");

		using var client = new HttpClient();
		using var response = await client.GetAsync(file.DirectDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength ?? -1L;
		var totalBytesRead = 0L;

		using var contentStream = await response.Content.ReadAsStreamAsync();
		using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

		var buffer = new byte[8192];
		int bytesRead;
		while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
		{
			await fileStream.WriteAsync(buffer, 0, bytesRead);
			totalBytesRead += bytesRead;

			if (totalBytes != -1)
			{
				progress.Report((double)totalBytesRead / totalBytes * 100);
			}
		}
	}

}