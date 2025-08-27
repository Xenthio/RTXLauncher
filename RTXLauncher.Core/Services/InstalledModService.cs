using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Text.Json;

namespace RTXLauncher.Core.Services;

public class InstalledModsService
{
	private readonly string _filePath;
	private List<InstalledModInfo>? _installedModsCache;

	public InstalledModsService()
	{
		var installFolder = GarrysModUtility.GetThisInstallFolder();
		_filePath = Path.Combine(installFolder, "gmrtx-installedmods.json");
	}

	public async Task<List<InstalledModInfo>> GetInstalledModsAsync()
	{
		if (_installedModsCache != null)
		{
			return _installedModsCache;
		}

		if (!File.Exists(_filePath))
		{
			_installedModsCache = new List<InstalledModInfo>();
			return _installedModsCache;
		}

		try
		{
			var json = await File.ReadAllTextAsync(_filePath);
			_installedModsCache = JsonSerializer.Deserialize<List<InstalledModInfo>>(json) ?? new List<InstalledModInfo>();
			return _installedModsCache;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[InstalledModsService] Error reading installed mods file: {ex.Message}");
			_installedModsCache = new List<InstalledModInfo>();
			return _installedModsCache; // Return empty list on error
		}
	}

	public async Task AddInstalledModAsync(InstalledModInfo mod)
	{
		var mods = await GetInstalledModsAsync();
		// Remove any existing entry for this mod page to avoid duplicates
		mods.RemoveAll(m => m.ModPageUrl.Equals(mod.ModPageUrl, StringComparison.OrdinalIgnoreCase));
		mods.Add(mod);
		await SaveAsync();
	}

	private async Task SaveAsync()
	{
		if (_installedModsCache == null) return;
		var json = JsonSerializer.Serialize(_installedModsCache, new JsonSerializerOptions { WriteIndented = true });
		await File.WriteAllTextAsync(_filePath, json);
	}
}