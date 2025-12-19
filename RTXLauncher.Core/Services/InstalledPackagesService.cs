// Services/InstalledPackagesService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Service for tracking installed package versions (Remix, Fixes, Patches)
/// </summary>
public class InstalledPackagesService
{
	private const string FileName = "installed_packages.json";
	private readonly string _filePath;
	private InstalledPackagesInfo? _cache;

	public InstalledPackagesService()
	{
		var installFolder = GarrysModUtility.GetThisInstallFolder();
		_filePath = Path.Combine(installFolder, FileName);
	}

	/// <summary>
	/// Gets the installed packages info, loading from disk if not cached
	/// </summary>
	public async Task<InstalledPackagesInfo> GetInstalledPackagesAsync()
	{
		if (_cache != null)
		{
			return _cache;
		}

		if (!File.Exists(_filePath))
		{
			_cache = new InstalledPackagesInfo();
			return _cache;
		}

		try
		{
			var json = await File.ReadAllTextAsync(_filePath);
			_cache = JsonSerializer.Deserialize(json, InstalledPackagesJsonContext.Default.InstalledPackagesInfo) 
				?? new InstalledPackagesInfo();
			return _cache;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[InstalledPackagesService] Error reading installed packages file: {ex.Message}");
			_cache = new InstalledPackagesInfo();
			return _cache;
		}
	}

	/// <summary>
	/// Records that a Remix package was installed
	/// </summary>
	public async Task SetRemixVersionAsync(string source, string version, string releaseName)
	{
		var packages = await GetInstalledPackagesAsync();
		packages.Remix = new InstalledPackageVersion
		{
			Source = source,
			Version = version,
			ReleaseName = releaseName,
			InstalledAt = DateTime.Now
		};
		await SaveAsync();
	}

	/// <summary>
	/// Records that a Fixes package was installed
	/// </summary>
	public async Task SetFixesVersionAsync(string source, string version, string releaseName)
	{
		var packages = await GetInstalledPackagesAsync();
		packages.Fixes = new InstalledPackageVersion
		{
			Source = source,
			Version = version,
			ReleaseName = releaseName,
			InstalledAt = DateTime.Now
		};
		await SaveAsync();
	}

	/// <summary>
	/// Records that binary patches were applied
	/// </summary>
	public async Task SetPatchesVersionAsync(string source, string branch)
	{
		var packages = await GetInstalledPackagesAsync();
		packages.Patches = new InstalledPackageVersion
		{
			Source = source,
			Version = branch, // For patches, the branch is effectively the version
			ReleaseName = $"Branch: {branch}",
			Branch = branch,
			InstalledAt = DateTime.Now
		};
		await SaveAsync();
	}

	/// <summary>
	/// Gets the installed Remix version info, or null if not installed
	/// </summary>
	public async Task<InstalledPackageVersion?> GetRemixVersionAsync()
	{
		var packages = await GetInstalledPackagesAsync();
		return packages.Remix;
	}

	/// <summary>
	/// Gets the installed Fixes version info, or null if not installed
	/// </summary>
	public async Task<InstalledPackageVersion?> GetFixesVersionAsync()
	{
		var packages = await GetInstalledPackagesAsync();
		return packages.Fixes;
	}

	/// <summary>
	/// Gets the installed Patches version info, or null if not installed
	/// </summary>
	public async Task<InstalledPackageVersion?> GetPatchesVersionAsync()
	{
		var packages = await GetInstalledPackagesAsync();
		return packages.Patches;
	}

	/// <summary>
	/// Clears the cache, forcing a reload from disk on next access
	/// </summary>
	public void ClearCache()
	{
		_cache = null;
	}

	private async Task SaveAsync()
	{
		if (_cache == null) return;

		try
		{
			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonSerializer.Serialize(_cache, InstalledPackagesJsonContext.Default.InstalledPackagesInfo);
			await File.WriteAllTextAsync(_filePath, json);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[InstalledPackagesService] Error saving installed packages file: {ex.Message}");
		}
	}
}

/// <summary>
/// JSON serialization context for InstalledPackages models
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(InstalledPackagesInfo))]
[System.Text.Json.Serialization.JsonSerializable(typeof(InstalledPackageVersion))]
public partial class InstalledPackagesJsonContext : JsonSerializerContext
{
}

