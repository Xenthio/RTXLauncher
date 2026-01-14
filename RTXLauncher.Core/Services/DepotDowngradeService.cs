using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Service for downgrading Garry's Mod to a legacy build using DepotDownloader.
/// </summary>
public class DepotDowngradeService
{
    // Known depot information for Garry's Mod legacy build
    private const int GARRYSMOD_APP_ID = 4000;
    private const int GARRYSMOD_DEPOT_ID = 4002;
    private const string LEGACY_BETA = "x86-64";
    private readonly DepotDownloaderAdapter _depotDownloaderAdapter = new();

    /// <summary>
    /// Downloads a legacy depot of Garry's Mod using in-process DepotDownloader.
    /// </summary>
    /// <param name="request">Download request details</param>
    /// <param name="progress">Progress reporter for UI updates</param>
    /// <param name="authUi">Steam auth UI handler</param>
    /// <returns>The path where the depot was downloaded</returns>
    public async Task<string> DownloadLegacyDepotAsync(
        DepotDownloadRequest request,
        IProgress<InstallProgressReport> progress,
        IDepotDownloaderAuthUi authUi)
    {
        return await Task.Run(async () =>
        {
            try
            {
                progress.Report(new InstallProgressReport
                {
                    Message = "Preparing depot download...",
                    Percentage = 5
                });

                var cacheRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RTXLauncher",
                    "depot-cache");
                var outputPath = Path.Combine(cacheRoot, $"gmod-legacy-{request.ManifestId}");

                var adapterRequest = new DepotDownloadRequest
                {
                    AppId = GARRYSMOD_APP_ID,
                    DepotId = GARRYSMOD_DEPOT_ID,
                    ManifestId = request.ManifestId,
                    Branch = LEGACY_BETA,
                    OutputPath = outputPath,
                    Username = request.Username,
                    Password = request.Password,
                    UseQrCode = request.UseQrCode,
                    RememberPassword = request.RememberPassword,
                    SkipAppConfirmation = request.SkipAppConfirmation,
                    MaxDownloads = request.MaxDownloads,
                    OperatingSystem = request.OperatingSystem,
                    Architecture = request.Architecture,
                    Language = request.Language,
                    LowViolence = request.LowViolence
                };

                await _depotDownloaderAdapter.DownloadLegacyDepotAsync(adapterRequest, progress, authUi);

                progress.Report(new InstallProgressReport
                {
                    Message = "Depot download completed. Verifying files...",
                    Percentage = 90
                });

                if (!File.Exists(Path.Combine(outputPath, "gmod.exe")) &&
                    !File.Exists(Path.Combine(outputPath, "hl2.exe")))
                {
                    throw new DirectoryNotFoundException($"Could not find game executables in depot path: {outputPath}");
                }

                var preservedDepotPath = Path.Combine(cacheRoot, "preserved_depot");
                if (Directory.Exists(preservedDepotPath))
                {
                    Directory.Delete(preservedDepotPath, recursive: true);
                }

                progress.Report(new InstallProgressReport
                {
                    Message = "Preserving depot for future patch restoration...",
                    Percentage = 95
                });

                CopyDirectory(outputPath, preservedDepotPath);

                progress.Report(new InstallProgressReport
                {
                    Message = "Depot successfully downloaded and preserved",
                    Percentage = 100
                });

                return preservedDepotPath;
            }
            catch (Exception ex)
            {
                progress.Report(new InstallProgressReport
                {
                    Message = $"Error during depot download: {ex.Message}",
                    Percentage = 100
                });
                throw;
            }
        });
    }

    /// <summary>
    /// Copies the downloaded depot files to the current RTX installation.
    /// </summary>
    /// <param name="depotPath">Path to the downloaded depot</param>
    /// <param name="targetPath">Path to the RTX installation</param>
    /// <param name="progress">Progress reporter</param>
    public async Task ApplyDepotToInstallationAsync(
        string depotPath,
        string targetPath,
        IProgress<InstallProgressReport> progress)
    {
        await Task.Run(() =>
        {
            progress.Report(new InstallProgressReport
            {
                Message = "Preparing to apply depot files...",
                Percentage = 5
            });

            if (!Directory.Exists(depotPath))
            {
                throw new DirectoryNotFoundException($"Depot path not found: {depotPath}");
            }

            if (!Directory.Exists(targetPath))
            {
                throw new DirectoryNotFoundException($"Target installation not found: {targetPath}");
            }

            // Backup current files
            progress.Report(new InstallProgressReport
            {
                Message = "Creating backup of current installation...",
                Percentage = 10
            });

            var backupPath = Path.Combine(targetPath, "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupPath);

            // Get all items from the depot to copy
            var depotItems = new List<string>();

            // Add all directories
            foreach (var dir in Directory.GetDirectories(depotPath))
            {
                depotItems.Add(Path.GetFileName(dir));
            }

            // Add all files
            foreach (var file in Directory.GetFiles(depotPath))
            {
                depotItems.Add(Path.GetFileName(file));
            }

            progress.Report(new InstallProgressReport
            {
                Message = $"Found {depotItems.Count} items to copy from depot...",
                Percentage = 15
            });

            // Backup existing items that will be replaced
            foreach (var item in depotItems)
            {
                var targetItemPath = Path.Combine(targetPath, item);
                var backupItemPath = Path.Combine(backupPath, item);

                if (Directory.Exists(targetItemPath))
                {
                    progress.Report(new InstallProgressReport
                    {
                        Message = $"Backing up {item}...",
                        Percentage = 20
                    });

                    CopyDirectory(targetItemPath, backupItemPath);
                }
                else if (File.Exists(targetItemPath))
                {
                    File.Copy(targetItemPath, backupItemPath, overwrite: true);
                }
            }

            // Copy ALL depot files to installation
            progress.Report(new InstallProgressReport
            {
                Message = "Applying depot files to installation...",
                Percentage = 50
            });

            foreach (var item in depotItems)
            {
                var sourcePath = Path.Combine(depotPath, item);
                var targetItemPath = Path.Combine(targetPath, item);

                if (Directory.Exists(sourcePath))
                {
                    progress.Report(new InstallProgressReport
                    {
                        Message = $"Copying directory {item}...",
                        Percentage = 60
                    });

                    if (Directory.Exists(targetItemPath))
                    {
                        // Delete existing
                        Directory.Delete(targetItemPath, recursive: true);
                    }

                    CopyDirectory(sourcePath, targetItemPath);
                }
                else if (File.Exists(sourcePath))
                {
                    progress.Report(new InstallProgressReport
                    {
                        Message = $"Copying file {item}...",
                        Percentage = 60
                    });

                    if (File.Exists(targetItemPath))
                    {
                        File.Delete(targetItemPath);
                    }

                    File.Copy(sourcePath, targetItemPath);
                }
            }

            progress.Report(new InstallProgressReport
            {
                Message = $"Downgrade completed successfully! Backup saved to: {backupPath}",
                Percentage = 100
            });
        });
    }

    private void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var destDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(directory, destDir);
        }
    }

    /// <summary>
    /// Restores the bin folder from the preserved downgraded depot
    /// This should be called before reapplying patches to ensure clean unpatched binaries
    /// </summary>
    public async Task RestoreBinariesFromDepotAsync(
        string depotPath,
        string targetPath,
        IProgress<InstallProgressReport> progress)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(depotPath))
            {
                throw new DirectoryNotFoundException($"Preserved depot not found: {depotPath}");
            }

            if (!Directory.Exists(targetPath))
            {
                throw new DirectoryNotFoundException($"Target installation not found: {targetPath}");
            }

            progress.Report(new InstallProgressReport
            {
                Message = "Restoring original binaries from depot...",
                Percentage = 10
            });

            // Restore bin folder
            var sourceBinPath = Path.Combine(depotPath, "bin");
            var targetBinPath = Path.Combine(targetPath, "bin");

            if (Directory.Exists(sourceBinPath))
            {
                progress.Report(new InstallProgressReport
                {
                    Message = "Restoring bin folder...",
                    Percentage = 50
                });

                if (Directory.Exists(targetBinPath))
                {
                    Directory.Delete(targetBinPath, recursive: true);
                }

                CopyDirectory(sourceBinPath, targetBinPath);
            }

            progress.Report(new InstallProgressReport
            {
                Message = "Binaries restored successfully",
                Percentage = 100
            });
        });
    }
}
