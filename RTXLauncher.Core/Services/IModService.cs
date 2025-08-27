using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Services;

public interface IModService : IDisposable
{
	Task<List<ModInfo>> GetAllModsAsync();

	// New methods for downloading
	Task<List<ModFile>> GetFilesForModAsync(ModInfo mod);
	Task<ModFile> GetFileDetailsAndUrlAsync(ModFile file);
	Task DownloadFileAsync(ModFile file, string destinationPath, IProgress<double> progress);
	/// <summary>
	/// Handles the entire installation process for a mod file, including download,
	/// installation via AddonInstallService, and persisting the installed state.
	/// </summary>
	Task InstallModFileAsync(ModInfo mod, ModFile file, Func<string, Task<bool>> confirmationProvider, IProgress<InstallProgressReport> progress);

}