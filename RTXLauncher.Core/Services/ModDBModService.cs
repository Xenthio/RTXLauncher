using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
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
	private readonly DownloadManager _downloadManager;
	private IBrowser? _browser;
	private IPage? _page; // Reusable page instance for performance
	private bool _isDisposed;

	// Use a full browser User-Agent string consistently.
	public readonly static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 (RTXLauncher/1.0)";

	/// <summary>
	/// Enable debug mode to capture screenshots and HTML dumps
	/// </summary>
	public static bool DebugMode { get; set; } = false;

	/// <summary>
	/// Run browser in headless mode (false = show browser window for debugging)
	/// </summary>
	public static bool Headless { get; set; } = true;
	
	/// <summary>
	/// Progress callback for UI updates (e.g., Chrome download status)
	/// </summary>
	public IProgress<string>? OnStatusUpdate { get; set; }

	public ModDBModService(AddonInstallService addonInstallService, InstalledModsService installedModsService, DownloadManager? downloadManager = null)
	{
		_addonInstallService = addonInstallService;
		_installedModsService = installedModsService;
		_downloadManager = downloadManager ?? new DownloadManager();
	}

	private async Task EnsureBrowserAsync()
	{
		if (_browser != null && _browser.IsConnected) return;
		Debug.WriteLine("[ModDBModService] Creating new shared browser instance with Stealth plugin...");
		
		try
		{
			// Use BrowserFetcher to ensure Chrome is downloaded
			var browserFetcher = new BrowserFetcher();
			
			OnStatusUpdate?.Report("Downloading Chrome (first time setup)...");
			Debug.WriteLine("[ModDBModService] Starting Chrome download/verification...");
			
			var installedBrowser = await browserFetcher.DownloadAsync();
			
			Debug.WriteLine($"[ModDBModService] Browser info - Path: {installedBrowser.Browser}, BuildId: {installedBrowser.BuildId}");
			
			// Get the actual executable path
			var executablePath = installedBrowser.GetExecutablePath();
			Debug.WriteLine($"[ModDBModService] Chrome executable path: {executablePath}");
			
			// Verify the executable exists - if not, try to clean up and re-download
			if (!File.Exists(executablePath))
			{
				OnStatusUpdate?.Report("Chrome download incomplete, cleaning up...");
				Debug.WriteLine($"[ModDBModService] Chrome executable not found! Attempting to clean up incomplete download...");
				
				// Try to delete the incomplete Chrome folder
				var chromeDir = Path.GetDirectoryName(Path.GetDirectoryName(executablePath)); // Go up two levels to Chrome folder
				if (Directory.Exists(chromeDir))
				{
					Debug.WriteLine($"[ModDBModService] Deleting incomplete Chrome directory: {chromeDir}");
					try
					{
						Directory.Delete(chromeDir, recursive: true);
						Debug.WriteLine($"[ModDBModService] Deleted successfully. Attempting re-download...");
						
						OnStatusUpdate?.Report("Re-downloading Chrome...");
						
						// Try downloading again
						installedBrowser = await browserFetcher.DownloadAsync();
						executablePath = installedBrowser.GetExecutablePath();
						
						if (!File.Exists(executablePath))
						{
							throw new FileNotFoundException($"Chrome executable still not found after re-download at: {executablePath}", executablePath);
						}
						
						Debug.WriteLine($"[ModDBModService] Re-download successful!");
					}
					catch (Exception cleanupEx)
					{
						Debug.WriteLine($"[ModDBModService] Failed to cleanup and re-download: {cleanupEx.Message}");
						throw new InvalidOperationException($"Chrome download is incomplete and cleanup failed. Please manually delete: {chromeDir}", cleanupEx);
					}
				}
				else
				{
					throw new FileNotFoundException($"Chrome executable not found and directory doesn't exist: {executablePath}", executablePath);
				}
			}
			
			Debug.WriteLine("[ModDBModService] Chrome verification successful.");
			
			OnStatusUpdate?.Report("Starting browser...");
			
			// Initialize PuppeteerExtra with Stealth plugin to bypass Cloudflare
			var puppeteerExtra = new PuppeteerExtra();
			puppeteerExtra.Use(new StealthPlugin());
			
			// Launch with explicit executable path
			_browser = await puppeteerExtra.LaunchAsync(new LaunchOptions
			{
				ExecutablePath = executablePath,
				Headless = Headless,
				Args = new[] 
				{ 
					"--no-sandbox",
					"--disable-setuid-sandbox",
					"--disable-blink-features=AutomationControlled" // Additional stealth
				},
				DefaultViewport = new ViewPortOptions { Width = 1920, Height = 1080 }
			});
			
			Debug.WriteLine($"[ModDBModService] Shared browser instance created with Stealth (Headless: {Headless}).");
			OnStatusUpdate?.Report("Browser ready");
		}
		catch (Exception ex)
		{
			OnStatusUpdate?.Report($"Browser initialization failed: {ex.Message}");
			Debug.WriteLine($"[ModDBModService] FATAL ERROR in EnsureBrowserAsync: {ex.Message}");
			Debug.WriteLine($"[ModDBModService] Stack trace: {ex.StackTrace}");
			throw;
		}
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

		// Enable debug logging if debug mode is on
		if (DebugMode)
		{
			ModDBDebugHelper.EnableConsoleLogging(_page);
			Debug.WriteLine("[ModDBModService] Debug mode ENABLED - screenshots and HTML dumps will be saved");
		}

		// --- THE #1 PERFORMANCE OPTIMIZATION: REQUEST BLOCKING ---
		// Disable in debug mode to see all resources
		if (!DebugMode)
		{
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
		else
		{
			Debug.WriteLine("[ModDBModService] Shared page created WITHOUT request interception (debug mode).");
		}
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

			if (DebugMode)
			{
				await ModDBDebugHelper.LogPageInfoAsync(_page);
				await ModDBDebugHelper.TakeScreenshotAsync(_page, "01_file_page_loaded");
			}

			async Task<string?> GetElementText(string selector, string description = "")
			{
				try
				{
					if (DebugMode && !string.IsNullOrEmpty(description))
					{
						await ModDBDebugHelper.CheckSelectorExistsAsync(_page, selector, description);
					}
					
					await _page.WaitForSelectorAsync(selector, new WaitForSelectorOptions { Timeout = 7000 });
					return await _page.EvaluateExpressionAsync<string>($"document.querySelector('{selector}').textContent");
				}
				catch (WaitTaskTimeoutException) 
				{ 
					Debug.WriteLine($"[ModDBModService] Timeout: Selector '{selector}' not found.");
					
					if (DebugMode)
					{
						// Try to find similar elements
						await ModDBDebugHelper.FindElementsLikeAsync(_page, selector.Contains("#") ? selector.Split('#')[1].Split(' ')[0] : selector.Split('.')[0]);
					}
					
					return null; 
				}
			}

			Debug.WriteLine("[ModDBModService] Scraping file details...");
			file.Filename = (await GetElementText("#downloadsinfo .row:nth-child(2) .summary", "Filename"))?.Trim();
			file.Uploader = (await GetElementText("#downloadsinfo .row:nth-child(4) .summary a", "Uploader"))?.Trim();
			var sizeText = await GetElementText("#downloadsinfo .row:nth-child(6) .summary", "File size");
			file.Md5Hash = (await GetElementText("#downloadsinfo .row:nth-child(8) .summary", "MD5 hash"))?.Trim();
			
			if (!string.IsNullOrEmpty(sizeText))
			{
				var match = Regex.Match(sizeText, @"\(([\d,]+) bytes\)");
				if (match.Success) file.SizeInBytes = long.Parse(match.Groups[1].Value.Replace(",", ""));
			}

			Debug.WriteLine($"[ModDBModService] Scraped - Filename: {file.Filename}, Size: {file.SizeInBytes}, MD5: {file.Md5Hash}");

			if (DebugMode)
			{
				await ModDBDebugHelper.TakeScreenshotAsync(_page, "02_details_scraped");
			}

			Debug.WriteLine("[ModDBModService] Finding start link...");
			const string startLinkSelector = "a#downloadmirrorstoggle";
			
			if (DebugMode)
			{
				var hasDownloadButton = await ModDBDebugHelper.CheckSelectorExistsAsync(_page, startLinkSelector, "Download button");
				if (!hasDownloadButton)
				{
					// Try to find download-related links
					await ModDBDebugHelper.GetLinksContainingTextAsync(_page, "download");
				}
			}
			
			await _page.WaitForSelectorAsync(startLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
			var startLink = await _page.EvaluateExpressionAsync<string>($"document.querySelector('{startLinkSelector}').href");

			if (!string.IsNullOrEmpty(startLink))
			{
				Debug.WriteLine($"[ModDBModService] Navigating to intermediate page: {startLink}");
				await _page.GoToAsync(startLink, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 15000 });

				if (DebugMode)
				{
					await ModDBDebugHelper.LogPageInfoAsync(_page);
					await ModDBDebugHelper.TakeScreenshotAsync(_page, "03_intermediate_page");
				}

				Debug.WriteLine("[ModDBModService] Finding final mirror link...");
				const string mirrorLinkSelector = @"p > a[href*='/downloads/mirror/']";
				
				if (DebugMode)
				{
					var hasMirrorLink = await ModDBDebugHelper.CheckSelectorExistsAsync(_page, mirrorLinkSelector, "Mirror link");
					if (!hasMirrorLink)
					{
						await ModDBDebugHelper.GetLinksContainingTextAsync(_page, "mirror");
						await ModDBDebugHelper.DumpPageHtmlAsync(_page, "04_mirror_page_html");
					}
				}
				
				await _page.WaitForSelectorAsync(mirrorLinkSelector, new WaitForSelectorOptions { Timeout = 10000 });
				var mirrorLink = await _page.EvaluateExpressionAsync<string>($"document.querySelector(`{mirrorLinkSelector}`).href");

				file.DirectDownloadUrl = mirrorLink;
				Debug.WriteLine($"[ModDBModService] SUCCESS: Found direct URL: {file.DirectDownloadUrl}");
				
				if (DebugMode)
				{
					await ModDBDebugHelper.TakeScreenshotAsync(_page, "05_success");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBModService] FATAL Error getting file details: {ex}");
			Debug.WriteLine($"[ModDBModService] Stack trace: {ex.StackTrace}");
			
			if (DebugMode)
			{
				try
				{
					await ModDBDebugHelper.CreateDebugReportAsync(_page!, file.FilePageUrl);
					Debug.WriteLine("[ModDBModService] Debug report created. Check the debug folder for screenshots and HTML dumps.");
					Debug.WriteLine($"[ModDBModService] To open debug folder, call ModDBDebugHelper.OpenDebugFolder()");
				}
				catch (Exception debugEx)
				{
					Debug.WriteLine($"[ModDBModService] Failed to create debug report: {debugEx.Message}");
				}
			}
			
			if (_page != null) { await _page.CloseAsync(); _page = null; }
		}
		return file;
	}

	public async Task DownloadFileAsync(ModFile file, string destinationPath, IProgress<double> progress)
	{
		if (string.IsNullOrEmpty(file.DirectDownloadUrl))
			throw new InvalidOperationException("Direct download URL is not available.");

		// Try using DownloadManager first for direct URLs (simpler, more robust)
		// Fall back to Puppeteer if DownloadManager fails
		bool tryDownloadManager = true;
		
		if (tryDownloadManager)
		{
			try
			{
				Debug.WriteLine($"[ModDBModService] Attempting download with DownloadManager: {file.DirectDownloadUrl}");
				
				// Configure download options with MD5 verification if available
				var downloadOptions = new DownloadOptions
				{
					MaxRetries = 5,
					TimeoutMinutes = 30,
					AllowResume = true,
					UserAgent = UserAgent
				};

				// Add MD5 verification if hash is available
				if (!string.IsNullOrEmpty(file.Md5Hash))
				{
					downloadOptions.ExpectedHash = file.Md5Hash;
					downloadOptions.HashAlgorithm = HashAlgorithmType.MD5;
					Debug.WriteLine($"[ModDBModService] Will verify MD5 hash: {file.Md5Hash}");
				}

				// Map EnhancedDownloadProgress to simple percentage progress
				var mappedProgress = new Progress<EnhancedDownloadProgress>(enhancedProgress =>
				{
					progress.Report(enhancedProgress.PercentComplete);
				});

				// Attempt download with DownloadManager
				var result = await _downloadManager.DownloadFileAsync(
					file.DirectDownloadUrl,
					destinationPath,
					downloadOptions,
					mappedProgress);

				if (result.Success)
				{
					Debug.WriteLine($"[ModDBModService] DownloadManager succeeded. Downloaded {result.BytesDownloaded} bytes.");
					progress.Report(100);
					return; // Success!
				}
				else
				{
					Debug.WriteLine($"[ModDBModService] DownloadManager failed: {result.ErrorMessage}. Falling back to Puppeteer...");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ModDBModService] DownloadManager exception: {ex.Message}. Falling back to Puppeteer...");
			}
		}

		// Fallback to Puppeteer-based download (original implementation)
		Debug.WriteLine("[ModDBModService] Using Puppeteer browser-based download...");
		await DownloadFileWithPuppeteerAsync(file, destinationPath, progress);
	}

	/// <summary>
	/// Original Puppeteer-based download method as fallback
	/// </summary>
	private async Task DownloadFileWithPuppeteerAsync(ModFile file, string destinationPath, IProgress<double> progress)
	{
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