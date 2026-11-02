using RTXLauncher.Core.Utilities;
using System.Security.Cryptography;
using System.Text.Json;

namespace RTXLauncher.Core.Tests.Utilities;

public class StartupProxyUtilityTests : IDisposable
{
	private readonly string root = Path.Combine(Path.GetTempPath(), "RTXStartupTests_" + Guid.NewGuid());
	private string Binary => Path.Combine(root, "bin", "win64");
	private string Package => Path.Combine(Binary, "astra-startup");
	private string Active => Path.Combine(Binary, "d3d9.dll");
	private string Original => Path.Combine(Binary, "d3d9_astra_renderer.dll");
	private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

	public StartupProxyUtilityTests()
	{
		Directory.CreateDirectory(Binary);
		Directory.CreateDirectory(Path.Combine(root, "garrysmod"));
		File.WriteAllText(Path.Combine(Binary, "gmod.exe"), "fixture");
	}
	private void Stage(string renderer = "renderer-one", string proxy = "proxy-one")
	{
		Directory.CreateDirectory(Package);
		File.WriteAllText(Path.Combine(Package, "d3d9.dll"), proxy);
		File.WriteAllText(Path.Combine(Package, "proxy_renderer_exports.json"), JsonSerializer.Serialize(new
		{
			version = 1, renderer_sha256 = Digest(renderer), renderer_bytes = renderer.Length,
			original_name = "d3d9_astra_renderer.dll"
		}));
	}
	[Fact]
	public void FreshInstallAndRepeatPreserveOriginalAndTimestamp()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one");
		Assert.Contains("installed:", StartupProxyUtility.Install(root));
		Assert.Equal("renderer-one", File.ReadAllText(Original));
		Assert.Equal("proxy-one", File.ReadAllText(Active));
		var stamp = File.GetLastWriteTimeUtc(Active);
		Assert.Contains("already installed", StartupProxyUtility.Install(root));
		Assert.Equal(stamp, File.GetLastWriteTimeUtc(Active));
	}
	[Fact]
	public void FixesCanBeInstalledBeforeRenderer()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage();
		Assert.Contains("waiting", StartupProxyUtility.Install(root));
		File.WriteAllText(Active, "renderer-one");
		Assert.Contains("installed:", StartupProxyUtility.Install(root));
	}
	[Fact]
	public void RendererUpdateReplacesOnlyManagedOriginal()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one"); StartupProxyUtility.Install(root);
		Stage("renderer-two", "proxy-two"); File.WriteAllText(Active, "renderer-two");
		StartupProxyUtility.Install(root);
		Assert.Equal("renderer-two", File.ReadAllText(Original));
		Assert.Equal("proxy-two", File.ReadAllText(Active));
	}
	[Fact]
	public void ProxyUpgradePreservesRenderer()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one"); StartupProxyUtility.Install(root);
		Stage(proxy: "proxy-two"); StartupProxyUtility.Install(root);
		Assert.Equal("renderer-one", File.ReadAllText(Original));
		Assert.Equal("proxy-two", File.ReadAllText(Active));
	}
	[Fact]
	public void UnsupportedRendererStaysUnwrapped()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "unsupported");
		Assert.Contains("NOT enabled", StartupProxyUtility.Install(root));
		Assert.Equal("unsupported", File.ReadAllText(Active));
		Assert.False(File.Exists(Original));
	}
	[Fact]
	public void ContractChangeRestoresOriginalUntilMatchingRendererArrives()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one"); StartupProxyUtility.Install(root);
		Stage("renderer-two", "proxy-two");
		Assert.Contains("NOT enabled", StartupProxyUtility.Install(root));
		Assert.Equal("renderer-one", File.ReadAllText(Active));
		File.WriteAllText(Active, "renderer-two"); StartupProxyUtility.Install(root);
		Assert.Equal("renderer-two", File.ReadAllText(Original));
		Assert.Equal("proxy-two", File.ReadAllText(Active));
	}
	[Fact]
	public void ChangedOriginalAndUnmanagedSiblingAreNotOverwritten()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one"); File.WriteAllText(Original, "user-file");
		Assert.Throws<IOException>(() => StartupProxyUtility.Install(root));
		Assert.Equal("user-file", File.ReadAllText(Original));
		File.Delete(Original); StartupProxyUtility.Install(root);
		File.WriteAllText(Original, "changed");
		Assert.Throws<InvalidDataException>(() => StartupProxyUtility.Install(root));
		Assert.Equal("proxy-one", File.ReadAllText(Active));
	}
	[Fact]
	public void DisabledRemixStaysDisabled()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active + ".disabled", "renderer-one"); StartupProxyUtility.Install(root);
		Assert.False(File.Exists(Active));
		Assert.Equal("proxy-one", File.ReadAllText(Active + ".disabled"));
		Assert.Equal("renderer-one", File.ReadAllText(Original));
	}
	[Fact]
	public void OlderPackagesAnd32BitInstallsStayUnchanged()
	{
		File.WriteAllText(Active, "renderer-one"); StartupProxyUtility.Install(root);
		Assert.Equal("renderer-one", File.ReadAllText(Active));
		Stage(); File.Delete(Path.Combine(Binary, "gmod.exe"));
		File.WriteAllText(Path.Combine(root, "gmod.exe"), "fixture"); StartupProxyUtility.Install(root);
		Assert.Equal("renderer-one", File.ReadAllText(Active));
		Assert.False(File.Exists(Original));
	}
	[Fact]
	public void IncompletePackageDoesNotChangeRenderer()
	{
		if (!OperatingSystem.IsWindows()) return;
		Stage(); File.WriteAllText(Active, "renderer-one");
		File.Delete(Path.Combine(Package, "d3d9.dll"));
		Assert.Throws<InvalidDataException>(() => StartupProxyUtility.Install(root));
		Assert.Equal("renderer-one", File.ReadAllText(Active));
	}
	public void Dispose() => Directory.Delete(root, recursive: true);
}
