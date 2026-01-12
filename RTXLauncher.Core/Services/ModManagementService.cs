using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Text.Json;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Service for managing installed mods (both from ModDB and local mods)
/// </summary>
public class ModManagementService
{
	private readonly InstalledModsService _installedModsService;
	private readonly string _modsPath;
	private readonly string _disabledModsPath;

	public ModManagementService(InstalledModsService installedModsService)
	{
		_installedModsService = installedModsService;
		
		var installFolder = GarrysModUtility.GetThisInstallFolder();
		_modsPath = Path.Combine(installFolder, "rtx-remix", "mods");
		_disabledModsPath = Path.Combine(installFolder, "rtx-remix", "disabled_mods");
	}

	/// <summary>
	/// Gets all installed mods from both the mods and disabled_mods folders
	/// </summary>
	public async Task<List<InstalledMod>> GetAllInstalledModsAsync()
	{
		var allMods = new List<InstalledMod>();

		// Scan enabled mods folder
		if (Directory.Exists(_modsPath))
		{
			var enabledMods = await ScanModFolderAsync(_modsPath, isEnabled: true);
			allMods.AddRange(enabledMods);
		}

		// Scan disabled mods folder
		if (Directory.Exists(_disabledModsPath))
		{
			var disabledMods = await ScanModFolderAsync(_disabledModsPath, isEnabled: false);
			allMods.AddRange(disabledMods);
		}

		// Get ModDB tracked mods and enrich the list
		var moddbMods = await _installedModsService.GetInstalledModsAsync();
		EnrichWithModDBInfo(allMods, moddbMods);

		return allMods;
	}

	/// <summary>
	/// Disables a mod by moving it from mods/ to disabled_mods/
	/// </summary>
	public async Task DisableModAsync(string modFolderName)
	{
		var sourcePath = Path.Combine(_modsPath, modFolderName);
		var destPath = Path.Combine(_disabledModsPath, modFolderName);

		if (!Directory.Exists(sourcePath))
		{
			throw new DirectoryNotFoundException($"Mod folder not found: {sourcePath}");
		}

		// Create disabled_mods folder if it doesn't exist
		Directory.CreateDirectory(_disabledModsPath);

		// Move the folder
		Directory.Move(sourcePath, destPath);
		
		Debug.WriteLine($"[ModManagementService] Disabled mod: {modFolderName}");
		await Task.CompletedTask;
	}

	/// <summary>
	/// Enables a mod by moving it from disabled_mods/ to mods/
	/// </summary>
	public async Task EnableModAsync(string modFolderName)
	{
		var sourcePath = Path.Combine(_disabledModsPath, modFolderName);
		var destPath = Path.Combine(_modsPath, modFolderName);

		if (!Directory.Exists(sourcePath))
		{
			throw new DirectoryNotFoundException($"Mod folder not found: {sourcePath}");
		}

		// Ensure mods folder exists
		Directory.CreateDirectory(_modsPath);

		// Move the folder
		Directory.Move(sourcePath, destPath);
		
		Debug.WriteLine($"[ModManagementService] Enabled mod: {modFolderName}");
		await Task.CompletedTask;
	}

