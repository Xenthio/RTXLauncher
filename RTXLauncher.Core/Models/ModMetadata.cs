namespace RTXLauncher.Core.Models;

/// <summary>
/// Metadata stored in mod_info.json for each mod
/// </summary>
public class ModMetadata
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? Author { get; set; }
	public string? Version { get; set; }
	public bool FromModDB { get; set; }
	public string? ModDBUrl { get; set; }
	public DateTime? InstallDate { get; set; }
}
