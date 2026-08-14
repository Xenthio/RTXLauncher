using System.Diagnostics;
using System.Text;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Applies a prepared update after the running launcher has exited.
/// </summary>
public sealed class LauncherUpdateInstaller
{
	public void ApplyUpdate(string updateFolder, string targetExecutablePath)
	{
		if (!Directory.Exists(updateFolder))
			throw new DirectoryNotFoundException($"Update content was not found at: {updateFolder}");

		if (!File.Exists(Path.Combine(updateFolder, Path.GetFileName(targetExecutablePath))))
			throw new FileNotFoundException("The prepared update does not contain the launcher executable.",
				Path.Combine(updateFolder, Path.GetFileName(targetExecutablePath)));

		string? targetDirectory = Path.GetDirectoryName(targetExecutablePath);
		if (string.IsNullOrWhiteSpace(targetDirectory))
			throw new ArgumentException("The target executable must include a directory.", nameof(targetExecutablePath));

		Directory.CreateDirectory(targetDirectory);
		foreach (var sourceFile in Directory.EnumerateFiles(updateFolder, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(updateFolder, sourceFile);
			string destinationPath = Path.Combine(targetDirectory, relativePath);
			string? destinationDirectory = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrEmpty(destinationDirectory))
				Directory.CreateDirectory(destinationDirectory);

			File.Copy(sourceFile, destinationPath, overwrite: true);
		}

		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(
				targetExecutablePath,
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
				UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
				UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
		}
	}

	public string CreateUpdaterScript(string updateFolder, string targetExecutablePath)
	{
		if (!Directory.Exists(updateFolder))
			throw new DirectoryNotFoundException($"Update content was not found at: {updateFolder}");

		string targetFileName = Path.GetFileName(targetExecutablePath);
		if (!File.Exists(Path.Combine(updateFolder, targetFileName)))
			throw new FileNotFoundException("The prepared update does not contain the launcher executable.",
				Path.Combine(updateFolder, targetFileName));

		string scriptDirectory = Path.Combine(Path.GetTempPath(), "RTXLauncherUpdater");
		Directory.CreateDirectory(scriptDirectory);

		string extension = OperatingSystem.IsWindows() ? ".cmd" : ".sh";
		string scriptPath = Path.Combine(scriptDirectory, $"apply-{Guid.NewGuid():N}{extension}");
		string script = OperatingSystem.IsWindows()
			? BuildWindowsScript(updateFolder, targetExecutablePath)
			: BuildUnixScript(updateFolder, targetExecutablePath);

		File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(
				scriptPath,
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
				UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
				UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
		}

		return scriptPath;
	}

	public void Launch(string scriptPath)
	{
		if (!File.Exists(scriptPath))
			throw new FileNotFoundException("The updater script was not found.", scriptPath);

		var processStartInfo = new ProcessStartInfo
		{
			UseShellExecute = OperatingSystem.IsWindows(),
			WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? AppContext.BaseDirectory
		};

		if (OperatingSystem.IsWindows())
		{
			processStartInfo.FileName = scriptPath;
		}
		else
		{
			processStartInfo.FileName = "/bin/sh";
			processStartInfo.ArgumentList.Add(scriptPath);
		}

		if (Process.Start(processStartInfo) == null)
			throw new InvalidOperationException("The updater process could not be started.");
	}

	private static string BuildWindowsScript(string updateFolder, string targetExecutablePath)
	{
		string targetDirectory = Path.GetDirectoryName(targetExecutablePath)!;
		return $"@echo off\r\n" +
			$"timeout /t 2 /nobreak >nul\r\n" +
			$"if not exist {QuoteWindows(targetDirectory)} mkdir {QuoteWindows(targetDirectory)}\r\n" +
			$"xcopy {QuoteWindows(Path.Combine(updateFolder, "*"))} {QuoteWindows(targetDirectory + "\\")} /E /I /Y /Q >nul\r\n" +
			$"start \"\" {QuoteWindows(targetExecutablePath)}\r\n" +
			$"rmdir /S /Q {QuoteWindows(updateFolder)}\r\n" +
			$"del \"%~f0\"\r\n";
	}

	private static string BuildUnixScript(string updateFolder, string targetExecutablePath)
	{
		string targetDirectory = Path.GetDirectoryName(targetExecutablePath)!;
		string quotedUpdateFolder = QuoteUnix(updateFolder);
		string quotedTargetDirectory = QuoteUnix(targetDirectory);
		string quotedTarget = QuoteUnix(targetExecutablePath);

		return "#!/bin/sh\n" +
			"set -eu\n" +
			"sleep 2\n" +
			$"mkdir -p {quotedTargetDirectory}\n" +
			$"cp -a {quotedUpdateFolder}/. {quotedTargetDirectory}/\n" +
			$"chmod +x {quotedTarget}\n" +
			$"rm -rf {quotedUpdateFolder}\n" +
			$"exec {quotedTarget}\n";
	}

	private static string QuoteWindows(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

	private static string QuoteUnix(string value) => $"'{value.Replace("'", "'\\''")}'";
}
