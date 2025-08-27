using System.Net;

namespace RTXLauncher.Core.Models;

public class ModFile
{
	// Information from the RSS Feed
	public string Title { get; set; } = string.Empty;
	public string FilePageUrl { get; set; } = string.Empty;
	public DateTime PublishDate { get; set; }
	public bool IsInstalled { get; set; }

	// Information scraped from the FilePageUrl
	public string? Filename { get; set; }
	public long? SizeInBytes { get; set; }
	public string? Md5Hash { get; set; }
	public string? Uploader { get; set; } // We can finally get an author name here!

	// The final goal
	public string? DirectDownloadUrl { get; set; }
	public Cookie[]? Cookies { get; set; }

}