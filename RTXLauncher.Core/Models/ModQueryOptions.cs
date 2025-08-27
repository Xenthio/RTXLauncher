using System.Net;

namespace RTXLauncher.Core.Models;

/// <summary>
/// Represents the options for querying the list of mods.
/// </summary>
public class ModQueryOptions
{
	/// <summary>
	/// The page number to retrieve. Defaults to 1.
	/// </summary>
	public int Page { get; set; } = 1;

	/// <summary>
	/// The search term to filter mods by.
	/// </summary>
	public string? SearchText { get; set; }

	/// <summary>
	/// The sorting order for the mods.
	/// See ModDB for available options (e.g., "visitstotal-desc", "dateup-desc").
	/// </summary>
	public string SortOrder { get; set; } = "visitstotal-desc";

	/// <summary>
	/// Builds the URL query string based on the current options.
	/// </summary>
	/// <returns>A URL-formatted query string.</returns>
	public string ToUrlQuery()
	{
		var queryParts = new List<string>
		{
			"filter=t",
			"rtx=1",
			$"sort={SortOrder}"
		};

		if (!string.IsNullOrWhiteSpace(SearchText))
		{
			// ModDB uses '+' for spaces in search queries
			queryParts.Add($"kw={WebUtility.UrlEncode(SearchText).Replace("%20", "+")}");
		}

		return string.Join('&', queryParts);
	}
}