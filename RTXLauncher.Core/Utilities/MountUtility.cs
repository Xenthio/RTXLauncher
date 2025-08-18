// Utilities/MountingUtility.cs
namespace RTXLauncher.Core.Utilities;

public static class MountingUtility
{
	/// <summary>
	/// Checks if a game appears to be mounted by looking for its symbolic links.
	/// </summary>
	public static bool IsGameMounted(string gameFolder, string remixModFolder)
	{
		try
		{
			var gmodPath = GarrysModUtility.GetThisInstallFolder();
			var sourceContentMountPath = Path.Combine(gmodPath, "garrysmod", "addons", $"mount-{gameFolder}");
			var remixModMountPath = Path.Combine(gmodPath, "rtx-remix", "mods", $"mount-{gameFolder}-{remixModFolder}");

			// A game is considered mounted if its main addon symlink exists.
			// We don't need to check both, as they are created/deleted together.
			return Directory.Exists(sourceContentMountPath);
		}
		catch
		{
			// If any error occurs (e.g., permissions), assume it's not mounted.
			return false;
		}
	}
}