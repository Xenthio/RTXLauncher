// Services/PackageInstallService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.IO.Compression;

namespace RTXLauncher.Core.Services;

public class PackageInstallService
{
	private readonly HttpClient _httpClient;


	public static Dictionary<string, (string Owner, string Repo)> RemixSources = new Dictionary<string, (string, string)>
	{
		{ "(OFFICIAL) NVIDIAGameWorks/rtx-remix", ("NVIDIAGameWorks", "rtx-remix") },
		{ "sambow23/dxvk-remix-gmod", ("sambow23", "dxvk-remix-gmod") },
	};

	public static Dictionary<string, (string Owner, string Repo, string InstallType)> PackageSources = new Dictionary<string, (string, string, string)>
	{
		{ "Xenthio/garrys-mod-rtx-remixed (Any)", ("Xenthio", "garrys-mod-rtx-remixed", "Any") },
		{ "Xenthio/RTXFixes (gmod_main)", ("Xenthio", "RTXFixes", "gmod_main") }
	};

	public static Dictionary<string, (string Owner, string Repo, string FilePath)> PatchSources = new Dictionary<string, (string, string, string)>
	{
		{ "BlueAmulet/SourceRTXTweaks", ("BlueAmulet", "SourceRTXTweaks", "applypatch.py") },
		{ "sambow23/SourceRTXTweaks", ("sambow23", "SourceRTXTweaks", "applypatch.py") },
		{ "Xenthio/SourceRTXTweaks (outdated, here to test multiple repos)", ("Xenthio", "SourceRTXTweaks", "applypatch.py") }
	};

	public static Dictionary<string, (string Owner, string Repo)> OptiScalerSources = new Dictionary<string, (string, string)>
	{
		{ "sambow23/OptiScaler-Releases", ("sambow23", "OptiScaler-Releases") }
	};

