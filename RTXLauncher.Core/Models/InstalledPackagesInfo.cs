namespace RTXLauncher.Core.Models;

/// <summary>
/// Stores information about installed RTX Launcher packages/components
/// </summary>
public class InstalledPackagesInfo
{
	/// <summary>
	/// Information about the installed RTX Remix version
	/// </summary>
	public InstalledPackageVersion? Remix { get; set; }

	/// <summary>
	/// Information about the installed Fixes package version
	/// </summary>
	public InstalledPackageVersion? Fixes { get; set; }

	/// <summary>
	/// Information about the applied binary patches
	/// </summary>
	public InstalledPackageVersion? Patches { get; set; }
}

/// <summary>
/// Represents a single installed package version
/// </summary>
public class InstalledPackageVersion
{
	/// <summary>
	/// The source repository (e.g., "sambow23/dxvk-remix-gmod")
	/// </summary>
	public string Source { get; set; } = string.Empty;

	/// <summary>
	/// The version/tag name (e.g., "v0.6.3")
	/// </summary>
	public string Version { get; set; } = string.Empty;

	/// <summary>
	/// The release name (e.g., "Release 0.6.3")
	/// </summary>
	public string ReleaseName { get; set; } = string.Empty;

	/// <summary>
	/// When the package was installed
	/// </summary>
	public DateTime InstalledAt { get; set; }

	/// <summary>
	/// Optional: branch used for patches
	/// </summary>
	public string? Branch { get; set; }
}