	/// <summary>
	/// Deletes a mod permanently
	/// </summary>
	public async Task DeleteModAsync(string modFolderName)
	{
		if (modFolderName.Equals("base_mod", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Cannot delete base_mod");
		}

		// Try to find the mod in either location
		var enabledPath = Path.Combine(_modsPath, modFolderName);
		var disabledPath = Path.Combine(_disabledModsPath, modFolderName);

		string? modPath = null;
		if (Directory.Exists(enabledPath))
		{
			modPath = enabledPath;
		}
		else if (Directory.Exists(disabledPath))
		{
			modPath = disabledPath;
		}

		if (modPath == null)
		{
			throw new DirectoryNotFoundException($"Mod folder not found: {modFolderName}");
		}

		// Delete the folder
		Directory.Delete(modPath, recursive: true);

		// Remove from ModDB tracking if it exists
		var moddbMods = await _installedModsService.GetInstalledModsAsync();
		var moddbEntry = moddbMods.FirstOrDefault(m => 
			m.InstalledPaths.Any(p => p.Contains(modFolderName)));
		
		if (moddbEntry != null)
		{
			await _installedModsService.RemoveInstalledModAsync(moddbEntry.ModPageUrl);
		}

		Debug.WriteLine($"[ModManagementService] Deleted mod: {modFolderName}");
	}

	/// <summary>
	/// Checks if a mod is currently enabled (in mods/ folder)
	/// </summary>
	public bool IsModEnabled(string modFolderName)
	{
		var enabledPath = Path.Combine(_modsPath, modFolderName);
		return Directory.Exists(enabledPath);
	}

	/// <summary>
	/// Checks if a mod can be deleted (base_mod cannot be deleted)
	/// </summary>
	public bool CanDeleteMod(string modFolderName)
	{
		return !modFolderName.Equals("base_mod", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Scans a folder for mods (folders containing mod.usda)
	/// </summary>
	private async Task<List<InstalledMod>> ScanModFolderAsync(string folderPath, bool isEnabled)
	{
		var mods = new List<InstalledMod>();

		if (!Directory.Exists(folderPath))
		{
			return mods;
		}

		var modFolders = Directory.GetDirectories(folderPath);

		foreach (var modFolder in modFolders)
		{
			var folderName = Path.GetFileName(modFolder);
			var modUsdaPath = Path.Combine(modFolder, "mod.usda");
			
			// Only include folders that have mod.usda
			if (!File.Exists(modUsdaPath))
			{
				Debug.WriteLine($"[ModManagementService] Skipping {folderName} - no mod.usda found");
				continue;
			}

			// Try to read metadata from mod_info.json
			var metadata = await ReadModMetadataAsync(modFolder);

			var mod = new InstalledMod
			{
				Name = metadata?.Name ?? folderName,
				FolderName = folderName,
				Description = metadata?.Description ?? string.Empty,
				Author = metadata?.Author,
				Version = metadata?.Version,
				IsEnabled = isEnabled,
				IsFromModDB = metadata?.FromModDB ?? false,
				ModDBPageUrl = metadata?.ModDBUrl,
				InstallDate = metadata?.InstallDate,
				FullPath = modFolder,
				HasModUsda = true
			};

			mods.Add(mod);
		}

		return mods;
	}

	/// <summary>
	/// Reads metadata from mod_info.json if it exists
	/// </summary>
	private async Task<ModMetadata?> ReadModMetadataAsync(string modFolder)
	{
		var metadataPath = Path.Combine(modFolder, "mod_info.json");
		
		if (!File.Exists(metadataPath))
		{
			return null;
		}

		try
		{
			var json = await File.ReadAllTextAsync(metadataPath);
			return JsonSerializer.Deserialize(json, GitHubJsonContext.Default.ModMetadata);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ModManagementService] Error reading metadata for {modFolder}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Enriches the mod list with information from ModDB tracking
	/// </summary>
	private void EnrichWithModDBInfo(List<InstalledMod> allMods, List<InstalledModInfo> moddbMods)
	{
		foreach (var mod in allMods)
		{
			// Try to find matching ModDB entry by checking if any installed paths contain this mod folder
			var moddbEntry = moddbMods.FirstOrDefault(m => 
				m.InstalledPaths.Any(p => p.Contains(mod.FolderName, StringComparison.OrdinalIgnoreCase)));

			if (moddbEntry != null && !mod.IsFromModDB)
			{
				// Enrich with ModDB info
				mod.IsFromModDB = true;
				mod.ModDBPageUrl = moddbEntry.ModPageUrl;
				mod.InstallDate = moddbEntry.InstallDate;
			}
		}
	}

	/// <summary>
	/// Saves metadata to mod_info.json in the mod folder
	/// </summary>
	public async Task SaveModMetadataAsync(string modFolderName, ModMetadata metadata)
	{
		// Try both locations
		var enabledPath = Path.Combine(_modsPath, modFolderName);
		var disabledPath = Path.Combine(_disabledModsPath, modFolderName);

		string? modPath = null;
		if (Directory.Exists(enabledPath))
		{
			modPath = enabledPath;
		}
		else if (Directory.Exists(disabledPath))
		{
			modPath = disabledPath;
		}

		if (modPath == null)
		{
			throw new DirectoryNotFoundException($"Mod folder not found: {modFolderName}");
		}

		var metadataPath = Path.Combine(modPath, "mod_info.json");
		var json = JsonSerializer.Serialize(metadata, GitHubJsonContext.Default.ModMetadata);
		await File.WriteAllTextAsync(metadataPath, json);

		Debug.WriteLine($"[ModManagementService] Saved metadata for mod: {modFolderName}");
	}
}
