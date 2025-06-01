using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;

namespace RTXLauncher
{
    public static class RTXIOPackageManager
    {
        private static readonly string RTXIOFolder = Path.Combine(".", "launcherdeps", "rtxio");
        private static readonly string RTXIOExtractorPath = Path.Combine(RTXIOFolder, "bin", "RtxIoResourceExtractor.exe");
        private static readonly string TempOutputFolder = Path.Combine(".", "out");
        
        // dxvk-remix repository information
        private const string DxvkRemixRepoUrl = "https://github.com/NVIDIAGameWorks/dxvk-remix.git";
        private const string DxvkRemixZipUrl = "https://github.com/NVIDIAGameWorks/dxvk-remix/archive/refs/heads/main.zip";
        private static readonly string TempRepoFolder = Path.Combine(".", "launcherdeps", "dxvk-remix-temp");

        // USDA fixes repository information
        private const string UsdaFixesRepoUrl = "https://github.com/sambow23/rtx-usda-fixes.git";
        private const string UsdaFixesZipUrl = "https://github.com/sambow23/rtx-usda-fixes/archive/refs/heads/main.zip";
        private static readonly string TempUsdaFixesFolder = Path.Combine(".", "launcherdeps", "usda-fixes-temp");

        public delegate void ProgressUpdateHandler(string message, int progress);
        public static event ProgressUpdateHandler OnProgressUpdate;

        /// <summary>
        /// Checks if a game directory contains RTXIO package files (.pkg)
        /// </summary>
        /// <param name="gameInstallPath">Path to the game installation</param>
        /// <param name="remixModFolder">Name of the remix mod folder</param>
        /// <returns>True if .pkg files are found</returns>
        public static bool HasRTXIOPackageFiles(string gameInstallPath, string remixModFolder)
        {
            var remixModPath = Path.Combine(gameInstallPath, "rtx-remix", "mods", remixModFolder);
            if (!Directory.Exists(remixModPath))
                return false;

            var pkgFiles = Directory.GetFiles(remixModPath, "*.pkg", SearchOption.TopDirectoryOnly);
            return pkgFiles.Length > 0;
        }

