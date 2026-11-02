using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System.IO.Compression;
using System.Text;

namespace RTXLauncher.Core.Tests.Utilities;

public class ProxyUtilityTests : IDisposable
{
	private readonly string _testDirectory;
	private readonly string _win64;

	// What each DLL holds: the proxy names shaderapidx9_orig.dll, the game's own does not.
	private const string GameDll = "game shader api v1";
	private const string GameDllUpdated = "game shader api v2";
	private const string ProxyDll = "proxy, loading shaderapidx9_orig.dll";

	public ProxyUtilityTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherProxyTests_{Guid.NewGuid()}");
		_win64 = Path.Combine(_testDirectory, "install", "bin", "win64");
		Directory.CreateDirectory(_win64);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, true);
		}
	}

	private string Win64(string name) => Path.Combine(_win64, name);

	private string WriteProxy()
	{
		var path = Path.Combine(_testDirectory, "proxy", ProxyUtility.ProxyName);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, ProxyDll);
		return path;
	}

	[Fact]
	public void Deploy_MovesTheGamesDllToOrig()
	{
		File.WriteAllText(Win64(ProxyUtility.ProxyName), GameDll);

		ProxyUtility.Deploy(WriteProxy(), _win64);

		Assert.Equal(ProxyDll, File.ReadAllText(Win64(ProxyUtility.ProxyName)));
		Assert.Equal(GameDll, File.ReadAllText(Win64(ProxyUtility.OriginalName)));
		Assert.True(ProxyUtility.IsDeployed(_win64));
	}

	[Fact]
	public void Deploy_Again_KeepsTheOriginal()
	{
		File.WriteAllText(Win64(ProxyUtility.ProxyName), GameDll);
		var proxy = WriteProxy();
		ProxyUtility.Deploy(proxy, _win64);

		ProxyUtility.Deploy(proxy, _win64);

		Assert.Equal(GameDll, File.ReadAllText(Win64(ProxyUtility.OriginalName)));
	}

	[Fact]
	public void Deploy_AfterAGameUpdate_TakesTheUpdatedDllAsOrig()
	{
		File.WriteAllText(Win64(ProxyUtility.ProxyName), GameDll);
		var proxy = WriteProxy();
		ProxyUtility.Deploy(proxy, _win64);
		// A game update (or a file check) put the game's DLL back over the proxy.
		File.WriteAllText(Win64(ProxyUtility.ProxyName), GameDllUpdated);

		ProxyUtility.Deploy(proxy, _win64);

		Assert.Equal(GameDllUpdated, File.ReadAllText(Win64(ProxyUtility.OriginalName)));
		Assert.Equal(ProxyDll, File.ReadAllText(Win64(ProxyUtility.ProxyName)));
	}

	[Fact]
	public void Deploy_WithoutTheGamesDll_Throws()
	{
		Assert.Throws<FileNotFoundException>(() => ProxyUtility.Deploy(WriteProxy(), _win64));
	}

	[Fact]
	public void SetEnabled_SwapsTheGamesDllBackAndForth()
	{
		File.WriteAllText(Win64(ProxyUtility.ProxyName), GameDll);
		ProxyUtility.Deploy(WriteProxy(), _win64);

		ProxyUtility.SetEnabled(_win64, false);

		Assert.Equal(GameDll, File.ReadAllText(Win64(ProxyUtility.ProxyName)));
		Assert.False(File.Exists(Win64(ProxyUtility.OriginalName)));
		Assert.True(ProxyUtility.IsDisabled(_win64));
		Assert.False(ProxyUtility.IsDeployed(_win64));

		ProxyUtility.SetEnabled(_win64, true);

		Assert.Equal(ProxyDll, File.ReadAllText(Win64(ProxyUtility.ProxyName)));
		Assert.Equal(GameDll, File.ReadAllText(Win64(ProxyUtility.OriginalName)));
		Assert.False(ProxyUtility.IsDisabled(_win64));
	}

	[Fact]
	public void DisableLegacyRemix_RenamesD3d9()
	{
		File.WriteAllText(Win64("d3d9.dll"), "dxvk-remix");

		ProxyUtility.DisableLegacyRemix(_win64);

		Assert.False(File.Exists(Win64("d3d9.dll")));
		Assert.True(File.Exists(Win64("d3d9.dll.disabled")));
	}

	[Fact]
	public async Task InstallOpenRemixFromLocalZip_CopiesTheRuntimeIntoOpenremix()
	{
		var zipPath = Path.Combine(_testDirectory, "openremix-windows-x64.zip");
		using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
		{
			foreach (var name in new[] { "remix.dll", "usd/plugInfo.json" })
			{
				using var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.ASCII);
				writer.Write(name);
			}
		}
		File.WriteAllText(Win64("d3d9.dll"), "dxvk-remix");
		var installDir = Path.Combine(_testDirectory, "install");

		await new PackageInstallService().InstallOpenRemixFromLocalZipAsync(zipPath, installDir, new Progress<InstallProgressReport>());

		Assert.True(File.Exists(Path.Combine(_win64, "openremix", "remix.dll")));
		Assert.True(File.Exists(Path.Combine(_win64, "openremix", "usd", "plugInfo.json")));
		Assert.True(File.Exists(Win64("d3d9.dll.disabled")));
	}

	[Fact]
	public void SelectRelease_StableFallsBackToTheNightly()
	{
		var nightly = new GitHubRelease { TagName = "nightly", Prerelease = true, PublishedAt = DateTime.UtcNow };
		var releases = new List<GitHubRelease> { nightly };

		Assert.Same(nightly, PackageInstallService.SelectRelease(releases, ReleaseChannel.Nightly));
		Assert.Same(nightly, PackageInstallService.SelectRelease(releases, ReleaseChannel.Stable));

		var stable = new GitHubRelease { TagName = "v1.0", PublishedAt = DateTime.UtcNow.AddDays(-1) };
		releases.Add(stable);
		Assert.Same(stable, PackageInstallService.SelectRelease(releases, ReleaseChannel.Stable));
	}
}
