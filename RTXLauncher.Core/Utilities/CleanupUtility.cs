// Utilities/CleanupUtility.cs

using System.Text;
using System.Text.RegularExpressions;

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
	/// Performs post-installation cleanup by removing outdated config entries.
	/// This should be called AFTER all files are extracted.
	/// </summary>
	/// <param name="installPath">The root installation directory</param>
	/// <returns>A list of cleanup messages describing what was done</returns>
	public static List<string> PerformPostInstallCleanup(string installPath)
	{
		var messages = new List<string>();

		if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
		{
			messages.Add("Post-install cleanup skipped: Invalid installation path");
			return messages;
		}

		// Clean config files
		CleanConfigFile(Path.Combine(installPath, "garrysmod", "cfg", "server.vdf"), messages);
		CleanConfigFile(Path.Combine(installPath, "garrysmod", "cfg", "client.vdf"), messages);

		if (messages.Count == 0)
		{
			messages.Add("No config entries to clean");
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

	/// <summary>
	/// Removes lines containing rtx_ or remix_ cvars from VDF config files
	/// </summary>
	private static void CleanConfigFile(string filePath, List<string> messages)
	{
		if (!File.Exists(filePath))
		{
			return; // File doesn't exist, nothing to clean
		}

		try
		{
			var lines = File.ReadAllLines(filePath);
			var cleanedLines = new List<string>();
			int removedCount = 0;

			foreach (var line in lines)
			{
				// Check if line contains rtx_ or remix_ (case insensitive)
				if (Regex.IsMatch(line, @"\b(rtx_|remix_)", RegexOptions.IgnoreCase))
				{
					removedCount++;
					continue; // Skip this line
				}
				cleanedLines.Add(line);
			}

			if (removedCount > 0)
			{
				// Write the cleaned content back
				File.WriteAllLines(filePath, cleanedLines, Encoding.UTF8);
				messages.Add($"Cleaned {removedCount} RTX/Remix cvar(s) from {Path.GetFileName(filePath)}");
			}
		}
		catch (Exception ex)
		{
			messages.Add($"Warning: Could not clean {Path.GetFileName(filePath)}: {ex.Message}");
		}
	}
}

