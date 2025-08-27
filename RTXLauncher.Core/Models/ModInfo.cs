namespace RTXLauncher.Core.Models;

public class ModInfo
{
	public string Title { get; set; } = string.Empty;
	public string Author { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string? ThumbnailUrl { get; set; }
	public string? ModPageUrl { get; set; }
	public bool IsInstalled { get; set; }
	public int? ModId { get; set; }
	public int? Rank { get; set; }
	public int? TotalVisits { get; set; }
	public int? DailyVisits { get; set; }
	public DateTime? ReleaseDate { get; set; }
	public string? Genre { get; set; }
}