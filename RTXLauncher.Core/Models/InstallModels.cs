// Place these in a common folder like 'Models' or 'Services'
namespace RTXLauncher.Core.Models;

public class InstallProgressReport
{
	public string Message { get; init; } = string.Empty;
	public int Percentage { get; init; }
}

public class SymlinkFailedException : Exception
{
	public string TargetPath { get; }
	public SymlinkFailedException(string message, string targetPath) : base(message)
	{
		TargetPath = targetPath;
	}
	public SymlinkFailedException(string message, string targetPath, Exception inner) : base(message, inner)
	{
		TargetPath = targetPath;
	}
}

public class DownloadProgressReport : InstallProgressReport
{
	public long BytesDownloaded { get; init; }
	public long TotalBytes { get; init; }
}

public enum FixesPackageOption
{
	Standard,
	Performance
}

public enum ReleaseChannel
{
	Stable,
	Nightly
}

/// <summary>
/// Optional local zip file overrides for quick install.
/// When a path is set, that step will use the local zip instead of downloading from GitHub.
/// </summary>
public class LocalZipOverrides
{
	/// <summary>
	/// Local zip path for RTX Remix package. Null means download from GitHub.
	/// </summary>
	public string? RemixZipPath { get; set; }

	/// <summary>
	/// Local zip path for binary patches (must contain applypatch.py). Null means download from GitHub.
	/// </summary>
	public string? PatchesZipPath { get; set; }

	/// <summary>
	/// Local zip path for fixes package. Null means download from GitHub.
	/// </summary>
	public string? FixesZipPath { get; set; }

	/// <summary>
	/// Returns true if any local zip override is set.
	/// </summary>
	public bool HasAnyOverride => RemixZipPath != null || PatchesZipPath != null || FixesZipPath != null;
}

public class FixesPackageInfo
{
	public FixesPackageOption Option { get; init; }
	public string DisplayName { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Owner { get; init; } = string.Empty;
	public string Repo { get; init; } = string.Empty;
	public string PatchOwner { get; init; } = string.Empty;
	public string PatchRepo { get; init; } = string.Empty;
	public string PatchBranch { get; init; } = string.Empty;
	public string PatchFile { get; init; } = string.Empty;
	public bool RequiresX64 { get; init; }
	public bool RequiresLegacyBuild { get; init; }
	public string LegacyManifestId { get; init; } = "2195078592256565401"; // Default to May 1, 2025
}