        /// <summary>
        /// Gets all .pkg files in the remix mod directory
        /// </summary>
        /// <param name="gameInstallPath">Path to the game installation</param>
        /// <param name="remixModFolder">Name of the remix mod folder</param>
        /// <returns>Array of .pkg file paths</returns>
        public static string[] GetPackageFiles(string gameInstallPath, string remixModFolder)
        {
            var remixModPath = Path.Combine(gameInstallPath, "rtx-remix", "mods", remixModFolder);
            if (!Directory.Exists(remixModPath))
                return new string[0];

            return Directory.GetFiles(remixModPath, "*.pkg", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Downloads and installs RTXIO package using dxvk-remix repository's dependency system
        /// </summary>
        /// <returns>True if RTXIO is available (either already installed or successfully downloaded)</returns>
        public static async Task<bool> EnsureRTXIOAvailableAsync()
        {
            try
            {
                // Check if RTXIO extractor already exists
                if (File.Exists(RTXIOExtractorPath))
                {
                    LogProgress("RTXIO extractor already available", 50);
                    return true;
                }

                LogProgress("RTXIO extractor not found, downloading dxvk-remix...", 10);

                // Create the RTXIO directory
                Directory.CreateDirectory(RTXIOFolder);

                // Download dxvk-remix repository
                if (!await DownloadDxvkRemixRepository())
                {
                    LogProgress("Failed to download dxvk-remix repository", 0);
                    return false;
                }

                LogProgress("Running dxvk-remix dependency update...", 25);
                
                if (!await RunDxvkRemixDependencyUpdate())
                {
                    LogProgress("Failed to update dxvk-remix dependencies", 0);
                    LogProgress("Note: The GitHub ZIP download doesn't include packman tools needed for dependency management", 0);
                    LogProgress("Please install dxvk-remix manually or use an existing installation", 0);
                    return false;
                }

                LogProgress("Extracting RTXIO from dxvk-remix dependencies...", 40);
                
                if (!ExtractRTXIOFromDxvkRemix())
                {
                    LogProgress("Failed to extract RTXIO from dxvk-remix", 0);
                    return false;
                }

                // Clean up temporary repository
                CleanupTempRepository();

                // Verify the extractor exists
                if (File.Exists(RTXIOExtractorPath))
                {
                    LogProgress("RTXIO extractor ready", 50);
                    return true;
                }
                else
                {
                    LogProgress($"RTXIO extractor not found at expected path: {RTXIOExtractorPath}", 0);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Error setting up RTXIO: {ex.Message}", 0);
                CleanupTempRepository();
                return false;
            }
        }

        /// <summary>
        /// Downloads the dxvk-remix repository as a ZIP file
        /// </summary>
        private static async Task<bool> DownloadDxvkRemixRepository()
        {
            try
            {
                // Clean up any existing temp repository
                CleanupTempRepository();
                Directory.CreateDirectory(TempRepoFolder);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher/1.0");

                    LogProgress("Downloading dxvk-remix repository archive...", 25);
                    
                    var response = await httpClient.GetAsync(DxvkRemixZipUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        LogProgress($"Failed to download dxvk-remix: HTTP {response.StatusCode} - {response.ReasonPhrase}", 0);
                        return false;
                    }

                    var zipData = await response.Content.ReadAsByteArrayAsync();
                    if (zipData.Length == 0)
                    {
                        LogProgress("Downloaded dxvk-remix archive is empty", 0);
                        return false;
                    }

                    var tempZipPath = Path.Combine(TempRepoFolder, "dxvk-remix.zip");
                    await File.WriteAllBytesAsync(tempZipPath, zipData);

                    LogProgress("Extracting dxvk-remix repository...", 40);

                    try
                    {
                        ZipFile.ExtractToDirectory(tempZipPath, TempRepoFolder, true);
                    }
                    catch (InvalidDataException)
                    {
                        LogProgress("Downloaded file is not a valid ZIP archive", 0);
                        return false;
                    }

                    // Clean up zip file
                    File.Delete(tempZipPath);

                    LogProgress("dxvk-remix repository extracted successfully", 45);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Error downloading dxvk-remix repository: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Runs the dxvk-remix dependency update script to download RTXIO
        /// </summary>
        private static async Task<bool> RunDxvkRemixDependencyUpdate()
        {
            try
            {
                // The extracted directory will be named dxvk-remix-main
                var dxvkRemixDir = Path.Combine(TempRepoFolder, "dxvk-remix-main");
                
                if (!Directory.Exists(dxvkRemixDir))
                {
                    LogProgress($"Could not find extracted dxvk-remix directory at: {dxvkRemixDir}", 0);
                    return false;
                }

                var updateDepsScript = Path.Combine(dxvkRemixDir, "scripts-common", "update-deps.cmd");

                if (!File.Exists(updateDepsScript))
                {
                    LogProgress($"update-deps.cmd not found at: {updateDepsScript}", 0);
                    return false;
                }

                // Convert to absolute paths to ensure proper resolution
                var absoluteDxvkRemixDir = Path.GetFullPath(dxvkRemixDir);
                var absoluteUpdateDepsScript = Path.GetFullPath(updateDepsScript);

                LogProgress("Running dxvk-remix dependency update script...", 55);
                LogProgress($"Script location: {absoluteUpdateDepsScript}", 56);
                LogProgress($"Working directory: {absoluteDxvkRemixDir}", 57);

                // Check if packman repository already exists
                var packmanRepoPath = Path.Combine("C:", "packman-repo");
                if (Directory.Exists(packmanRepoPath))
                {
                    LogProgress($"Packman repository already exists at: {packmanRepoPath}", 58);
                }
                else
                {
                    LogProgress("Packman repository not found, will be created during dependency update", 58);
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = absoluteUpdateDepsScript,
                    WorkingDirectory = absoluteDxvkRemixDir, // Use absolute path for working directory
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        LogProgress("Failed to start dependency update script", 0);
                        return false;
                    }

                    LogProgress($"Dependency update script started with PID: {process.Id}", 58);

                    // Create string builders to capture all output
                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();

                    // Set up real-time output capture
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputBuilder.AppendLine(e.Data);
                            LogProgress($"Packman: {e.Data}", 60);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorBuilder.AppendLine(e.Data);
                            LogProgress($"Packman Error: {e.Data}", 60);
                        }
                    };

                    // Start reading output
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Set a reasonable timeout for the dependency update (15 minutes)
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(15));
                    var processTask = process.WaitForExitAsync();
                    
                    LogProgress("Waiting for packman to download dependencies...", 65);
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        LogProgress("Dependency update script timed out after 15 minutes", 0);
                        LogProgress($"Final output: {outputBuilder.ToString()}", 0);
                        LogProgress($"Final error: {errorBuilder.ToString()}", 0);
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return false;
                    }

                    // Wait a moment for any remaining output
                    await Task.Delay(500);

                    var finalOutput = outputBuilder.ToString();
                    var finalError = errorBuilder.ToString();

                    LogProgress($"Dependency update script finished with exit code: {process.ExitCode}", 70);
                    
                    if (!string.IsNullOrEmpty(finalOutput))
                    {
                        LogProgress($"Complete packman output: {finalOutput}", 71);
                    }
                    
                    if (!string.IsNullOrEmpty(finalError))
                    {
                        LogProgress($"Complete packman error output: {finalError}", 72);
                    }

                    if (process.ExitCode != 0)
                    {
                        LogProgress($"Dependency update failed: Exit code {process.ExitCode}", 0);
                        LogProgress("This may be normal if packman needs to download dependencies for the first time", 0);
                        
                        // Don't immediately fail - packman might still have downloaded what we need
                        // We'll check for RTXIO in the extraction step
                        LogProgress("Continuing to check if RTXIO was downloaded despite exit code...", 73);
                    }
                    else
                    {
                        LogProgress("Dependency update completed successfully", 75);
                    }
                }

                return true; // Always return true and let the extraction step determine if RTXIO is available
            }
            catch (Exception ex)
            {
                LogProgress($"Error running dependency update: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Extracts RTXIO from the dxvk-remix external dependencies
        /// </summary>
        private static bool ExtractRTXIOFromDxvkRemix()
        {
            try
            {
                // The extracted directory will be named dxvk-remix-main
                var dxvkRemixDir = Path.Combine(TempRepoFolder, "dxvk-remix-main");
                
                if (!Directory.Exists(dxvkRemixDir))
                {
                    LogProgress($"Could not find extracted dxvk-remix directory at: {dxvkRemixDir}", 0);
                    return false;
                }

                // After running packman, RTXIO will be in the global packman repository
                // Try multiple possible locations for RTXIO
                var possibleRtxioLocations = new[]
                {
                    // Global packman repository location
                    Path.Combine("C:", "packman-repo", "chk", "rtx-remix-rtxio", "7", "bin", "RtxIoResourceExtractor.exe"),
                    
                    // Alternative packman locations
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "packman-repo", "chk", "rtx-remix-rtxio", "7", "bin", "RtxIoResourceExtractor.exe"),
                    
                    // Symlinked location in dxvk-remix (if packman created symlinks)
                    Path.Combine(dxvkRemixDir, "external", "rtxio", "bin", "RtxIoResourceExtractor.exe"),
                    
                    // Alternative symlink locations
                    Path.Combine(dxvkRemixDir, "_build", "target-deps", "rtxio", "bin", "RtxIoResourceExtractor.exe"),
                };

                string rtxioExtractorSource = null;
                string rtxioSourceDir = null;

                LogProgress("Searching for RTXIO extractor in packman repository...", 85);

                foreach (var location in possibleRtxioLocations)
                {
                    LogProgress($"Checking: {location}", 86);
                    if (File.Exists(location))
                    {
                        rtxioExtractorSource = location;
                        rtxioSourceDir = Path.GetDirectoryName(Path.GetDirectoryName(location)); // Go up two levels from bin/RtxIoResourceExtractor.exe
                        LogProgress($"Found RTXIO extractor at: {rtxioExtractorSource}", 87);
                        break;
                    }
                }

                if (rtxioExtractorSource == null)
                {
                    LogProgress("RTXIO extractor not found in any expected location", 0);
                    LogProgress("Packman may have failed to download RTXIO, or it's in an unexpected location", 0);
                    
                    // Try to find any RtxIoResourceExtractor.exe on the system
                    LogProgress("Searching for RtxIoResourceExtractor.exe in common locations...", 88);
                    var searchPaths = new[]
                    {
                        "C:\\packman-repo",
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "packman-repo"),
                        dxvkRemixDir
                    };

                    foreach (var searchPath in searchPaths)
                    {
                        if (Directory.Exists(searchPath))
                        {
                            try
                            {
                                var foundFiles = Directory.GetFiles(searchPath, "RtxIoResourceExtractor.exe", SearchOption.AllDirectories);
                                if (foundFiles.Length > 0)
                                {
                                    rtxioExtractorSource = foundFiles[0];
                                    rtxioSourceDir = Path.GetDirectoryName(Path.GetDirectoryName(rtxioExtractorSource));
                                    LogProgress($"Found RTXIO extractor via search at: {rtxioExtractorSource}", 89);
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogProgress($"Error searching in {searchPath}: {ex.Message}", 0);
                            }
                        }
                    }
                }

                if (rtxioExtractorSource == null)
                {
                    LogProgress("Could not find RTXIO extractor anywhere on the system", 0);
                    return false;
                }

                // Copy the entire rtxio directory to our target location
                if (Directory.Exists(rtxioSourceDir))
                {
                    LogProgress($"Copying RTXIO from: {rtxioSourceDir}", 90);
                    CopyDirectory(rtxioSourceDir, RTXIOFolder);
                }
                else
                {
                    // If we can't find the full directory, just copy the executable
                    LogProgress($"Copying RTXIO executable only from: {rtxioExtractorSource}", 90);
                    Directory.CreateDirectory(Path.GetDirectoryName(RTXIOExtractorPath));
                    File.Copy(rtxioExtractorSource, RTXIOExtractorPath, true);
                }

                LogProgress("RTXIO extracted successfully from packman repository", 95);
                return true;
            }
            catch (Exception ex)
            {
                LogProgress($"Error extracting RTXIO from dxvk-remix: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Cleans up the temporary repository directory
        /// </summary>
        private static void CleanupTempRepository()
        {
            try
            {
                if (Directory.Exists(TempRepoFolder))
                {
                    Directory.Delete(TempRepoFolder, true);
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Warning: Could not clean up temporary repository: {ex.Message}", 0);
                // Not a critical failure
            }
        }

        /// <summary>
        /// Extracts all .pkg files in the specified game directory
        /// </summary>
        /// <param name="gameInstallPath">Path to the game installation</param>
        /// <param name="remixModFolder">Name of the remix mod folder</param>
        /// <returns>True if extraction was successful</returns>
        public static async Task<bool> ExtractPackageFilesAsync(string gameInstallPath, string remixModFolder)
        {
            try
            {
                var remixModPath = Path.Combine(gameInstallPath, "rtx-remix", "mods", remixModFolder);
                var pkgFiles = GetPackageFiles(gameInstallPath, remixModFolder);

                if (pkgFiles.Length == 0)
                {
                    LogProgress("No .pkg files found to extract", 100);
                    return true;
                }

                LogProgress($"Found {pkgFiles.Length} .pkg files to extract", 5);

                // Validate that all .pkg files exist and are accessible
                foreach (var pkgFile in pkgFiles)
                {
                    if (!File.Exists(pkgFile))
                    {
                        LogProgress($"Package file not found: {Path.GetFileName(pkgFile)}", 0);
                        return false;
                    }
                    
                    try
                    {
                        // Try to open the file to check if it's accessible
                        using (var fs = File.OpenRead(pkgFile))
                        {
                            // Just check if we can read the first byte
                            fs.ReadByte();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogProgress($"Cannot access package file {Path.GetFileName(pkgFile)}: {ex.Message}", 0);
                        return false;
                    }
                }

                // Ensure RTXIO is available (this will report progress from 10-50%)
                if (!await EnsureRTXIOAvailableAsync())
                {
                    LogProgress("Failed to obtain RTXIO extractor", 0);
                    return false;
                }

                // Verify RTXIO extractor exists and log its details
                if (!File.Exists(RTXIOExtractorPath))
                {
                    LogProgress($"RTXIO extractor not found at: {RTXIOExtractorPath}", 0);
                    return false;
                }

                var extractorInfo = new FileInfo(RTXIOExtractorPath);
                LogProgress($"RTXIO extractor found: {RTXIOExtractorPath} (Size: {extractorInfo.Length} bytes, Modified: {extractorInfo.LastWriteTime})", 50);

                // Create output directory
                if (Directory.Exists(TempOutputFolder))
                {
                    Directory.Delete(TempOutputFolder, true);
                }
                Directory.CreateDirectory(TempOutputFolder);

                LogProgress($"Created temporary output directory: {Path.GetFullPath(TempOutputFolder)}", 52);
                LogProgress($"Current working directory: {Directory.GetCurrentDirectory()}", 53);
                LogProgress("Starting package extraction...", 55);

                // Extract each .pkg file (progress from 55% to 85%)
                for (int i = 0; i < pkgFiles.Length; i++)
                {
                    var pkgFile = pkgFiles[i];
                    var fileName = Path.GetFileName(pkgFile);
                    
                    int progressPercent = 55 + (i * 30 / pkgFiles.Length);
                    LogProgress($"Extracting {fileName} ({i + 1}/{pkgFiles.Length})", progressPercent);

                    // Log detailed file information
                    var pkgFileInfo = new FileInfo(pkgFile);
                    LogProgress($"Processing file: {pkgFile}", progressPercent);
                    LogProgress($"File size: {pkgFileInfo.Length} bytes ({pkgFileInfo.Length / 1024.0 / 1024.0:F2} MB)", progressPercent);
                    LogProgress($"File exists: {File.Exists(pkgFile)}", progressPercent);

                    // Log the exact command being executed
                    var arguments = $"\"{pkgFile}\" --force -o \"{TempOutputFolder}\"";
                    LogProgress($"Executing: {RTXIOExtractorPath} {arguments}", progressPercent);

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = RTXIOExtractorPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        if (process == null)
                        {
                            LogProgress($"Failed to start RTXIO extractor for {fileName}", 0);
                            return false;
                        }

                        LogProgress($"RTXIO extractor started with PID: {process.Id}", progressPercent);

                        // Read output in real-time to see what's happening
                        var outputBuilder = new System.Text.StringBuilder();
                        var errorBuilder = new System.Text.StringBuilder();

                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                outputBuilder.AppendLine(e.Data);
                                LogProgress($"RTXIO Output: {e.Data}", progressPercent);
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                errorBuilder.AppendLine(e.Data);
                                LogProgress($"RTXIO Error: {e.Data}", progressPercent);
                            }
                        };

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // Set a reasonable timeout for the extraction process
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10));
                        var processTask = process.WaitForExitAsync();
                        
                        LogProgress($"Waiting for RTXIO extractor to complete...", progressPercent);
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            LogProgress($"RTXIO extractor timed out for {fileName} after 10 minutes", 0);
                            LogProgress($"Final output: {outputBuilder.ToString()}", 0);
                            LogProgress($"Final error: {errorBuilder.ToString()}", 0);
                            try
                            {
                                process.Kill();
                            }
                            catch { }
                            return false;
                        }

                        // Wait a moment for any remaining output
                        await Task.Delay(100);

                        var finalOutput = outputBuilder.ToString();
                        var finalError = errorBuilder.ToString();

                        LogProgress($"RTXIO extractor finished with exit code: {process.ExitCode}", progressPercent);
                        
                        if (!string.IsNullOrEmpty(finalOutput))
                        {
                            LogProgress($"Complete output: {finalOutput}", progressPercent);
                        }
                        
                        if (!string.IsNullOrEmpty(finalError))
                        {
                            LogProgress($"Complete error output: {finalError}", progressPercent);
                        }

                        if (process.ExitCode != 0)
                        {
                            LogProgress($"RTXIO extractor failed for {fileName}: Exit code {process.ExitCode}", 0);
                            return false;
                        }

                        LogProgress($"Successfully extracted {fileName}", progressPercent);
                    }
                }

                LogProgress("Package extraction completed", 85);

                // Verify that extraction produced some output
                if (!Directory.Exists(TempOutputFolder) || !Directory.GetFileSystemEntries(TempOutputFolder).Any())
                {
                    LogProgress("No files were extracted from the packages", 0);
                    return false;
                }

                // Delete original .pkg files
                LogProgress("Removing original .pkg files...", 90);
                foreach (var pkgFile in pkgFiles)
                {
                    try
                    {
                        File.Delete(pkgFile);
                    }
                    catch (Exception ex)
                    {
                        LogProgress($"Warning: Could not delete {Path.GetFileName(pkgFile)}: {ex.Message}", 0);
                        // Continue anyway - this is not a critical failure
                    }
                }

                // Copy extracted content to the remix mod folder
                LogProgress("Copying extracted content...", 95);
                CopyDirectory(TempOutputFolder, remixModPath);

                // Clean up temporary output folder
                try
                {
                    Directory.Delete(TempOutputFolder, true);
                }
                catch (Exception ex)
                {
                    LogProgress($"Warning: Could not clean up temporary folder: {ex.Message}", 0);
                    // Not a critical failure
                }

                LogProgress("RTXIO package extraction completed successfully", 100);
                return true;
            }
            catch (Exception ex)
            {
                LogProgress($"Error during package extraction: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Copies a directory recursively
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        /// <summary>
        /// Logs progress updates
        /// </summary>
        private static void LogProgress(string message, int progress)
        {
            OnProgressUpdate?.Invoke(message, progress);
        }

        /// <summary>
        /// Downloads and applies USDA fixes for Half-Life 2: RTX
        /// </summary>
        /// <param name="gameInstallPath">Path to the game installation</param>
        /// <param name="remixModFolder">Name of the remix mod folder</param>
        /// <returns>True if USDA fixes were applied successfully</returns>
        public static async Task<bool> ApplyUsdaFixesAsync(string gameInstallPath, string remixModFolder)
        {
            try
            {
                // Only apply USDA fixes for Half-Life 2: RTX
                if (remixModFolder != "hl2rtx")
                {
                    LogProgress("USDA fixes not needed for this game", 100);
                    return true;
                }

                LogProgress("Downloading USDA fixes for Half-Life 2: RTX...", 5);

                var remixModPath = Path.Combine(gameInstallPath, "rtx-remix", "mods", remixModFolder);
                if (!Directory.Exists(remixModPath))
                {
                    LogProgress($"RTX Remix mod folder not found: {remixModPath}", 0);
                    return false;
                }

                // Download USDA fixes repository
                if (!await DownloadUsdaFixesRepository())
                {
                    LogProgress("Failed to download USDA fixes repository", 0);
                    return false;
                }

                LogProgress("Applying USDA fixes...", 80);

                // Apply the fixes
                if (!ApplyUsdaFixesToGame(remixModPath))
                {
                    LogProgress("Failed to apply USDA fixes", 0);
                    return false;
                }

                // Clean up temporary repository
                CleanupUsdaFixesRepository();

                LogProgress("USDA fixes applied successfully", 100);
                return true;
            }
            catch (Exception ex)
            {
                LogProgress($"Error applying USDA fixes: {ex.Message}", 0);
                CleanupUsdaFixesRepository();
                return false;
            }
        }

        /// <summary>
        /// Downloads the USDA fixes repository as a ZIP file
        /// </summary>
        private static async Task<bool> DownloadUsdaFixesRepository()
        {
            try
            {
                // Clean up any existing temp repository
                CleanupUsdaFixesRepository();
                Directory.CreateDirectory(TempUsdaFixesFolder);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher/1.0");

                    LogProgress("Downloading USDA fixes repository archive...", 20);
                    
                    var response = await httpClient.GetAsync(UsdaFixesZipUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        LogProgress($"Failed to download USDA fixes: HTTP {response.StatusCode} - {response.ReasonPhrase}", 0);
                        return false;
                    }

                    var zipData = await response.Content.ReadAsByteArrayAsync();
                    if (zipData.Length == 0)
                    {
                        LogProgress("Downloaded USDA fixes archive is empty", 0);
                        return false;
                    }

                    var tempZipPath = Path.Combine(TempUsdaFixesFolder, "usda-fixes.zip");
                    await File.WriteAllBytesAsync(tempZipPath, zipData);

                    LogProgress("Extracting USDA fixes repository...", 50);

                    try
                    {
                        ZipFile.ExtractToDirectory(tempZipPath, TempUsdaFixesFolder, true);
                    }
                    catch (InvalidDataException)
                    {
                        LogProgress("Downloaded USDA fixes file is not a valid ZIP archive", 0);
                        return false;
                    }

                    // Clean up zip file
                    File.Delete(tempZipPath);

                    LogProgress("USDA fixes repository extracted successfully", 70);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Error downloading USDA fixes repository: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Applies USDA fixes to the game directory
        /// </summary>
        private static bool ApplyUsdaFixesToGame(string remixModPath)
        {
            try
            {
                // Find the extracted USDA fixes directory (it will be named rtx-usda-fixes-main)
                var extractedDirs = Directory.GetDirectories(TempUsdaFixesFolder, "rtx-usda-fixes-*");
                if (extractedDirs.Length == 0)
                {
                    LogProgress("Could not find extracted USDA fixes directory", 0);
                    return false;
                }

                var usdaFixesDir = extractedDirs[0];
                var hl2rtxdemoDir = Path.Combine(usdaFixesDir, "hl2rtxdemo");

                if (!Directory.Exists(hl2rtxdemoDir))
                {
                    LogProgress($"hl2rtxdemo directory not found at: {hl2rtxdemoDir}", 0);
                    return false;
                }

                // Get all .usda files from the hl2rtxdemo directory
                var usdaFiles = Directory.GetFiles(hl2rtxdemoDir, "*.usda", SearchOption.AllDirectories);
                if (usdaFiles.Length == 0)
                {
                    LogProgress("No .usda files found in USDA fixes repository", 0);
                    return false;
                }

                LogProgress($"Found {usdaFiles.Length} USDA fix files to copy", 85);

                // Copy each .usda file to the remix mod directory
                foreach (var usdaFile in usdaFiles)
                {
                    var fileName = Path.GetFileName(usdaFile);
                    var destinationPath = Path.Combine(remixModPath, fileName);
                    
                    try
                    {
                        File.Copy(usdaFile, destinationPath, true);
                        LogProgress($"Copied USDA fix: {fileName}", 90);
                    }
                    catch (Exception ex)
                    {
                        LogProgress($"Warning: Could not copy {fileName}: {ex.Message}", 0);
                        // Continue with other files - this is not a critical failure
                    }
                }

                LogProgress($"Successfully copied {usdaFiles.Length} USDA fix files", 95);
                return true;
            }
            catch (Exception ex)
            {
                LogProgress($"Error applying USDA fixes to game: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>
        /// Cleans up the temporary USDA fixes repository directory
        /// </summary>
        private static void CleanupUsdaFixesRepository()
        {
            try
            {
                if (Directory.Exists(TempUsdaFixesFolder))
                {
                    Directory.Delete(TempUsdaFixesFolder, true);
                }
            }
            catch (Exception ex)
            {
                LogProgress($"Warning: Could not clean up temporary USDA fixes repository: {ex.Message}", 0);
                // Not a critical failure
            }
        }
    }
} 