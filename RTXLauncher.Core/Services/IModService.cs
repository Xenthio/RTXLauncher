using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Services;

public interface IModService
{
	Task<List<ModInfo>> GetAllModsAsync();
}