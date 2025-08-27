using HtmlAgilityPack;
using PuppeteerSharp;
using RTXLauncher.Core.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
}