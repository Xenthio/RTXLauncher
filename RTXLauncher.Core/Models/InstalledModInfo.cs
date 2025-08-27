namespace RTXLauncher.Core.Models;

public class InstalledModInfo
{
	public string ModPageUrl { get; set; } = string.Empty;
	public string FilePageUrl { get; set; } = string.Empty;
	public DateTime InstallDate { get; set; }
	public List<string> InstalledPaths { get; set; } = new();
}