using System.Text;

namespace RTXLauncher.Core.Services;

public sealed class Hl2RtxMountLogger
{
	private static readonly object WriteLock = new();
	private static readonly UTF8Encoding Utf8NoBom = new(false);

	public Hl2RtxMountLogger(string logPath, string operation)
	{
		LogPath = logPath;
		SessionId = Guid.NewGuid().ToString("N")[..8];
		Write($"BEGIN {operation}");
	}

	public string LogPath { get; }
	public string SessionId { get; }
	public string? LastWriteError { get; private set; }
	public bool IsAvailable => LastWriteError == null;

	public static Hl2RtxMountLogger Create(string operation)
	{
		var logPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"RTXLauncher", "logs", "hl2rtx-mount.log");
		return new Hl2RtxMountLogger(logPath, operation);
	}

	public void Write(string message)
	{
		var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
		var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
		var output = new StringBuilder();
		foreach (var line in normalized.Split('\n'))
		{
			output.Append('[').Append(timestamp).Append("] [")
				.Append(SessionId).Append("] ")
				.AppendLine(line);
		}

		try
		{
			lock (WriteLock)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
				File.AppendAllText(LogPath, output.ToString(), Utf8NoBom);
			}
			LastWriteError = null;
		}
		catch (Exception ex)
		{
			LastWriteError = ex.Message;
		}
	}

	public void WriteException(string context, Exception exception)
	{
		Write($"{context}: {exception}");
	}
}
