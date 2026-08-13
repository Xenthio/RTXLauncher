using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RTXLauncher.Core.Services;

public sealed record Hl2RtxMountLayout(
	string InstallPath,
	string SourceContentPath,
	string Hl2MiscVpkPath,
	string CompatibilityAddonPath,
	IReadOnlyList<string> CustomContentPaths);

public static class Hl2RtxMountService
{
	public const int ContractVersion = 1;
	public const string BasePathId = "rtxlauncher_hl2rtx";
	public const string Hl2MiscPathId = "rtxlauncher_hl2rtx_hl2_misc";
	public const string LamarrPathId = "rtxlauncher_hl2rtx_custom_lamarr_hack";
	public const string HandsPathId = "rtxlauncher_hl2rtx_custom_new_rtx_hands";
	public const string ManifestFileName = ".rtxlauncher-hl2rtx-overlay.json";

	private static readonly string[] GeneratedRoots = { "models", "materials", "maps" };
	private static readonly HashSet<string> ExcludedMaterialFolders = new(
		new[] { "vgui", "dev", "editor", "perftest", "tools" },
		StringComparer.OrdinalIgnoreCase);

	public static Hl2RtxMountLayout ValidateLayout(
		string installPath, string gmodPath)
	{
		var sourceContentPath = Path.Combine(installPath, "hl2rtx");
		var hl2MiscVpkPath = Path.Combine(
			installPath, "hl2", "hl2_misc_dir.vpk");
		var addonPath = Path.Combine(
			gmodPath, "garrysmod", "addons", "mount-hl2rtx");
		var contractPath = Path.Combine(
			addonPath, "rtxlauncher-mount-contract.json");

		if (!Directory.Exists(sourceContentPath))
		{
			throw new DirectoryNotFoundException(
				$"Half-Life 2 RTX content was not found at: {sourceContentPath}");
		}
		if (!File.Exists(hl2MiscVpkPath))
		{
			throw new FileNotFoundException(
				"Half-Life 2 RTX's HL2 model VPK is missing.", hl2MiscVpkPath);
		}
		if (!File.Exists(contractPath))
		{
			throw new InvalidOperationException(
				"The mount-hl2rtx addon is missing. Install or update the garrys-mod-rtx-remixed Fixes package before mounting Half-Life 2 RTX.");
		}

		using (var contract = JsonDocument.Parse(File.ReadAllText(contractPath)))
		{
			var root = contract.RootElement;
			if (!root.TryGetProperty("name", out var name) ||
				name.GetString() != "hl2rtx" ||
				!root.TryGetProperty("version", out var version) ||
				version.GetInt32() != ContractVersion)
			{
				throw new InvalidOperationException(
					$"The mount-hl2rtx addon contract is incompatible. Install the latest Fixes package (required contract version {ContractVersion}).");
			}
		}

		var customRoot = Path.Combine(sourceContentPath, "custom");
		var customPaths = Directory.Exists(customRoot)
			? Directory.GetDirectories(customRoot)
				.OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
				.ToArray()
			: Array.Empty<string>();

		RequireCustomFile(
			customPaths, "lamarr_hack", "models", "headcrabclassic.mdl");
		RequireCustomFile(
			customPaths, "new_rtx_hands", "models", "weapons", "v_crowbar.mdl");

		return new Hl2RtxMountLayout(
			installPath,
			sourceContentPath,
			hl2MiscVpkPath,
			addonPath,
			customPaths);
	}

	public static IReadOnlyList<KeyValuePair<string, string>> BuildMountEntries(
		Hl2RtxMountLayout layout)
	{
		var entries = new List<KeyValuePair<string, string>>();
		foreach (var customPath in layout.CustomContentPaths)
		{
			entries.Add(new KeyValuePair<string, string>(
				CustomPathId(Path.GetFileName(customPath)), customPath));
		}
		entries.Add(new KeyValuePair<string, string>(
			BasePathId, layout.SourceContentPath));
		entries.Add(new KeyValuePair<string, string>(
			Hl2MiscPathId, Path.GetDirectoryName(layout.Hl2MiscVpkPath)!));
		return entries;
	}

