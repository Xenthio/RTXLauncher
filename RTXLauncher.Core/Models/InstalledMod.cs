namespace RTXLauncher.Core.Models;

/// <summary>
/// Represents an installed mod from either ModDB or local rtx-remix/mods folder
/// </summary>
public class InstalledMod
{
	/// <summary>
	/// Display name of the mod
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Folder name of the mod (e.g., "testmod")
	/// </summary>
	public string FolderName { get; set; } = string.Empty;

	/// <summary>
	/// Description of the mod
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Author of the mod
	/// </summary>
	public string? Author { get; set; }

	/// <summary>
	/// Version of the mod
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	/// Whether the mod is currently enabled (in mods/ folder) or disabled (in disabled_mods/ folder)
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// Whether this mod was installed from ModDB
	/// </summary>
	public bool IsFromModDB { get; set; }

	/// <summary>
	/// ModDB page URL if this mod is from ModDB
	/// </summary>
	public string? ModDBPageUrl { get; set; }

	/// <summary>
	/// Date when the mod was installed
	/// </summary>
	public DateTime? InstallDate { get; set; }

	/// <summary>
	/// Whether this mod can be deleted (base_mod cannot be deleted)
	/// </summary>
	public bool IsDeletable => !FolderName.Equals("base_mod", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Full path to the mod folder
	/// </summary>
	public string FullPath { get; set; } = string.Empty;

	/// <summary>
	/// Whether the mod has a valid mod.usda file
	/// </summary>
	public bool HasModUsda { get; set; }
}