	public static readonly string DefaultIgnorePatterns =
@"
# We ignore these because the installer does rtx remix installations for us, 
# but some packages might contain them for easy manual installation.

# 32bit Bridge
bin/.trex/*
bin/d3d8to9.dll
bin/d3d9.dll
bin/LICENSE.txt
bin/NvRemixLauncher32.exe
bin/ThirdPartyLicenses-bridge.txt
bin/ThirdPartyLicenses-d3d8to9.txt
bin/ThirdPartyLicenses-dxvk.txt

# Remix in 64 install
bin/win64/usd/*
bin/win64/artifacts_readme.txt
bin/win64/cudart64_12.dll
bin/win64/d3d9.dll
bin/win64/d3d9.pdb
bin/win64/GFSDK_Aftermath_Lib.x64.dll
bin/win64/NRC_Vulkan.dll
bin/win64/NRD.dll
bin/win64/NvLowLatencyVk.dll
bin/win64/nvngx_dlss.dll
bin/win64/nvngx_dlssd.dll
bin/win64/nvngx_dlssg.dll
bin/win64/NvRemixBridge.exe
bin/win64/nvrtc64_120_0.dll
bin/win64/nvrtc-builtins64_125.dll
bin/win64/rtxio.dll
bin/win64/tbb.dll
bin/win64/tbbmalloc.dll
bin/win64/usd_ms.dll
";
	public PackageInstallService()
	{
		_httpClient = new HttpClient();
		// GitHub API can sometimes reject requests without a User-Agent
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher");
	}
	/// <summary>
	/// Installs RTX Remix, which has special logic for finding .trex/bin folders
	/// and placing them in the correct 32-bit or 64-bit game directory.
	/// </summary>
	public async Task InstallRemixPackageAsync(GitHubRelease release, string installDir, IProgress<InstallProgressReport> progress)
	{
		var asset = FindBestReleaseAsset(release);
		if (asset == null) throw new Exception("Could not find a suitable zip package in the release.");

		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncherRemix_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			// --- 1. Download ---
			string zipPath = Path.Combine(tempDir, asset.Name);
			var downloadProgress = new Progress<DownloadProgressReport>(report =>
			{
				int percentage = report.TotalBytes > 0 ? (int)((double)report.BytesDownloaded / report.TotalBytes * 40) : 20;
				progress.Report(new InstallProgressReport { Message = $"Downloading: {report.BytesDownloaded / 1048576}MB", Percentage = percentage });
			});
			await DownloadFileAsync(asset.BrowserDownloadUrl, zipPath, downloadProgress);

			// --- 2. Extract to a temporary location for inspection ---
			string extractTempDir = Path.Combine(tempDir, "extracted");
			Directory.CreateDirectory(extractTempDir);
			progress.Report(new InstallProgressReport { Message = "Extracting package for inspection...", Percentage = 45 });
			ZipFile.ExtractToDirectory(zipPath, extractTempDir);

			// --- 3. Determine correct source and destination paths ---
			progress.Report(new InstallProgressReport { Message = "Analyzing package structure...", Percentage = 55 });
			var installType = GarrysModUtility.GetInstallType(installDir);

			string? sourcePath = null;
			string? destPath = null;

			// Find the deepest .trex or bin folder in the extracted contents
			var trexFolder = Directory.GetDirectories(extractTempDir, "*.trex", SearchOption.AllDirectories).FirstOrDefault();
			var binFolder = Directory.GetDirectories(extractTempDir, "bin", SearchOption.AllDirectories).FirstOrDefault();

			if (installType == "gmod_x86-64" && trexFolder != null)
			{
				// Primary 64-bit case: .trex folder exists
				sourcePath = trexFolder;
				destPath = Path.Combine(installDir, "bin", "win64");
				progress.Report(new InstallProgressReport { Message = "Found .trex folder for 64-bit install.", Percentage = 60 });
			}
			else if (binFolder != null)
			{
				// 32-bit case or fallback for 64-bit: bin folder exists
				sourcePath = binFolder;
				destPath = (installType == "gmod_x86-64")
					? Path.Combine(installDir, "bin", "win64")
					: Path.Combine(installDir, "bin");
				progress.Report(new InstallProgressReport { Message = "Found bin folder for install.", Percentage = 60 });
			}
			else
			{
				throw new Exception("Remix package does not have a recognizable structure (missing 'bin' or '.trex' folder).");
			}

			Directory.CreateDirectory(destPath);

			// --- 4. Copy the files from the determined source to the destination ---
			var copyProgress = new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 60 + (int)(report.Percentage * 0.4) });
			});
			await CopyDirectoryWithProgress(sourcePath, destPath, true, copyProgress);

			progress.Report(new InstallProgressReport { Message = "Remix installed successfully!", Percentage = 100 });
		}
		finally
		{
			if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
		}
	}

	/// <summary>
	/// Installs a standard package (like Fixes) by extracting its contents directly into the install folder.
	/// </summary>
	public async Task InstallStandardPackageAsync(GitHubRelease release, string installDir, string defaultIgnorePatterns, IProgress<InstallProgressReport> progress)
	{
		// This method was previously named InstallGenericPackageAsync
		var asset = FindBestReleaseAsset(release);
		if (asset == null) throw new Exception("Could not find a suitable zip package in the release.");

		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncherPkg_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			string zipPath = Path.Combine(tempDir, asset.Name);

			var downloadProgress = new Progress<DownloadProgressReport>(report =>
			{
				int percentage = report.TotalBytes > 0 ? (int)((double)report.BytesDownloaded / report.TotalBytes * 50) : 25;
				progress.Report(new InstallProgressReport { Message = $"Downloading: {report.BytesDownloaded / 1048576}MB", Percentage = percentage });
			});
			await DownloadFileAsync(asset.BrowserDownloadUrl, zipPath, downloadProgress);

			var extractProgress = new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 50 + (report.Percentage / 2) });
			});
			await ExtractZipWithIgnoreAsync(zipPath, installDir, defaultIgnorePatterns, extractProgress);

			progress.Report(new InstallProgressReport { Message = "Installation complete!", Percentage = 100 });
		}
		finally
		{
			if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
		}
	}

	/// <summary>
	/// Installs the OptiScaler package, which has special logic for copying files based on game architecture.
	/// </summary>
	public async Task InstallOptiScalerPackageAsync(GitHubRelease release, string installDir, IProgress<InstallProgressReport> progress)
	{
		var asset = FindBestReleaseAsset(release);
		if (asset == null)
		{
			throw new Exception("Could not find a suitable zip package for OptiScaler.");
		}

		string tempDir = Path.Combine(Path.GetTempPath(), $"RTXLauncherOpti_{Path.GetRandomFileName()}");
		Directory.CreateDirectory(tempDir);

		try
		{
			string zipPath = Path.Combine(tempDir, asset.Name);
			string extractTempDir = Path.Combine(tempDir, "extracted");

			var downloadProgress = new Progress<DownloadProgressReport>(report =>
			{
				int percentage = report.TotalBytes > 0 ? (int)((double)report.BytesDownloaded / report.TotalBytes * 40) : 20;
				progress.Report(new InstallProgressReport { Message = $"Downloading: {report.BytesDownloaded / 1048576}MB", Percentage = percentage });
			});
			await DownloadFileAsync(asset.BrowserDownloadUrl, zipPath, downloadProgress);

			progress.Report(new InstallProgressReport { Message = "Extracting package for inspection...", Percentage = 45 });
			ZipFile.ExtractToDirectory(zipPath, extractTempDir);

			string trexFolderPath = Path.Combine(extractTempDir, "bin", ".trex");
			if (!Directory.Exists(trexFolderPath))
			{
				throw new Exception("Package does not contain the expected bin/.trex folder structure.");
			}

			string installType = GarrysModUtility.GetInstallType(installDir);
			bool isX64 = installType == "gmod_x86-64";

			string destPath = isX64 ? Path.Combine(installDir, "bin", "win64") : Path.Combine(installDir, "bin");
			string sourcePath = isX64 ? trexFolderPath : Path.Combine(extractTempDir, "bin");

			Directory.CreateDirectory(destPath);

			var copyProgress = new Progress<InstallProgressReport>(report =>
			{
				progress.Report(new InstallProgressReport { Message = report.Message, Percentage = 60 + (int)(report.Percentage * 0.4) });
			});

			await CopyDirectoryWithProgress(sourcePath, destPath, true, copyProgress);

			progress.Report(new InstallProgressReport { Message = "OptiScaler installed successfully!", Percentage = 100 });
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	// --- Private Helper Methods ---

	private async Task DownloadFileAsync(string url, string destinationPath, IProgress<DownloadProgressReport> progress)
	{
		using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		long totalBytes = response.Content.Headers.ContentLength ?? -1;
		using var downloadStream = await response.Content.ReadAsStreamAsync();
		using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

		long totalBytesRead = 0;
		var buffer = new byte[8192];
		int bytesRead;

		// --- THE CHANGE IS HERE ---

		// 1. Keep track of the last percentage we reported.
		int lastPercentageReported = -1;

		while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
		{
			await fileStream.WriteAsync(buffer, 0, bytesRead);
			totalBytesRead += bytesRead;

			if (totalBytes > 0)
			{
				// 2. Calculate the current percentage.
				int currentPercentage = (int)((double)totalBytesRead / totalBytes * 100);

				// 3. ONLY report progress if the percentage has changed since the last report.
				if (currentPercentage > lastPercentageReported)
				{
					lastPercentageReported = currentPercentage;
					progress.Report(new DownloadProgressReport
					{
						BytesDownloaded = totalBytesRead,
						TotalBytes = totalBytes,
						// We can also pass the percentage along in the report itself
						// if the receiver needs it for remapping.
						Percentage = currentPercentage
					});
				}
			}
			else
			{
				// For indeterminate downloads, we can't use percentage.
				// A time-based throttle could be used here if needed, but it's more complex.
				// For now, reporting on every read is acceptable as it's less common.
				progress.Report(new DownloadProgressReport { BytesDownloaded = totalBytesRead, TotalBytes = totalBytes });
			}
		}

		// Ensure the final 100% completion is always reported
		if (lastPercentageReported < 100)
		{
			progress.Report(new DownloadProgressReport { BytesDownloaded = totalBytes, TotalBytes = totalBytes, Percentage = 100 });
		}
	}

	private async Task ExtractZipWithIgnoreAsync(string zipPath, string installDir, string defaultIgnore, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			var ignoredPaths = ParseIgnorePatterns(defaultIgnore);

			using var zip = ZipFile.OpenRead(zipPath);
			var launcherIgnoreEntry = zip.Entries.FirstOrDefault(e => e.Name.Equals(".launcherignore", StringComparison.OrdinalIgnoreCase));
			if (launcherIgnoreEntry != null)
			{
				// This logic requires a temporary file write/read.
				// For simplicity in a single file, you could extract to a MemoryStream.
				using var reader = new StreamReader(launcherIgnoreEntry.Open());
				var customPatterns = ParseIgnorePatterns(reader.ReadToEnd());
				ignoredPaths.UnionWith(customPatterns);
			}

			var entriesToExtract = zip.Entries.Where(e => !ShouldIgnore(e.FullName, ignoredPaths)).ToList();
			int total = entriesToExtract.Count;
			int processed = 0;

			foreach (var entry in entriesToExtract)
			{
				string destPath = Path.Combine(installDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

				if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
				{
					Directory.CreateDirectory(destPath);
				}
				else
				{
					Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
					entry.ExtractToFile(destPath, true);
				}
				processed++;
				progress.Report(new InstallProgressReport { Message = $"Extracting: {entry.Name}", Percentage = (processed * 100) / total });
			}
		});
	}

	private async Task CopyDirectoryWithProgress(string sourceDir, string destDir, bool overwrite, IProgress<InstallProgressReport> progress)
	{
		await Task.Run(() =>
		{
			Directory.CreateDirectory(destDir);
			var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
			int total = allFiles.Length;
			int copied = 0;

			foreach (string sourceFile in allFiles)
			{
				string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
				string destFile = Path.Combine(destDir, relativePath);
				Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
				File.Copy(sourceFile, destFile, overwrite);
				copied++;
				progress.Report(new InstallProgressReport { Message = $"Copying: {Path.GetFileName(sourceFile)}", Percentage = (copied * 100) / total });
			}
		});
	}

	private GitHubAsset? FindBestReleaseAsset(GitHubRelease release)
	{
		string[] patterns = { "-gmod.zip", "-release.zip", ".zip" };
		foreach (var pattern in patterns)
		{
			var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) && !a.Name.Contains("-symbols"));
			if (asset != null) return asset;
		}
		return null;
	}

	private HashSet<string> ParseIgnorePatterns(string ignorePatterns)
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(ignorePatterns)) return result;

		foreach (var line in ignorePatterns.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var trimmedLine = line.Trim();
			if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("#"))
			{
				result.Add(trimmedLine.Replace('\\', '/').TrimStart('/'));
			}
		}
		return result;
	}

	private bool ShouldIgnore(string entryPath, HashSet<string> ignoredPaths)
	{
		string normalizedPath = entryPath.Replace('\\', '/').TrimStart('/');
		if (ignoredPaths.Contains(normalizedPath)) return true;

		foreach (var pattern in ignoredPaths.Where(p => p.EndsWith("/*")))
		{
			if (normalizedPath.StartsWith(pattern.Substring(0, pattern.Length - 1)))
			{
				return true;
			}
		}
		return false;
	}
}