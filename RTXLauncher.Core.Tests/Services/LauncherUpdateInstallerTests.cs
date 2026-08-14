using RTXLauncher.Core.Services;

namespace RTXLauncher.Core.Tests.Services;

public sealed class LauncherUpdateInstallerTests : IDisposable
{
	private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherInstallerTests_{Guid.NewGuid():N}");

	public LauncherUpdateInstallerTests()
	{
		Directory.CreateDirectory(_testDirectory);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
			Directory.Delete(_testDirectory, recursive: true);
	}

	[Fact]
	public void ApplyUpdate_CopiesPreparedLauncherToInstallation()
	{
		string updateFolder = Path.Combine(_testDirectory, "prepared");
		string targetPath = Path.Combine(_testDirectory, "install", "RTXLauncher.Avalonia");
		Directory.CreateDirectory(updateFolder);
		File.WriteAllText(Path.Combine(updateFolder, "RTXLauncher.Avalonia"), "updated");
		File.WriteAllText(Path.Combine(updateFolder, "support.dat"), "supporting file");

		new LauncherUpdateInstaller().ApplyUpdate(updateFolder, targetPath);

		Assert.Equal("updated", File.ReadAllText(targetPath));
		Assert.Equal("supporting file", File.ReadAllText(Path.Combine(_testDirectory, "install", "support.dat")));
	}

	[Fact]
	public void CreateUpdaterScript_ContainsCrossProcessCopyAndRestartSteps()
	{
		string updateFolder = Path.Combine(_testDirectory, "prepared");
		string targetPath = Path.Combine(_testDirectory, "install", "RTXLauncher.Avalonia");
		Directory.CreateDirectory(updateFolder);
		File.WriteAllText(Path.Combine(updateFolder, "RTXLauncher.Avalonia"), "updated");

		string scriptPath = new LauncherUpdateInstaller().CreateUpdaterScript(updateFolder, targetPath);
		string script = File.ReadAllText(scriptPath);

		if (OperatingSystem.IsWindows())
		{
			Assert.Contains("xcopy", script, StringComparison.OrdinalIgnoreCase);
			Assert.Contains("start", script, StringComparison.OrdinalIgnoreCase);
		}
		else
		{
			Assert.Contains("cp -a", script, StringComparison.Ordinal);
			Assert.Contains("exec", script, StringComparison.Ordinal);
		}

		File.Delete(scriptPath);
	}
}
