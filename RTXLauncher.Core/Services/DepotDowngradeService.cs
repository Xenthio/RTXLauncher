using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RTXLauncher.Core.Services;

/// <summary>
/// Service for downgrading Garry's Mod to a legacy build using Steam depot downloads.
/// </summary>
public class DepotDowngradeService
{
    // Known depot information for Garry's Mod legacy build
    private const int GARRYSMOD_APP_ID = 4000;
    private const int GARRYSMOD_DEPOT_ID = 4002;
    private const string LEGACY_MANIFEST_ID = "2195078592256565401";
    
    /// <summary>
    /// Downloads a legacy depot of Garry's Mod using the Steam console.
    /// </summary>
    /// <param name="progress">Progress reporter for UI updates</param>
    /// <param name="customSteamPath">Optional custom Steam installation path</param>
    /// <returns>The path where the depot was downloaded</returns>
    public async Task<string> DownloadLegacyDepotAsync(
        IProgress<InstallProgressReport> progress,
        string? customSteamPath = null)
    {
        return await Task.Run(() => DownloadLegacyDepot(progress, customSteamPath));
    }
    
    private string DownloadLegacyDepot(
        IProgress<InstallProgressReport> progress,
        string? customSteamPath)
    {
        try
        {
            progress.Report(new InstallProgressReport 
            { 
                Message = "Locating Steam installation...", 
                Percentage = 5 
            });
            
            // Find Steam installation
            string steamPath = customSteamPath ?? SteamLibraryUtility.GetSteamRoot()
                ?? throw new DirectoryNotFoundException("Could not locate Steam installation. Please ensure Steam is installed.");
            
            // Locate steam.exe
            string steamExe = Path.Combine(steamPath, "steam.exe");
            if (!File.Exists(steamExe))
            {
                // Try alternative paths
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    steamExe = Path.Combine(steamPath, "steam.sh");
                }
                
                if (!File.Exists(steamExe))
                {
                    throw new FileNotFoundException($"Could not find Steam executable at {steamExe}");
                }
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Preparing Steam console command...", 
                Percentage = 10 
            });
            
