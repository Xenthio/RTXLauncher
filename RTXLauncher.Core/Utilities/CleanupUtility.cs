// Utilities/CleanupUtility.cs

namespace RTXLauncher.Core.Utilities;

public static class CleanupUtility
{
	/// <summary>
	/// Performs pre-installation cleanup by removing outdated folders that might conflict with new files.
	/// This should be called BEFORE extracting new files.
	/// </summary>
	/// <param name="installPath">The root installation directory</param>
	/// <returns>A list of cleanup messages describing what was done</returns>
	public static List<string> PerformPreInstallCleanup(string installPath)
	{
		var messages = new List<string>();

		if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
		{
			messages.Add("Pre-install cleanup skipped: Invalid installation path");
			return messages;
		}

		messages.Add("Checking for outdated folders...");

		// Delete outdated folders
		DeleteFolderIfExists(Path.Combine(installPath, "garrysmod", "addons", "remixbinary"), messages);
		DeleteFolderIfExists(Path.Combine(installPath, "garrysmod", "data", "remix_map_configs"), messages);

		// Add a summary message if nothing was deleted
		int deletedCount = messages.Count(m => m.StartsWith("Deleted outdated folder:"));
		if (deletedCount == 0)
		{
			messages.Add("No outdated folders found (already clean)");
		}
		else
		{
			messages.Add($"Pre-cleanup complete: removed {deletedCount} folder(s)");
		}

		return messages;
	}

	/// <summary>
	/// Deletes a folder if it exists
	/// </summary>
	private static void DeleteFolderIfExists(string path, List<string> messages)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
				messages.Add($"Deleted outdated folder: {Path.GetFileName(path)}");
			}
		}
		catch (Exception ex)
		{
			messages.Add($"Warning: Could not delete {Path.GetFileName(path)}: {ex.Message}");
		}
	}
}

