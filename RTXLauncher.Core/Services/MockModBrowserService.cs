// Core/Services/ModBrowserService.cs
using RTXLauncher.Core.Models;

namespace RTXLauncher.Core.Services;

public class ModBrowserService
{
	// This method returns a list of simple data models, NOT ViewModels.
	public async Task<List<ModInfo>> GetAllModsAsync()
	{
		await Task.Delay(1500); // Simulate network delay

		return new List<ModInfo>
		{
			new() {
				Title = "HD PBR Vending Machines",
				Author = "AwesomeDev",
				Summary = "Replaces all vending machines with high-quality, physically-based rendered models.",
				ThumbnailUrl = "https://i.imgur.com/gTf8s4F.jpeg",
				IsInstalled = true
			},
			new() {
				Title = "Realistic Water Shaders",
				Author = "ShaderWizard",
				Summary = "A complete overhaul of all water surfaces for a more realistic look.",
				ThumbnailUrl = "https://i.imgur.com/rMUMiFz.jpeg"
			},
			new() {
				Title = "Ultimate Weapons Pack 1",
				Author = "ModelMan",
				Summary = "Re-textured Pistol, SMG, and Shotgun with 4K textures.",
				ThumbnailUrl = "https://i.imgur.com/sSPSsBE.jpeg"
			},
			new() {
				Title = "Combine Soldier Redux",
				Author = "AwesomeDev",
				Summary = "A modernized, high-poly version of the Combine Soldier.",
				ThumbnailUrl = "https://i.imgur.com/1y4hJ2m.jpeg",
				IsInstalled = true
			},
			new() {
				Title = "PBR Props Pack - Office",
				Author = "AssetFactory",
				Summary = "A collection of common office props (monitors, keyboards, etc.) with PBR materials.",
				ThumbnailUrl = "https://i.imgur.com/Qk7xUk3.jpeg"
			},
		};
	}
}