            string arguments = $"-console +download_depot {GARRYSMOD_APP_ID} {GARRYSMOD_DEPOT_ID} {LEGACY_MANIFEST_ID}";
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Launching Steam console for depot download...\nThis may take several minutes depending on your connection speed.", 
                Percentage = 15 
            });
            
            // Create process start info
            var startInfo = new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = steamPath
            };
            
            // Start the process
            using var process = new Process();
            process.StartInfo = startInfo;
            
            // Set up output handlers
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    
                    // Try to parse progress from Steam output
                    UpdateProgressFromOutput(e.Data, progress);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // Wait for the process to exit
            // Steam depot downloads can take a while, so we use a longer timeout
            bool exited = process.WaitForExit(600000); // 10 minutes timeout
            
            if (!exited)
            {
                process.Kill();
                throw new TimeoutException("Steam depot download timed out after 10 minutes.");
            }
            
            // Check exit code
            if (process.ExitCode != 0)
            {
                string errorOutput = errorBuilder.ToString();
                if (string.IsNullOrWhiteSpace(errorOutput))
                {
                    errorOutput = outputBuilder.ToString();
                }
                
                // Check for common error patterns
                if (errorOutput.Contains("not logged in", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Steam is not logged in. Please log in to Steam first.");
                }
                else if (errorOutput.Contains("Steam Guard", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Steam Guard authentication required. Please authenticate in Steam first.");
                }
                else if (errorOutput.Contains("depot not found", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Could not find the specified depot. This may require ownership of Garry's Mod.");
                }
                else
                {
                    throw new Exception($"Steam depot download failed with exit code {process.ExitCode}.\n{errorOutput}");
                }
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = "Depot download completed. Locating downloaded files...", 
                Percentage = 90 
            });
            
            // Determine where Steam downloaded the depot
            // By default, Steam places depot downloads in steamapps/content/app_<appid>/depot_<depotid>
            string depotPath = Path.Combine(steamPath, "steamapps", "content", 
                $"app_{GARRYSMOD_APP_ID}", $"depot_{GARRYSMOD_DEPOT_ID}");
            
            if (!Directory.Exists(depotPath))
            {
                // Try alternative locations
                var steamappsPath = Path.Combine(steamPath, "steamapps");
                if (Directory.Exists(steamappsPath))
                {
                    var contentPath = Path.Combine(steamappsPath, "content");
                    if (Directory.Exists(contentPath))
                    {
                        // Look for any app_4000 folder
                        var appFolders = Directory.GetDirectories(contentPath, "app_4000", SearchOption.TopDirectoryOnly);
                        if (appFolders.Length > 0)
                        {
                            var depotFolder = Path.Combine(appFolders[0], $"depot_{GARRYSMOD_DEPOT_ID}");
                            if (Directory.Exists(depotFolder))
                            {
                                depotPath = depotFolder;
                            }
                        }
                    }
                }
                
                if (!Directory.Exists(depotPath))
                {
                    throw new DirectoryNotFoundException($"Could not find downloaded depot at expected location: {depotPath}");
                }
            }
            
            progress.Report(new InstallProgressReport 
            { 
                Message = $"Depot successfully downloaded to: {depotPath}", 
                Percentage = 100 
            });
            
            return depotPath;
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
    
    private void UpdateProgressFromOutput(string output, IProgress<InstallProgressReport> progress)
    {
        // Try to parse download progress from Steam output
        // Steam typically outputs something like "Downloading 1234/5678 KB"
        if (output.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
        {
            // Extract percentage if available
            var percentMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)%");
            if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int percent))
            {
                // Map download progress to 20-85% range
                int mappedPercent = 20 + (int)(percent * 0.65);
                progress.Report(new InstallProgressReport 
                { 
                    Message = output, 
                    Percentage = mappedPercent 
                });
            }
            else
            {
                // Just report the message
                progress.Report(new InstallProgressReport 
                { 
                    Message = output, 
                    Percentage = -1 // Don't update percentage
                });
            }
        }
        else if (output.Contains("Depot download complete", StringComparison.OrdinalIgnoreCase))
        {
            progress.Report(new InstallProgressReport 
            { 
                Message = "Depot download complete!", 
                Percentage = 85 
            });
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
            
            // List of files/folders to backup and replace
            var itemsToReplace = new[] { "bin", "garrysmod", "platform", "sourceengine" };
            
            foreach (var item in itemsToReplace)
            {
                var sourcePath = Path.Combine(depotPath, item);
                var targetItemPath = Path.Combine(targetPath, item);
                var backupItemPath = Path.Combine(backupPath, item);
                
                if (Directory.Exists(sourcePath))
                {
                    if (Directory.Exists(targetItemPath))
                    {
                        // Move existing to backup
                        progress.Report(new InstallProgressReport 
                        { 
                            Message = $"Backing up {item}...", 
                            Percentage = 20 
                        });
                        
                        CopyDirectory(targetItemPath, backupItemPath);
                    }
                }
            }
            
            // Copy depot files to installation
            progress.Report(new InstallProgressReport 
            { 
                Message = "Applying depot files to installation...", 
                Percentage = 50 
            });
            
            foreach (var item in itemsToReplace)
            {
                var sourcePath = Path.Combine(depotPath, item);
                var targetItemPath = Path.Combine(targetPath, item);
                
                if (Directory.Exists(sourcePath))
                {
                    progress.Report(new InstallProgressReport 
                    { 
                        Message = $"Copying {item}...", 
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
                    var targetFile = Path.Combine(targetPath, item);
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }
                    File.Copy(sourcePath, targetFile);
                }
            }
            
            // Copy executable files
            var exeFiles = new[] { "gmod.exe", "hl2.exe", "steam_appid.txt" };
            foreach (var exe in exeFiles)
            {
                var sourceFile = Path.Combine(depotPath, exe);
                var targetFile = Path.Combine(targetPath, exe);
                
                if (File.Exists(sourceFile))
                {
                    if (File.Exists(targetFile))
                    {
                        File.Copy(targetFile, Path.Combine(backupPath, exe), overwrite: true);
                        File.Delete(targetFile);
                    }
                    File.Copy(sourceFile, targetFile);
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
}
