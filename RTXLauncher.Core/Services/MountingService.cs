using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Services;

public class MountingService
{
	// We can reuse the SymlinkFailedException from the InstallService
	// or create a new, more specific one if desired.

	/// <summary>
	/// Creates all necessary symbolic links to mount a game's content.
	/// </summary>
	public async Task MountGameAsync(string gameName, string gameFolder, string installFolder, string remixModFolder, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			var installPath = SteamLibraryUtility.GetGameInstallFolder(installFolder);
			if (string.IsNullOrEmpty(installPath))
			{
				throw new DirectoryNotFoundException($"Could not find installation for {gameName}. Please ensure it is installed via Steam.");
			}

			var gmodPath = GarrysModUtility.GetThisInstallFolder();
			var sourceContentPath = Path.Combine(installPath, gameFolder);
			var remixModPath = Path.Combine(installPath, "rtx-remix", "mods", remixModFolder);

			if (gameFolder.Equals("hl2rtx", StringComparison.OrdinalIgnoreCase))
			{
				MountHl2Rtx(
					gameName, installPath, gmodPath, remixModPath,
					gameFolder, remixModFolder, progress);
				return;
			}

			if (!Directory.Exists(sourceContentPath))
			{
				throw new DirectoryNotFoundException($"Source content folder not found at: {sourceContentPath}");
			}

			// --- Create Mount Points ---
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", $"mount-{gameFolder}");
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", $"mount-{gameFolder}-{remixModFolder}");
			Directory.CreateDirectory(Path.Combine(gmodPath, "rtx-remix", "mods"));

			// --- Link Remix Mod ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} Remix content...", Percentage = 25 });
			if (Directory.Exists(remixModPath))
			{
				// Ensure a clean mount folder (remove old symlink or directory)
				if (Directory.Exists(remixModMountPath))
				{
					Directory.Delete(remixModMountPath, true);
				}
				Directory.CreateDirectory(remixModMountPath);

				// Copy only ROOT-level .usda files; symlink nested files (including nested .usda) // This is allows to overlay .usda fixes without replacing the orignal files. 
				MirrorRemixFolderWithUsdaCopies(remixModPath, remixModMountPath, true);
			}

			// --- Link Source Content ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} Source content...", Percentage = 50 });
			LinkSourceContent(sourceContentPath, sourceContentMountPath);

			// --- Link Custom Content ---
			progress.Report(new InstallProgressReport { Message = $"Mounting {gameName} custom content...", Percentage = 75 });
			var customPath = Path.Combine(sourceContentPath, "custom");
			if (Directory.Exists(customPath))
			{
				foreach (var folder in Directory.GetDirectories(customPath))
				{
					LinkSourceContent(folder, Path.Combine($"{sourceContentMountPath}-{Path.GetFileName(folder)}"));
				}
			}

			progress.Report(new InstallProgressReport { Message = $"{gameName} mounted successfully.", Percentage = 100 });
		});
	}

	/// <summary>
	/// Downloads the USDA fixes archive and overlays the HL2 RTX demo fixes into the
	/// mounted hl2rtx Remix mod folder (mount-hl2rtx-hl2rtx), overwriting copied .usda files.
	/// </summary>
	public async Task ApplyHl2UsdaFixesAsync(IProgress<InstallProgressReport> progress)
	{
		const string fixesUrl = "https://github.com/sambow23/rtx-usda-fixes/archive/refs/heads/main.zip";
		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncher_UsdFixes_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			progress.Report(new InstallProgressReport { Message = "Downloading USDA fixes...", Percentage = 5 });
			string zipPath = Path.Combine(tempDir, "usda_fixes.zip");
			using (var http = new HttpClient())
			{
				http.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher");
				using var resp = await http.GetAsync(fixesUrl, HttpCompletionOption.ResponseHeadersRead);
				resp.EnsureSuccessStatusCode();
				await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
				await resp.Content.CopyToAsync(fs);
			}

			progress.Report(new InstallProgressReport { Message = "Extracting fixes...", Percentage = 25 });
			string extractDir = Path.Combine(tempDir, "extracted");
			Directory.CreateDirectory(extractDir);
			System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

			// The archive structure: <root>/hl2rtxdemo/<files>
			string? hl2Root = Directory
				.GetDirectories(extractDir)
				.SelectMany(d => Directory.GetDirectories(d, "hl2rtxdemo", SearchOption.TopDirectoryOnly))
				.FirstOrDefault();
			if (string.IsNullOrEmpty(hl2Root) || !Directory.Exists(hl2Root))
			{
				throw new DirectoryNotFoundException("Could not find 'hl2rtxdemo' in the fixes archive.");
			}

			// Destination is the mounted hl2rtx Remix mod path we create during mount
			var gmodPath = GarrysModUtility.GetThisInstallFolder();
			string destMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", "mount-hl2rtx-hl2rtx");
			if (!Directory.Exists(destMountPath))
			{
				throw new DirectoryNotFoundException("Please mount 'Half-Life 2: RTX' first. The mount folder was not found.");
			}

			progress.Report(new InstallProgressReport { Message = "Applying fixes...", Percentage = 50 });
			await Task.Run(() => CopyDirectoryOverwrite(hl2Root, destMountPath));

			progress.Report(new InstallProgressReport { Message = "USDA fixes applied successfully.", Percentage = 100 });
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { /* ignore cleanup errors */ }
		}
	}

	private static void CopyDirectoryOverwrite(string sourceDir, string destDir)
	{
		Directory.CreateDirectory(destDir);
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var name = Path.GetFileName(file);
			var dest = Path.Combine(destDir, name);
			File.Copy(file, dest, true);
		}
		foreach (var dir in Directory.GetDirectories(sourceDir))
		{
			var name = Path.GetFileName(dir);
			CopyDirectoryOverwrite(dir, Path.Combine(destDir, name));
		}
	}

	/// <summary>
	/// Removes all symbolic links for a mounted game.
	/// </summary>
	public void UnmountGame(string gameFolder, string remixModFolder)
	{
		var gmodPath = GarrysModUtility.GetThisInstallFolder();
		if (gameFolder.Equals("hl2rtx", StringComparison.OrdinalIgnoreCase))
		{
			UnmountHl2Rtx(gmodPath, gameFolder, remixModFolder);
			return;
		}

		var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", $"mount-{gameFolder}");
		var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", $"mount-{gameFolder}-{remixModFolder}");

		if (Directory.Exists(remixModMountPath)) Directory.Delete(remixModMountPath, true);
		if (Directory.Exists(sourceContentMountPath)) Directory.Delete(sourceContentMountPath, true);

		// Unmount custom folders
		var customAddonsPath = Path.Combine(gmodPath, "garrysmod", "addons");
		if (Directory.Exists(customAddonsPath))
		{
			foreach (var directory in Directory.GetDirectories(customAddonsPath, $"mount-{gameFolder}-*"))
			{
				Directory.Delete(directory, true);
			}
		}
	}

	private void MountHl2Rtx(
		string gameName, string installPath, string gmodPath,
		string remixModPath, string gameFolder, string remixModFolder,
		IProgress<InstallProgressReport> progress)
	{
		var log = Hl2RtxMountLogger.Create("mount");
		log.Write(
			$"Launcher version: {typeof(MountingService).Assembly.GetName().Version}");
		log.Write($"Process: {Environment.ProcessPath}");
		log.Write($"OS: {Environment.OSVersion}; 64-bit process: {Environment.Is64BitProcess}");
		log.Write($"Steam installation: {installPath}");
		log.Write($"Garry's Mod RTX installation: {gmodPath}");
		log.Write($"Remix source mod: {remixModPath}");
		progress.Report(new InstallProgressReport
		{
			Message = $"[HL2 RTX] Diagnostic session {log.SessionId}: {log.LogPath}",
			Percentage = 1
		});
		if (!log.IsAvailable)
		{
			progress.Report(new InstallProgressReport
			{
				Message = $"[HL2 RTX] WARNING: Persistent log could not be opened: {log.LastWriteError}",
				Percentage = 1
			});
		}

		try
		{
			MountHl2RtxCore(
				gameName, installPath, gmodPath, remixModPath,
				gameFolder, remixModFolder, progress, log);
			log.Write("RESULT: mount succeeded");
		}
		catch (Exception ex)
		{
			log.WriteException("RESULT: mount failed", ex);
			throw;
		}
	}

	private void MountHl2RtxCore(
		string gameName, string installPath, string gmodPath,
		string remixModPath, string gameFolder, string remixModFolder,
		IProgress<InstallProgressReport> progress, Hl2RtxMountLogger log)
	{
		ReportHl2Rtx(progress, log,
			$"Validating {gameName} and the Fixes package...", 5);
		ReportHl2Rtx(progress, log,
			"Source content uses mount.cfg; separate mount-hl2rtx-* addon folders are intentionally not created.",
			6);
		var layout = Hl2RtxMountService.ValidateLayout(installPath, gmodPath);
		LogHl2RtxPathState(log, "HL2 RTX content", layout.SourceContentPath);
		LogHl2RtxPathState(log, "HL2 model content", Path.GetDirectoryName(layout.Hl2MiscVpkPath)!);
		LogHl2RtxPathState(log, "Compatibility addon", layout.CompatibilityAddonPath);
		foreach (var customPath in layout.CustomContentPaths)
		{
			LogHl2RtxPathState(log, "Custom content", customPath);
		}

		if (!Directory.Exists(remixModPath))
		{
			throw new DirectoryNotFoundException(
				$"Half-Life 2 RTX Remix mod was not found at: {remixModPath}");
		}
		LogHl2RtxPathState(log, "Remix source mod", remixModPath);
		var mountEntries = Hl2RtxMountService.BuildMountEntries(layout);
		foreach (var entry in mountEntries)
		{
			log.Write(
				$"Planned mount.cfg entry: {entry.Key} => {entry.Value} (exists={Directory.Exists(entry.Value)})");
		}

		var stagingRoot = Path.Combine(
			gmodPath, $".rtxlauncher-hl2rtx-{Guid.NewGuid():N}");
		var overlayStagingPath = Path.Combine(stagingRoot, "overlay");
		var remixStagingPath = Path.Combine(stagingRoot, "remix");
		var backupRoot = Path.Combine(stagingRoot, "backup");
		var addonBackupRoot = Path.Combine(backupRoot, "addon");
		var legacyBackupRoot = Path.Combine(backupRoot, "legacy");
		var remixBackupPath = Path.Combine(backupRoot, "remix");
		var remixMountPath = Path.Combine(
			gmodPath, "rtx-remix", "mods",
			$"mount-{gameFolder}-{remixModFolder}");
		var mountConfigPath = Path.Combine(
			gmodPath, "garrysmod", "cfg", "mount.cfg");
		var originalMountConfig = MountConfigService.ReadBytesOrDefault(
			mountConfigPath);
		var originalMountConfigExisted = File.Exists(mountConfigPath);
		var committedOverlayNames = new List<string>();
		var movedLegacyPaths = new List<(string Original, string Backup)>();
		var remixCommitted = false;
		var mountConfigCommitted = false;
		log.Write($"Transaction staging root: {stagingRoot}");
		log.Write(
			$"mount.cfg: {mountConfigPath} (existed={originalMountConfigExisted}, bytes={originalMountConfig.Length})");
		log.Write($"Remix destination: {remixMountPath}");

		Directory.CreateDirectory(stagingRoot);
		try
		{
			ReportHl2Rtx(progress, log,
				"Building the consolidated Half-Life 2 RTX content overlay...",
				20);
			Hl2RtxMountService.BuildOverlay(layout, overlayStagingPath);
			foreach (var name in Hl2RtxMountService.GeneratedOverlayRoots
				.Concat(new[] { Hl2RtxMountService.ManifestFileName }))
			{
				LogHl2RtxPathState(
					log, $"Staged overlay '{name}'",
					Path.Combine(overlayStagingPath, name));
			}

			ReportHl2Rtx(progress, log,
				"Preparing the Half-Life 2 RTX Remix mod...", 45);
			Directory.CreateDirectory(remixStagingPath);
			MirrorRemixFolderWithUsdaCopies(
				remixModPath, remixStagingPath, true);
			LogHl2RtxPathState(log, "Staged Remix mod", remixStagingPath);

			var configText = MountConfigService.Decode(
				originalMountConfig, out var configEncoding);
			var updatedConfigText = MountConfigService.UpdateManagedEntries(
				configText, mountEntries);
			var updatedMountConfig = MountConfigService.Encode(
				updatedConfigText, configEncoding);
			log.Write(
				$"Prepared mount.cfg update: encoding={configEncoding.EncodingName}, bytes={updatedMountConfig.Length}, managedEntries={mountEntries.Count}");

			ReportHl2Rtx(progress, log,
				"Activating the consolidated Half-Life 2 RTX mount...", 70);
			Directory.CreateDirectory(addonBackupRoot);
			foreach (var name in Hl2RtxMountService.GeneratedOverlayRoots
				.Concat(new[] { Hl2RtxMountService.ManifestFileName }))
			{
				var destination = Path.Combine(
					layout.CompatibilityAddonPath, name);
				var staged = Path.Combine(overlayStagingPath, name);
				var backup = Path.Combine(addonBackupRoot, name);
				if (PathExists(destination))
				{
					log.Write(
						$"Backing up existing compatibility path: {destination} => {backup}");
					MovePath(destination, backup);
				}
				if (PathExists(staged))
				{
					log.Write(
						$"Activating compatibility path: {staged} => {destination}");
					MovePath(staged, destination);
					committedOverlayNames.Add(name);
				}
				else
				{
					log.Write($"No staged '{name}' path was produced; skipping it.");
				}
			}

			Directory.CreateDirectory(Path.GetDirectoryName(remixMountPath)!);
			if (Directory.Exists(remixMountPath))
			{
				log.Write(
					$"Backing up existing Remix mount: {remixMountPath} => {remixBackupPath}");
				Directory.CreateDirectory(Path.GetDirectoryName(remixBackupPath)!);
				Directory.Move(remixMountPath, remixBackupPath);
			}
			log.Write(
				$"Activating Remix mount: {remixStagingPath} => {remixMountPath}");
			Directory.Move(remixStagingPath, remixMountPath);
			remixCommitted = true;

			MoveLegacyHl2RtxAddons(
				gmodPath, layout.CompatibilityAddonPath,
				legacyBackupRoot, movedLegacyPaths, log);

			MountConfigService.WriteAtomic(
				mountConfigPath, updatedMountConfig);
			mountConfigCommitted = true;
			log.Write($"Committed mount.cfg atomically: {mountConfigPath}");
			LogHl2RtxMountConfigState(
				log, mountConfigPath, mountEntries);

			ReportHl2Rtx(progress, log,
				"Verifying the activated Half-Life 2 RTX mount...", 90);
			LogHl2RtxPathState(
				log, "Activated compatibility addon", layout.CompatibilityAddonPath);
			foreach (var name in Hl2RtxMountService.GeneratedOverlayRoots
				.Concat(new[] { Hl2RtxMountService.ManifestFileName }))
			{
				LogHl2RtxPathState(
					log, $"Activated overlay '{name}'",
					Path.Combine(layout.CompatibilityAddonPath, name));
			}
			LogHl2RtxPathState(log, "Activated Remix mod", remixMountPath);
			var validationErrors = Hl2RtxMountService.GetMountValidationErrors(
				layout, remixMountPath, mountConfigPath);
			if (validationErrors.Count != 0)
			{
				foreach (var error in validationErrors)
				{
					log.Write($"VERIFY FAILED: {error}");
				}
				throw new InvalidOperationException(
					"Half-Life 2 RTX mount verification failed: " +
					string.Join(" ", validationErrors));
			}
			log.Write(
				$"VERIFY PASSED: compatibility addon, Remix mount, manifest, model overlay, and all {mountEntries.Count} mount.cfg entries are present.");
			ReportHl2Rtx(progress, log,
				$"Verification passed. Diagnostics: {log.LogPath}", 98);
			ReportHl2Rtx(progress, log,
				$"{gameName} mounted successfully.", 100);
		}
		catch (Exception ex)
		{
			log.Write(
				$"Mount transaction failed; rolling back (mountConfigCommitted={mountConfigCommitted}, remixCommitted={remixCommitted}, activatedOverlayCount={committedOverlayNames.Count}, movedLegacyCount={movedLegacyPaths.Count}).");
			log.WriteException("Transaction failure", ex);
			if (mountConfigCommitted)
			{
				if (originalMountConfigExisted)
				{
					MountConfigService.WriteAtomic(
						mountConfigPath, originalMountConfig);
				}
				else if (File.Exists(mountConfigPath))
				{
					File.Delete(mountConfigPath);
				}
			}

			foreach (var movedLegacyPath in movedLegacyPaths.AsEnumerable().Reverse())
			{
				if (PathExists(movedLegacyPath.Backup))
				{
					MovePath(movedLegacyPath.Backup, movedLegacyPath.Original);
				}
			}

			if (remixCommitted)
			{
				DeletePath(remixMountPath);
			}
			if (Directory.Exists(remixBackupPath))
			{
				Directory.Move(remixBackupPath, remixMountPath);
			}

			foreach (var name in committedOverlayNames)
			{
				DeletePath(Path.Combine(layout.CompatibilityAddonPath, name));
			}
			if (Directory.Exists(addonBackupRoot))
			{
				foreach (var backup in Directory.EnumerateFileSystemEntries(
					addonBackupRoot))
				{
					MovePath(
						backup,
						Path.Combine(layout.CompatibilityAddonPath,
							Path.GetFileName(backup)));
				}
			}
			log.Write("Rollback completed.");
			throw;
		}
		finally
		{
			DeletePath(stagingRoot);
			log.Write(
				$"Staging cleanup complete (exists={Directory.Exists(stagingRoot)}): {stagingRoot}");
		}
	}

	private static void UnmountHl2Rtx(
		string gmodPath, string gameFolder, string remixModFolder)
	{
		var mountConfigPath = Path.Combine(
			gmodPath, "garrysmod", "cfg", "mount.cfg");
		if (File.Exists(mountConfigPath))
		{
			var originalBytes = File.ReadAllBytes(mountConfigPath);
			var content = MountConfigService.Decode(
				originalBytes, out var encoding);
			var updated = MountConfigService.RemoveManagedEntries(content);
			MountConfigService.WriteAtomic(
				mountConfigPath,
				MountConfigService.Encode(updated, encoding));
		}

		var addonPath = Path.Combine(
			gmodPath, "garrysmod", "addons", "mount-hl2rtx");
		Hl2RtxMountService.RemoveGeneratedOverlay(addonPath);
		DeletePath(Path.Combine(
			gmodPath, "rtx-remix", "mods",
			$"mount-{gameFolder}-{remixModFolder}"));
		DeleteLegacyHl2RtxAddons(gmodPath, addonPath);
	}

	private static void MoveLegacyHl2RtxAddons(
		string gmodPath, string consolidatedAddonPath, string backupRoot,
		ICollection<(string Original, string Backup)> movedPaths,
		Hl2RtxMountLogger? log = null)
	{
		var addonsPath = Path.Combine(gmodPath, "garrysmod", "addons");
		if (!Directory.Exists(addonsPath))
		{
			return;
		}

		var legacyPaths = Directory.GetDirectories(addonsPath)
			.Where(path =>
				path.Equals(Path.Combine(addonsPath, "hl2rtx-model-compat"),
					StringComparison.OrdinalIgnoreCase) ||
				(Path.GetFileName(path).StartsWith(
					"mount-hl2rtx-", StringComparison.OrdinalIgnoreCase) &&
					!path.Equals(consolidatedAddonPath,
						StringComparison.OrdinalIgnoreCase)))
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		foreach (var legacyPath in legacyPaths)
		{
			Directory.CreateDirectory(backupRoot);
			var backup = Path.Combine(backupRoot, Path.GetFileName(legacyPath));
			log?.Write($"Moving legacy mount out of the active addon path: {legacyPath} => {backup}");
			Directory.Move(legacyPath, backup);
			movedPaths.Add((legacyPath, backup));
		}
		if (legacyPaths.Length == 0)
		{
			log?.Write("No legacy split HL2 RTX addon folders were present.");
		}
	}

	private static void DeleteLegacyHl2RtxAddons(
		string gmodPath, string consolidatedAddonPath)
	{
		var moved = new List<(string Original, string Backup)>();
		var temporaryRoot = Path.Combine(
			gmodPath, $".rtxlauncher-hl2rtx-cleanup-{Guid.NewGuid():N}");
		try
		{
			MoveLegacyHl2RtxAddons(
				gmodPath, consolidatedAddonPath, temporaryRoot, moved);
		}
		finally
		{
			DeletePath(temporaryRoot);
		}
	}

	private static bool PathExists(string path) =>
		File.Exists(path) || Directory.Exists(path);

	private static void ReportHl2Rtx(
		IProgress<InstallProgressReport> progress, Hl2RtxMountLogger log,
		string message, int percentage)
	{
		log.Write($"PROGRESS {percentage}%: {message}");
		progress.Report(new InstallProgressReport
		{
			Message = $"[HL2 RTX] {message}",
			Percentage = percentage
		});
	}

	private static void LogHl2RtxPathState(
		Hl2RtxMountLogger log, string label, string path)
	{
		try
		{
			var directoryExists = Directory.Exists(path);
			var fileExists = File.Exists(path);
			if (!directoryExists && !fileExists)
			{
				log.Write($"PATH {label}: MISSING | {path}");
				return;
			}

			FileSystemInfo item = directoryExists
				? new DirectoryInfo(path)
				: new FileInfo(path);
			var isReparsePoint = item.Attributes.HasFlag(FileAttributes.ReparsePoint);
			var linkTarget = item.LinkTarget;
			var itemCount = directoryExists
				? Directory.EnumerateFileSystemEntries(path).Take(10001).Count()
				: 0;
			var countDescription = directoryExists
				? $", topLevelEntries={itemCount}{(itemCount > 10000 ? "+" : string.Empty)}"
				: $", bytes={((FileInfo)item).Length}";
			log.Write(
				$"PATH {label}: PRESENT | {path} | kind={(directoryExists ? "directory" : "file")}, reparsePoint={isReparsePoint}, linkTarget={linkTarget ?? "<none>"}{countDescription}");
		}
		catch (Exception ex)
		{
			log.Write($"PATH {label}: ERROR | {path} | {ex.Message}");
		}
	}

	private static void LogHl2RtxMountConfigState(
		Hl2RtxMountLogger log, string mountConfigPath,
		IReadOnlyList<KeyValuePair<string, string>> expectedEntries)
	{
		try
		{
			var content = MountConfigService.Decode(
				File.ReadAllBytes(mountConfigPath), out var encoding);
			log.Write(
				$"mount.cfg readback: {mountConfigPath} (encoding={encoding.EncodingName}, characters={content.Length})");
			foreach (var expected in expectedEntries)
			{
				if (MountConfigService.TryGetEntryValue(
						content, expected.Key, out var actualValue))
				{
					log.Write(
						$"mount.cfg readback entry: {expected.Key} => {actualValue} (expected={expected.Value})");
				}
				else
				{
					log.Write(
						$"mount.cfg readback entry: {expected.Key} => <missing> (expected={expected.Value})");
				}
			}
		}
		catch (Exception ex)
		{
			log.WriteException("mount.cfg readback failed", ex);
		}
	}

	private static void MovePath(string source, string destination)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		if (File.Exists(source) && !Directory.Exists(source))
		{
			File.Move(source, destination);
		}
		else
		{
			Directory.Move(source, destination);
		}
	}

	private static void DeletePath(string path)
	{
		if (File.Exists(path) && !Directory.Exists(path))
		{
			File.Delete(path);
			return;
		}
		if (!Directory.Exists(path))
		{
			return;
		}

		var attributes = File.GetAttributes(path);
		Directory.Delete(path, (attributes & FileAttributes.ReparsePoint) == 0);
	}

	// --- Private Helper Methods (Ported directly from your old code) ---

	private void LinkSourceContent(string path, string destinationMountPath)
	{
		Directory.CreateDirectory(destinationMountPath);

		// Link models folder
		var modelsPath = Path.Combine(path, "models");
		if (Directory.Exists(modelsPath)) CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "models"), modelsPath);

		// Link maps folder
		var mapsPath = Path.Combine(path, "maps");
		if (Directory.Exists(mapsPath)) CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "maps"), mapsPath);

		// Link materials subfolders
		var materialsPath = Path.Combine(path, "materials");
		if (Directory.Exists(materialsPath))
		{
			Directory.CreateDirectory(Path.Combine(destinationMountPath, "materials"));
			var dontLink = new[] { "vgui", "dev", "editor", "perftest", "tools" };
			foreach (var folder in Directory.GetDirectories(materialsPath))
			{
				if (!dontLink.Contains(Path.GetFileName(folder)))
				{
					CreateDirectorySymbolicLink(Path.Combine(destinationMountPath, "materials", Path.GetFileName(folder)), folder);
				}
			}
		}
	}

	private void CreateDirectorySymbolicLink(string path, string pathToTarget)
	{
		try
		{
			Directory.CreateSymbolicLink(path, pathToTarget);
		}
		catch (IOException ex)
		{
			throw new SymlinkFailedException(
				"Failed to create directory symbolic link. This often requires Administrator privileges.",
				path,
				ex
			);
		}
	}

	private void CreateFileSymbolicLink(string path, string pathToTarget)
	{
		try
		{
			File.CreateSymbolicLink(path, pathToTarget);
		}
		catch (IOException ex)
		{
			throw new SymlinkFailedException(
				"Failed to create file symbolic link. This requires Administrator privileges.",
				path,
				ex
			);
		}
	}

	/// <summary>
	/// Recursively mirrors a Remix mod folder into the mount folder. Copies ONLY root-level
	/// .usda files and creates file symlinks for everything else (including nested .usda files).
	/// Directories are created normally (not symlinked) to allow selective file-level handling.
	/// </summary>
	private void MirrorRemixFolderWithUsdaCopies(string sourceDir, string destDir, bool isRoot)
	{
		// Create destination directory if missing
		Directory.CreateDirectory(destDir);

		// First, process files in this directory
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var fileName = Path.GetFileName(file);
			var destFile = Path.Combine(destDir, fileName);
			var ext = Path.GetExtension(file).ToLowerInvariant();

			if (ext == ".usda" && isRoot)
			{
				// Copy text-based USD ASCII files so users can edit without touching original game
				File.Copy(file, destFile, true);
			}
			else
			{
				// Symlink all other files
				CreateFileSymbolicLink(destFile, file);
			}
		}

		// Then recurse into subdirectories
		foreach (var subdir in Directory.GetDirectories(sourceDir))
		{
			var name = Path.GetFileName(subdir);
			var destSubdir = Path.Combine(destDir, name);
			MirrorRemixFolderWithUsdaCopies(subdir, destSubdir, false);
		}
	}
}
