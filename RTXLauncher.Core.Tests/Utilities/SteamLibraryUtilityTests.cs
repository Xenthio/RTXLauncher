using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Tests.Utilities;

public sealed class SteamLibraryUtilityTests : IDisposable
{
	private readonly string _testRoot = Path.Combine(
		Path.GetTempPath(), $"RTXLauncherSteamTests_{Guid.NewGuid():N}");

	public SteamLibraryUtilityTests()
	{
		Directory.CreateDirectory(_testRoot);
	}

	[Fact]
	public void FindsGameUsingExternalLibraryAndAppManifestInstallDirectory()
	{
		var externalLibrary = Path.Combine(_testRoot, "External Steam Library With Spaces");
		var steamApps = Path.Combine(externalLibrary, "steamapps");
		var gameDirectory = Path.Combine(steamApps, "common", "hl2rtx-custom-folder");
		Directory.CreateDirectory(Path.Combine(_testRoot, "steamapps"));
		Directory.CreateDirectory(gameDirectory);

		var escapedLibraryPath = externalLibrary.Replace("\\", "\\\\");
		File.WriteAllText(
			Path.Combine(_testRoot, "steamapps", "libraryfolders.vdf"),
			$"\"libraryfolders\"\n{{\n\t\"0\"\n\t{{\n\t\t\"path\"\t\"{escapedLibraryPath}\"\n\t}}\n}}\n");

		File.WriteAllText(
			Path.Combine(steamApps, "appmanifest_999999.acf"),
			"\"AppState\"\n{\n" +
			"\t\"name\"\t\"Half-Life 2 RTX\"\n" +
			"\t\"installdir\"\t\"hl2rtx-custom-folder\"\n" +
			"}\n");

		var result = SteamLibraryUtility.GetGameInstallFolder(
			"Half-Life 2 RTX", steamRootOverride: _testRoot);

		Assert.Equal(gameDirectory, result);
	}

	[Fact]
	public void PrefersManifestInstallDirectoryOverStaleConventionalFolder()
	{
		var steamApps = Path.Combine(_testRoot, "steamapps");
		var commonPath = Path.Combine(steamApps, "common");
		var actualGameDirectory = Path.Combine(commonPath, "hl2rtx-real-install");
		Directory.CreateDirectory(steamApps);
		Directory.CreateDirectory(Path.Combine(commonPath, "Half-Life 2 RTX"));
		Directory.CreateDirectory(actualGameDirectory);

		File.WriteAllText(
			Path.Combine(steamApps, "appmanifest_999999.acf"),
			"\"AppState\"\n{\n" +
			"\t\"name\"\t\"Half-Life 2: RTX\"\n" +
			"\t\"installdir\"\t\"hl2rtx-real-install\"\n" +
			"}\n");

		var result = SteamLibraryUtility.GetGameInstallFolder(
			"Half-Life 2 RTX", steamRootOverride: _testRoot);

		Assert.Equal(actualGameDirectory, result);
	}

	[Fact]
	public void ParsesLibraryFolderPathsWithVdfEscaping()
	{
		var content =
			"\"libraryfolders\"\n{\n" +
			"\t\"0\" { \"path\" \"/home/test/Steam\" }\n" +
			"\t\"1\" { \"path\" \"/run/media/test/My\\\\Steam\\\\Library\" }\n" +
			"}\n";

		var paths = SteamLibraryUtility.ParseLibraryFolderPaths(content);

		Assert.Equal(
			new[] { "/home/test/Steam", "/run/media/test/My\\Steam\\Library" },
			paths);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testRoot))
		{
			Directory.Delete(_testRoot, recursive: true);
		}
	}
}