	public static string CustomPathId(string folderName)
	{
		var normalized = new StringBuilder();
		foreach (var character in folderName.ToLowerInvariant())
		{
			normalized.Append(char.IsLetterOrDigit(character) ? character : '_');
		}
		var component = normalized.ToString();
		if (component.Length == 0)
		{
			component = Convert.ToHexString(
				SHA256.HashData(Encoding.UTF8.GetBytes(folderName)))[..12].ToLowerInvariant();
		}
		return $"{BasePathId}_custom_{component}";
	}

	public static string BuildOverlay(Hl2RtxMountLayout layout, string stagingRoot)
	{
		Directory.CreateDirectory(stagingRoot);
		var sources = layout.CustomContentPaths
			.Concat(new[] { layout.SourceContentPath })
			.ToArray();

		foreach (var generatedRoot in GeneratedRoots)
		{
			var candidates = sources
				.Select(source => Path.Combine(source, generatedRoot))
				.Where(Directory.Exists)
				.ToArray();
			if (candidates.Length == 0)
			{
				continue;
			}

			var destination = Path.Combine(stagingRoot, generatedRoot);
			BuildMergedDirectory(
				generatedRoot, candidates, destination);
		}

		var manifestPath = Path.Combine(stagingRoot, ManifestFileName);
		var manifest = new
		{
			contractVersion = ContractVersion,
			installPath = layout.InstallPath,
			baseContentPath = layout.SourceContentPath,
			hl2MiscVpkPath = layout.Hl2MiscVpkPath,
			customContentPaths = layout.CustomContentPaths,
			generatedRoots = GeneratedRoots
		};
		File.WriteAllText(
			manifestPath,
			JsonSerializer.Serialize(manifest, new JsonSerializerOptions
			{
				WriteIndented = true
			}),
			new UTF8Encoding(false));
		return manifestPath;
	}

	public static bool IsMounted(string gmodPath)
	{
		var addonPath = Path.Combine(
			gmodPath, "garrysmod", "addons", "mount-hl2rtx");
		var manifestPath = Path.Combine(addonPath, ManifestFileName);
		var mountConfigPath = Path.Combine(
			gmodPath, "garrysmod", "cfg", "mount.cfg");
		if (!File.Exists(manifestPath) || !File.Exists(mountConfigPath))
		{
			return false;
		}

		try
		{
			using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
			if (!manifest.RootElement.TryGetProperty(
					"contractVersion", out var version) ||
				version.GetInt32() != ContractVersion)
			{
				return false;
			}

			var bytes = File.ReadAllBytes(mountConfigPath);
			var content = MountConfigService.Decode(bytes, out _);
			return MountConfigService.ContainsManagedEntry(content, BasePathId) &&
				MountConfigService.ContainsManagedEntry(content, Hl2MiscPathId) &&
				Directory.Exists(Path.Combine(addonPath, "models"));
		}
		catch
		{
			return false;
		}
	}

	public static IReadOnlyList<string> GetMountValidationErrors(
		Hl2RtxMountLayout layout, string remixMountPath,
		string mountConfigPath)
	{
		var errors = new List<string>();
		if (!Directory.Exists(layout.CompatibilityAddonPath))
		{
			errors.Add(
				$"Compatibility addon folder is missing: {layout.CompatibilityAddonPath}");
		}

		var manifestPath = Path.Combine(
			layout.CompatibilityAddonPath, ManifestFileName);
		if (!File.Exists(manifestPath))
		{
			errors.Add($"Overlay manifest is missing: {manifestPath}");
		}
		else
		{
			try
			{
				using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
				if (!manifest.RootElement.TryGetProperty(
						"contractVersion", out var version) ||
					version.GetInt32() != ContractVersion)
				{
					errors.Add(
						$"Overlay manifest has an incompatible contract version: {manifestPath}");
				}
			}
			catch (Exception ex)
			{
				errors.Add($"Overlay manifest could not be read: {ex.Message}");
			}
		}

		var modelsPath = Path.Combine(layout.CompatibilityAddonPath, "models");
		if (!Directory.Exists(modelsPath))
		{
			errors.Add($"Generated model overlay is missing: {modelsPath}");
		}
		else if (!Directory.EnumerateFileSystemEntries(modelsPath).Any())
		{
			errors.Add($"Generated model overlay is empty: {modelsPath}");
		}

		if (!Directory.Exists(remixMountPath))
		{
			errors.Add($"Remix mod mount folder is missing: {remixMountPath}");
		}
		else if (!Directory.EnumerateFileSystemEntries(remixMountPath).Any())
		{
			errors.Add($"Remix mod mount folder is empty: {remixMountPath}");
		}

		if (!File.Exists(mountConfigPath))
		{
			errors.Add($"mount.cfg is missing: {mountConfigPath}");
			return errors;
		}

		try
		{
			var bytes = File.ReadAllBytes(mountConfigPath);
			var content = MountConfigService.Decode(bytes, out _);
			foreach (var entry in BuildMountEntries(layout))
			{
				if (!MountConfigService.TryGetEntryValue(
						content, entry.Key, out var actualValue))
				{
					errors.Add($"mount.cfg entry is missing: {entry.Key}");
					continue;
				}

				if (!PathsEqual(actualValue, entry.Value))
				{
					errors.Add(
						$"mount.cfg entry {entry.Key} points to '{actualValue}' instead of '{entry.Value}'.");
				}
				if (!Directory.Exists(entry.Value))
				{
					errors.Add(
						$"Mounted source folder for {entry.Key} is missing: {entry.Value}");
				}
			}
		}
		catch (Exception ex)
		{
			errors.Add($"mount.cfg could not be validated: {ex.Message}");
		}

		return errors;
	}

