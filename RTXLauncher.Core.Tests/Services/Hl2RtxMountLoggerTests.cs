using RTXLauncher.Core.Services;

namespace RTXLauncher.Core.Tests.Services;

public class Hl2RtxMountLoggerTests : IDisposable
{
	private readonly string _testRoot = Path.Combine(
		Path.GetTempPath(), $"RTXLauncherHl2RtxLogTests_{Guid.NewGuid():N}");

	public void Dispose()
	{
		if (Directory.Exists(_testRoot))
		{
			Directory.Delete(_testRoot, true);
		}
	}

	[Fact]
	public void Write_CreatesTimestampedSessionLog()
	{
		var logPath = Path.Combine(_testRoot, "logs", "hl2rtx-mount.log");
		var logger = new Hl2RtxMountLogger(logPath, "test mount");

		logger.Write("planned entry: rtxlauncher_hl2rtx => C:/HL2RTX");

		Assert.True(logger.IsAvailable);
		Assert.True(File.Exists(logPath));
		var contents = File.ReadAllText(logPath);
		Assert.Contains($"[{logger.SessionId}]", contents);
		Assert.Contains("BEGIN test mount", contents);
		Assert.Contains("planned entry: rtxlauncher_hl2rtx", contents);
	}
}
