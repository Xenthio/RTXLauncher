using System.Text.Json;
using RTXLauncher.Core.Services;

namespace RTXLauncher.Core.Tests.Services;

public class Hl2RtxMountServiceTests : IDisposable
{
	private readonly string _testRoot = Path.Combine(
		Path.GetTempPath(), $"RTXLauncherHl2RtxTests_{Guid.NewGuid():N}");

	public void Dispose()
	{
		if (Directory.Exists(_testRoot))
		{
			Directory.Delete(_testRoot, true);
		}
	}

	[Fact]
	public void ValidateLayoutAndBuildMountEntries_UseStablePriorityOrder()
	{
		var (installPath, gmodPath) = CreateValidLayout();

		var layout = Hl2RtxMountService.ValidateLayout(installPath, gmodPath);
		var entries = Hl2RtxMountService.BuildMountEntries(layout);

		Assert.Equal(
			["lamarr_hack", "new_rtx_hands"],
			layout.CustomContentPaths.Select(Path.GetFileName));
		Assert.Equal(Hl2RtxMountService.LamarrPathId, entries[0].Key);
		Assert.Equal(Hl2RtxMountService.HandsPathId, entries[1].Key);
		Assert.Equal(Hl2RtxMountService.BasePathId, entries[^2].Key);
		Assert.Equal(Hl2RtxMountService.Hl2MiscPathId, entries[^1].Key);
		Assert.Equal(
			Path.Combine(installPath, "hl2"),
			entries[^1].Value);
	}

	[Fact]
	public void BuildOverlay_MergesDirectoriesAndPrefersCustomContent()
	{
		var (installPath, gmodPath) = CreateValidLayout();
		var baseContent = Path.Combine(installPath, "hl2rtx");
		var lamarr = Path.Combine(baseContent, "custom", "lamarr_hack");
		WriteFile(Path.Combine(baseContent, "models", "shared", "base.txt"), "base");
		WriteFile(Path.Combine(lamarr, "models", "shared", "custom.txt"), "custom");
		WriteFile(Path.Combine(baseContent, "models", "duplicate.txt"), "base");
		WriteFile(Path.Combine(lamarr, "models", "duplicate.txt"), "custom");
		WriteFile(Path.Combine(baseContent, "materials", "vgui", "ignored.vmt"), "ignored");
		WriteFile(Path.Combine(baseContent, "materials", "models", "kept.vmt"), "kept");

		var layout = Hl2RtxMountService.ValidateLayout(installPath, gmodPath);
		var overlay = Path.Combine(_testRoot, "overlay");
		var manifestPath = Hl2RtxMountService.BuildOverlay(layout, overlay);

		Assert.Equal("custom", File.ReadAllText(Path.Combine(overlay, "models", "duplicate.txt")));
		Assert.Equal("base", File.ReadAllText(Path.Combine(overlay, "models", "shared", "base.txt")));
		Assert.Equal("custom", File.ReadAllText(Path.Combine(overlay, "models", "shared", "custom.txt")));
		Assert.False(Directory.Exists(Path.Combine(overlay, "materials", "vgui")));
		Assert.Equal("kept", File.ReadAllText(Path.Combine(overlay, "materials", "models", "kept.vmt")));
		Assert.True(File.GetAttributes(Path.Combine(overlay, "models", "duplicate.txt"))
			.HasFlag(FileAttributes.ReparsePoint));

		using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
		Assert.Equal(
			Hl2RtxMountService.ContractVersion,
			manifest.RootElement.GetProperty("contractVersion").GetInt32());
	}

	[Fact]
	public void ValidateLayout_RejectsOutdatedCompatibilityAddon()
	{
		var (installPath, gmodPath) = CreateValidLayout();
		var contractPath = Path.Combine(
			gmodPath, "garrysmod", "addons", "mount-hl2rtx",
			"rtxlauncher-mount-contract.json");
		File.WriteAllText(contractPath, "{\"name\":\"hl2rtx\",\"version\":0}");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			Hl2RtxMountService.ValidateLayout(installPath, gmodPath));

		Assert.Contains("latest Fixes package", exception.Message);
	}

	[Fact]
	public void GetMountValidationErrors_AcceptsCompleteActivatedMount()
	{
		var (installPath, gmodPath) = CreateValidLayout();
		var layout = Hl2RtxMountService.ValidateLayout(installPath, gmodPath);
		Hl2RtxMountService.BuildOverlay(
			layout, layout.CompatibilityAddonPath);

		var remixMountPath = Path.Combine(
			gmodPath, "rtx-remix", "mods", "mount-hl2rtx-hl2rtx");
		WriteFile(Path.Combine(remixMountPath, "mod.usda"), "#usda 1.0");
		var mountConfigPath = Path.Combine(
			gmodPath, "garrysmod", "cfg", "mount.cfg");
		var mountConfig = MountConfigService.UpdateManagedEntries(
			"\"mountcfg\"\r\n{\r\n}\r\n",
			Hl2RtxMountService.BuildMountEntries(layout));
		WriteFile(mountConfigPath, mountConfig);

		var errors = Hl2RtxMountService.GetMountValidationErrors(
			layout, remixMountPath, mountConfigPath);

		Assert.Empty(errors);
	}

	[Fact]
	public void GetMountValidationErrors_ReportsMissingActivatedPaths()
	{
		var (installPath, gmodPath) = CreateValidLayout();
		var layout = Hl2RtxMountService.ValidateLayout(installPath, gmodPath);
		var remixMountPath = Path.Combine(
			gmodPath, "rtx-remix", "mods", "mount-hl2rtx-hl2rtx");
		var mountConfigPath = Path.Combine(
			gmodPath, "garrysmod", "cfg", "mount.cfg");

		var errors = Hl2RtxMountService.GetMountValidationErrors(
			layout, remixMountPath, mountConfigPath);

		Assert.Contains(errors, error => error.Contains("Overlay manifest"));
		Assert.Contains(errors, error => error.Contains("model overlay"));
		Assert.Contains(errors, error => error.Contains("Remix mod mount"));
		Assert.Contains(errors, error => error.Contains("mount.cfg"));
	}

	private (string InstallPath, string GmodPath) CreateValidLayout()
	{
		var installPath = Path.Combine(_testRoot, "Half-Life 2 RTX");
		var gmodPath = Path.Combine(_testRoot, "Game");
		WriteFile(Path.Combine(installPath, "hl2", "hl2_misc_dir.vpk"), "vpk");
		WriteFile(Path.Combine(
			installPath, "hl2rtx", "custom", "lamarr_hack",
			"models", "headcrabclassic.mdl"), "mdl");
		WriteFile(Path.Combine(
			installPath, "hl2rtx", "custom", "new_rtx_hands",
			"models", "weapons", "v_crowbar.mdl"), "mdl");
		WriteFile(Path.Combine(
			gmodPath, "garrysmod", "addons", "mount-hl2rtx",
			"rtxlauncher-mount-contract.json"),
			"{\"name\":\"hl2rtx\",\"version\":1}");
		return (installPath, gmodPath);
	}

	private static void WriteFile(string path, string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}
}