	public static void RemoveGeneratedOverlay(string addonPath)
	{
		foreach (var generatedRoot in GeneratedRoots)
		{
			DeletePath(Path.Combine(addonPath, generatedRoot));
		}
		var manifestPath = Path.Combine(addonPath, ManifestFileName);
		if (File.Exists(manifestPath))
		{
			File.Delete(manifestPath);
		}
	}

	public static IReadOnlyList<string> GeneratedOverlayRoots => GeneratedRoots;

	private static void RequireCustomFile(
		IReadOnlyList<string> customPaths, string folderName,
		params string[] relativeParts)
	{
		var root = customPaths.FirstOrDefault(path =>
			string.Equals(Path.GetFileName(path), folderName,
				StringComparison.OrdinalIgnoreCase));
		var expectedPath = root;
		if (expectedPath != null)
		{
			foreach (var part in relativeParts)
			{
				expectedPath = Path.Combine(expectedPath, part);
			}
		}
		if (expectedPath == null || !File.Exists(expectedPath))
		{
			throw new InvalidOperationException(
				$"Half-Life 2 RTX custom content '{folderName}' is missing or incomplete. Verify the game installation before mounting.");
		}
	}

	private static void BuildMergedDirectory(
		string virtualPath, IReadOnlyList<string> candidates, string destination)
	{
		Directory.CreateDirectory(destination);
		var children = new Dictionary<string, List<FileSystemInfo>>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var candidate in candidates)
		{
			foreach (var childPath in Directory.EnumerateFileSystemEntries(candidate))
			{
				var child = Directory.Exists(childPath)
					? new DirectoryInfo(childPath) as FileSystemInfo
					: new FileInfo(childPath);
				if (!children.TryGetValue(child.Name, out var values))
				{
					values = new List<FileSystemInfo>();
					children.Add(child.Name, values);
				}
				values.Add(child);
			}
		}

		foreach (var childEntry in children.OrderBy(
			entry => entry.Key, StringComparer.OrdinalIgnoreCase))
		{
			var first = childEntry.Value[0];
			var childVirtualPath = virtualPath + "/" + first.Name;
			if (virtualPath.Equals("materials", StringComparison.OrdinalIgnoreCase) &&
				ExcludedMaterialFolders.Contains(first.Name))
			{
				continue;
			}

			var childDestination = Path.Combine(destination, first.Name);
			if (first is FileInfo)
			{
				File.CreateSymbolicLink(childDestination, first.FullName);
				continue;
			}

			var directories = childEntry.Value
				.OfType<DirectoryInfo>()
				.Select(directory => directory.FullName)
				.ToArray();
			if (directories.Length == 1)
			{
				Directory.CreateSymbolicLink(childDestination, directories[0]);
				continue;
			}

			BuildMergedDirectory(
				childVirtualPath, directories, childDestination);
		}
	}

	private static void DeletePath(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}

		var attributes = File.GetAttributes(path);
		Directory.Delete(path, (attributes & FileAttributes.ReparsePoint) == 0);
	}

	private static bool PathsEqual(string first, string second)
	{
		try
		{
			return string.Equals(
				Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar),
				Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar),
				StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
		}
	}
}
