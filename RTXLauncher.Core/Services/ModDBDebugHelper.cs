using PuppeteerSharp;
using System.Diagnostics;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Debugging utilities for ModDB scraping issues
/// </summary>
public static class ModDBDebugHelper
{
	private static readonly string DebugOutputPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"RTXLauncher",
		"ModDBDebug");

	static ModDBDebugHelper()
	{
		if (!Directory.Exists(DebugOutputPath))
		{
			Directory.CreateDirectory(DebugOutputPath);
		}
	}

	/// <summary>
	/// Takes a screenshot and saves it with a timestamp
	/// </summary>
	public static async Task<string> TakeScreenshotAsync(IPage page, string stepName)
	{
		try
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var filename = $"{timestamp}_{SanitizeFilename(stepName)}.png";
			var fullPath = Path.Combine(DebugOutputPath, filename);

			await page.ScreenshotAsync(fullPath);
			Debug.WriteLine($"[ModDBDebug] Screenshot saved: {fullPath}");
			return fullPath;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Failed to take screenshot: {ex.Message}");
			return string.Empty;
		}
	}

	/// <summary>
	/// Dumps the current page HTML to a file
	/// </summary>
	public static async Task<string> DumpPageHtmlAsync(IPage page, string stepName)
	{
		try
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var filename = $"{timestamp}_{SanitizeFilename(stepName)}.html";
			var fullPath = Path.Combine(DebugOutputPath, filename);

			var html = await page.GetContentAsync();
			await File.WriteAllTextAsync(fullPath, html);
			Debug.WriteLine($"[ModDBDebug] HTML dump saved: {fullPath}");
			return fullPath;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Failed to dump HTML: {ex.Message}");
			return string.Empty;
		}
	}

	/// <summary>
	/// Checks if a selector exists on the page and logs the result
	/// </summary>
	public static async Task<bool> CheckSelectorExistsAsync(IPage page, string selector, string description)
	{
		try
		{
			var element = await page.QuerySelectorAsync(selector);
			var exists = element != null;
			
			if (exists)
			{
				Debug.WriteLine($"[ModDBDebug] ✓ Found: {description} (selector: {selector})");
				
				// Try to get the text content
				try
				{
					var text = await page.EvaluateExpressionAsync<string>($"document.querySelector('{selector}')?.textContent");
					if (!string.IsNullOrEmpty(text))
					{
						Debug.WriteLine($"[ModDBDebug]   Content: {text.Substring(0, Math.Min(100, text.Length))}...");
					}
				}
				catch { }
			}
			else
			{
				Debug.WriteLine($"[ModDBDebug] ✗ NOT FOUND: {description} (selector: {selector})");
			}
			
			return exists;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Error checking selector '{selector}': {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Lists all elements matching a partial selector pattern
	/// </summary>
	public static async Task<List<string>> FindElementsLikeAsync(IPage page, string partialSelector)
	{
		try
		{
			var script = $@"
				Array.from(document.querySelectorAll('*'))
					.filter(el => el.matches('{partialSelector}') || el.id?.includes('{partialSelector}') || el.className?.includes('{partialSelector}'))
					.slice(0, 20)
					.map(el => {{
						const tag = el.tagName.toLowerCase();
						const id = el.id ? `#${{el.id}}` : '';
						const classes = el.className ? `.${{el.className.split(' ').join('.')}}` : '';
						return `${{tag}}${{id}}${{classes}}`;
					}});
			";
			
			var results = await page.EvaluateExpressionAsync<string[]>(script);
			Debug.WriteLine($"[ModDBDebug] Found {results.Length} elements matching '{partialSelector}':");
			foreach (var result in results)
			{
				Debug.WriteLine($"[ModDBDebug]   - {result}");
			}
			return results.ToList();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Error finding elements: {ex.Message}");
			return new List<string>();
		}
	}

	/// <summary>
	/// Gets all links on the page containing specific text
	/// </summary>
	public static async Task<Dictionary<string, string>> GetLinksContainingTextAsync(IPage page, string containsText)
	{
		try
		{
			var script = $@"
				Array.from(document.querySelectorAll('a'))
					.filter(a => a.href.includes('{containsText}'))
					.slice(0, 20)
					.map(a => ({{ text: a.textContent.trim(), href: a.href }}));
			";
			
			var results = await page.EvaluateExpressionAsync<dynamic[]>(script);
			var links = new Dictionary<string, string>();
			
			Debug.WriteLine($"[ModDBDebug] Found {results.Length} links containing '{containsText}':");
			foreach (var result in results)
			{
				var text = (string)result.text;
				var href = (string)result.href;
				Debug.WriteLine($"[ModDBDebug]   - '{text}' => {href}");
				links[text] = href;
			}
			
			return links;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Error getting links: {ex.Message}");
			return new Dictionary<string, string>();
		}
	}

	/// <summary>
	/// Logs detailed page information
	/// </summary>
	public static async Task LogPageInfoAsync(IPage page)
	{
		try
		{
			Debug.WriteLine("[ModDBDebug] === PAGE INFORMATION ===");
			Debug.WriteLine($"[ModDBDebug] URL: {page.Url}");
			Debug.WriteLine($"[ModDBDebug] Title: {await page.GetTitleAsync()}");
			
			// Get viewport size
			var viewport = page.Viewport;
			Debug.WriteLine($"[ModDBDebug] Viewport: {viewport?.Width}x{viewport?.Height}");
			
			// Check for common error messages
			var errorSelectors = new[]
			{
				"div.error",
				"div.alert",
				"div.warning",
				"p[contains(text(), 'error')]",
				"p[contains(text(), 'not found')]"
			};
			
			foreach (var selector in errorSelectors)
			{
				try
				{
					var element = await page.QuerySelectorAsync(selector);
					if (element != null)
					{
						var text = await page.EvaluateFunctionAsync<string>("el => el.textContent", element);
						Debug.WriteLine($"[ModDBDebug] ⚠ Found error message: {text}");
					}
				}
				catch { }
			}
			
			Debug.WriteLine("[ModDBDebug] === END PAGE INFORMATION ===");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Error logging page info: {ex.Message}");
		}
	}

	/// <summary>
	/// Enables detailed browser console logging
	/// </summary>
	public static void EnableConsoleLogging(IPage page)
	{
		page.Console += (sender, e) =>
		{
			var type = e.Message.Type.ToString().ToUpper();
			Debug.WriteLine($"[ModDBDebug] Browser Console [{type}]: {e.Message.Text}");
		};

		page.PageError += (sender, e) =>
		{
			Debug.WriteLine($"[ModDBDebug] Browser Error: {e.Message}");
		};

		page.RequestFailed += (sender, e) =>
		{
			Debug.WriteLine($"[ModDBDebug] Request Failed: {e.Request.Url}");
		};

		Debug.WriteLine("[ModDBDebug] Browser console logging enabled");
	}

	/// <summary>
	/// Creates a full debug report for a ModDB file page
	/// </summary>
	public static async Task CreateDebugReportAsync(IPage page, string filePageUrl)
	{
		var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		var reportPath = Path.Combine(DebugOutputPath, $"{timestamp}_debug_report.txt");
		
		using var writer = new StreamWriter(reportPath);
		
		await writer.WriteLineAsync("=".PadRight(80, '='));
		await writer.WriteLineAsync($"ModDB Debug Report - {DateTime.Now}");
		await writer.WriteLineAsync($"File Page URL: {filePageUrl}");
		await writer.WriteLineAsync("=".PadRight(80, '='));
		await writer.WriteLineAsync();
		
		// Page info
		await writer.WriteLineAsync("PAGE INFORMATION:");
		await writer.WriteLineAsync($"  Current URL: {page.Url}");
		await writer.WriteLineAsync($"  Title: {await page.GetTitleAsync()}");
		await writer.WriteLineAsync();
		
		// Check critical selectors
		await writer.WriteLineAsync("SELECTOR CHECK:");
		var selectors = new Dictionary<string, string>
		{
			{ "#downloadsinfo .row:nth-child(2) .summary", "Filename" },
			{ "#downloadsinfo .row:nth-child(4) .summary a", "Uploader" },
			{ "#downloadsinfo .row:nth-child(6) .summary", "File size" },
			{ "#downloadsinfo .row:nth-child(8) .summary", "MD5 hash" },
			{ "a#downloadmirrorstoggle", "Download button" }
		};
		
		foreach (var (selector, description) in selectors)
		{
			var exists = await page.QuerySelectorAsync(selector) != null;
			await writer.WriteLineAsync($"  {(exists ? "✓" : "✗")} {description}: {selector}");
		}
		await writer.WriteLineAsync();
		
		// Screenshot and HTML dump
		await writer.WriteLineAsync("ARTIFACTS:");
		var screenshotPath = await TakeScreenshotAsync(page, "debug_report");
		await writer.WriteLineAsync($"  Screenshot: {screenshotPath}");
		
		var htmlPath = await DumpPageHtmlAsync(page, "debug_report");
		await writer.WriteLineAsync($"  HTML dump: {htmlPath}");
		
		await writer.WriteLineAsync();
		await writer.WriteLineAsync("=".PadRight(80, '='));
		await writer.WriteLineAsync($"Debug files saved to: {DebugOutputPath}");
		await writer.WriteLineAsync("=".PadRight(80, '='));
		
		Debug.WriteLine($"[ModDBDebug] Full debug report created: {reportPath}");
		Debug.WriteLine($"[ModDBDebug] Debug folder: {DebugOutputPath}");
	}

	private static string SanitizeFilename(string filename)
	{
		var invalid = Path.GetInvalidFileNameChars();
		return string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
	}

	/// <summary>
	/// Opens the debug output folder in Windows Explorer
	/// </summary>
	public static void OpenDebugFolder()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = DebugOutputPath,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModDBDebug] Failed to open debug folder: {ex.Message}");
		}
	}
}
