using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO.Compression;
using System.Net.Http;

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
    private const string DEPOTDOWNLOADER_URL = "https://github.com/SteamRE/DepotDownloader/releases/latest/download/DepotDownloader-windows-x64.zip";
    
    /// <summary>
    /// Downloads a legacy depot of Garry's Mod using DepotDownloader.
    /// User will enter password and 2FA directly in DepotDownloader console.
    /// </summary>
    /// <param name="username">Steam username</param>
    /// <param name="manifestId">Steam depot manifest ID to download</param>
    /// <param name="progress">Progress reporter for UI updates</param>
    /// <returns>The path where the depot was downloaded</returns>
    public async Task<string> DownloadLegacyDepotAsync(
        string username,
        string manifestId,
        IProgress<InstallProgressReport> progress)
    {
        return await Task.Run(() => DownloadLegacyDepot(username, manifestId, progress));
    }
    
    private string DownloadLegacyDepot(
        string username,
        string manifestId,
        IProgress<InstallProgressReport> progress)
    {
        try
        {
            // Step 1: Ensure DepotDownloader is available
            progress.Report(new InstallProgressReport 
            { 
                Message = "Checking for DepotDownloader...", 
                Percentage = 5 
            });
            
            string depotDownloaderDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "depotdownloader");
            string depotDownloaderExe = Path.Combine(depotDownloaderDir, "DepotDownloader.exe");
            
            if (!File.Exists(depotDownloaderExe))
            {
                progress.Report(new InstallProgressReport 
                { 
                    Message = "Downloading DepotDownloader...", 
                    Percentage = 10 
                });
                
                DownloadAndExtractDepotDownloader(depotDownloaderDir, progress);
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Launching DepotDownloader...\n\nPlease enter your Steam password and 2FA code in the DepotDownloader console window.", 
                Percentage = 20 
            });
            
            // Step 2: Run DepotDownloader with visible console
            var outputPath = Path.Combine(depotDownloaderDir, "downloads");
            Directory.CreateDirectory(outputPath);
            
            string arguments = $"-app {GARRYSMOD_APP_ID} -depot {GARRYSMOD_DEPOT_ID} -manifest {manifestId} -beta {LEGACY_BETA} -username \"{username}\" -dir \"{outputPath}\"";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = depotDownloaderExe,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false, // Show the console window!
                WorkingDirectory = depotDownloaderDir
            };
            
            // Start the process
            using var process = new Process();
            process.StartInfo = startInfo;
            
            process.Start();
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "DepotDownloader is running. Please complete authentication in the console window...", 
                Percentage = 30 
            });
            
            // Wait for the process to exit (user handles everything in console)
            process.WaitForExit();
            
            // Check exit code
            if (process.ExitCode != 0)
            {
                throw new Exception($"DepotDownloader failed with exit code {process.ExitCode}. Please check the console output.");
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Depot download completed. Locating downloaded files...", 
                Percentage = 90 
            });
            
            // DepotDownloader puts game files directly in the downloads folder
            string depotPath = outputPath;
            
            // Verify game files exist
            if (!File.Exists(Path.Combine(depotPath, "gmod.exe")) && 
                !File.Exists(Path.Combine(depotPath, "hl2.exe")))
            {
                throw new DirectoryNotFoundException($"Could not find game executables in depot path: {depotPath}");
            }
            
            // Copy depot to a permanent preserved location for future restoration
            string preservedDepotPath = Path.Combine(depotDownloaderDir, "preserved_depot");
            if (Directory.Exists(preservedDepotPath))
            {
                Directory.Delete(preservedDepotPath, recursive: true);
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Preserving depot for future patch restoration...", 
                Percentage = 95 
            });
            
            CopyDirectory(depotPath, preservedDepotPath);
            
            progress.Report(new InstallProgressReport 
            { 
                Message = $"Depot successfully downloaded and preserved", 
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
    }
    
    private void DownloadAndExtractDepotDownloader(string targetDir, IProgress<InstallProgressReport> progress)
    {
        Directory.CreateDirectory(targetDir);
        string zipPath = Path.Combine(targetDir, "DepotDownloader.zip");
        
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                var response = httpClient.GetAsync(DEPOTDOWNLOADER_URL).Result;
                response.EnsureSuccessStatusCode();
                
                using (var fileStream = File.Create(zipPath))
                {
                    response.Content.CopyToAsync(fileStream).Wait();
                }
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Extracting DepotDownloader...", 
                Percentage = 15 
            });
            
            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
            File.Delete(zipPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download DepotDownloader: {ex.Message}", ex);
        }
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
