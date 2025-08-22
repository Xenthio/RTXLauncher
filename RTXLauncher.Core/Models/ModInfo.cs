// Core/Models/ModInfo.cs
namespace RTXLauncher.Core.Models;

public class ModInfo
{
	public string Title { get; set; } = string.Empty;
	public string Author { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string? ThumbnailUrl { get; set; }
	public bool IsInstalled { get; set; }
}