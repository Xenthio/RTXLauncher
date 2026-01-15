// Services/PatchingService.cs

using RTXLauncher.Core.Models;
using RTXLauncher.Core.Utilities;
using System.Text;
using System.Text.RegularExpressions;

namespace RTXLauncher.Core.Services;

public class PatchingService
{
	private readonly HttpClient _httpClient;

	public PatchingService()
	{
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "RTXLauncher");
	}

	/// <summary>
	/// Fetches a patch script from GitHub, parses it, and applies the binary patches to an installation.
	/// </summary>
	public async Task ApplyPatchesAsync(string owner, string repo, string filePath, string installPath, IProgress<InstallProgressReport> progress, string branch = "master")
	{
		progress.Report(new InstallProgressReport { Message = "Fetching patch definitions...", Percentage = 5 });
		var patchFileContent = await FetchPatchFileContentAsync(owner, repo, filePath, branch);

		await Task.Run(() =>
		{
			progress.Report(new InstallProgressReport { Message = "Parsing patch definitions...", Percentage = 10 });
			var (patches32, patches64) = PatchParser.ParsePatches(patchFileContent);

			var installType = GarrysModUtility.GetInstallType(installPath);
			PatchParser.PatchDictionary patchesToUse;

			if (installType == "gmod_x86-64")
			{
				patchesToUse = patches64;
				progress.Report(new InstallProgressReport { Message = "Detected 64-bit installation...", Percentage = 15 });
			}
			else if (installType == "gmod_main" || installType == "gmod_i386")
			{
				patchesToUse = patches32;
				progress.Report(new InstallProgressReport { Message = "Detected 32-bit installation...", Percentage = 15 });
			}
			else
			{
				throw new Exception($"Patching is not supported for this installation type: {installType}");
			}

		var fileContents = new Dictionary<string, byte[]>();
		var modifiedFiles = new Dictionary<string, byte[]>();
		var missingFiles = new List<string>();
		var fileCount = patchesToUse.Patches.Keys.Count;
		int currentFileIndex = 0;

		foreach (string fileName in patchesToUse.Patches.Keys)
		{
			string filePathOnDisk = ResolveFilePath(installPath, fileName, installType);
			if (!File.Exists(filePathOnDisk))
			{
				missingFiles.Add(fileName);
				continue;
			}

			fileContents[fileName] = File.ReadAllBytes(filePathOnDisk);
			currentFileIndex++;
			progress.Report(new InstallProgressReport { Message = $"Loaded file: {fileName}", Percentage = 15 + (int)((float)currentFileIndex / fileCount * 15) });
		}

			if (missingFiles.Count > 0)
			{
				throw new FileNotFoundException($"Patching failed. Missing required files: {string.Join(", ", missingFiles)}");
			}

			progress.Report(new InstallProgressReport { Message = "Applying patches...", Percentage = 30 });
			int totalPatches = patchesToUse.Patches.Sum(p => p.Value.Count);
			int completedPatches = 0;

			foreach (var filePair in patchesToUse.Patches)
			{
				string fileName = filePair.Key;
				var patches = filePair.Value;
				byte[] currentFileContent = fileContents[fileName];

				// ==========================================================
				//      THIS IS THE CORRECTED LOGIC BLOCK
				// ==========================================================
				foreach (var patchData in patches)
				{
					bool patched = false;

					// A valid patchData is a list of [ patterns_list, patch_hex_string ]
					if (patchData.Count >= 2 && patchData[0] is List<object> patterns && patchData[1] is string patchHex)
					{
						// Check if patterns is a list of lists (multiple patterns) or a single pattern list
						if (patterns.Count > 0 && patterns[0] is List<object>)
						{
							// It's a list of multiple patterns, try each one until one succeeds.
							foreach (var pattern in patterns.Cast<List<object>>())
							{
								if (TryApplyPattern(fileName, currentFileContent, pattern, ref modifiedFiles, patchHex, progress, completedPatches, totalPatches))
								{
									patched = true;
									break; // Success, stop trying other patterns for this patch.
								}
							}
						}
						else
						{
							// It's a single pattern.
							patched = TryApplyPattern(fileName, currentFileContent, patterns, ref modifiedFiles, patchHex, progress, completedPatches, totalPatches);
						}
					}

					if (!patched)
					{
						progress.Report(new InstallProgressReport { Message = $"Warning: A patch for {fileName} was not applied (pattern not found).", Percentage = 30 + (int)((float)completedPatches / totalPatches * 60) });
					}
					completedPatches++;
				}
			}

			if (modifiedFiles.Count == 0)
			{
				progress.Report(new InstallProgressReport { Message = "Patching complete. No applicable patches found for your game files.", Percentage = 100 });
				return;
			}

			progress.Report(new InstallProgressReport { Message = "Creating file backups...", Percentage = 90 });
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string backupDir = Path.Combine(installPath, $"backup_patches_{timestamp}");
			Directory.CreateDirectory(backupDir);

		int savedFiles = 0;
		foreach (var filePair in modifiedFiles)
		{
			string fileName = filePair.Key;
			byte[] modifiedContent = filePair.Value;
			string originalPath = ResolveFilePath(installPath, fileName, installType);
			string backupPath = Path.Combine(backupDir, fileName.Replace('/', Path.DirectorySeparatorChar));

			Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
			File.Copy(originalPath, backupPath, true);

			File.WriteAllBytes(originalPath, modifiedContent);
			savedFiles++;
			progress.Report(new InstallProgressReport { Message = $"Applied patch to {fileName}", Percentage = 90 + (int)((float)savedFiles / modifiedFiles.Count * 10) });
		}

			progress.Report(new InstallProgressReport { Message = $"Successfully patched {savedFiles} file(s). Backups are in {Path.GetFileName(backupDir)}.", Percentage = 100 });
		});
	}
	// --- Private Helper Methods (Ported from your original code) ---

	private async Task<string> FetchPatchFileContentAsync(string owner, string repo, string filePath, string branch = "master")
	{
		var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{filePath}";
		return await _httpClient.GetStringAsync(url);
	}

	/// <summary>
	/// Try to apply a pattern to a file
	/// </summary>
	private bool TryApplyPattern(
		string fileName,
		byte[] fileContent,
		List<object> pattern,
		ref Dictionary<string, byte[]> modifiedFiles,
		string patchHex,
		IProgress<InstallProgressReport> progress,
		int completedPatches,
		int totalPatches)
	{
		try
		{
			if (pattern == null || pattern.Count < 2)
			{

				progress.Report(new InstallProgressReport
				{
					Message = $"Invalid pattern (too few elements)",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});

				return false;
			}

			// Get pattern and offset
			if (!(pattern[0] is string hexPattern))
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Invalid pattern for {fileName}: first element must be a hex string",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			int offset;
			if (pattern[1] is int offsetValue)
			{
				offset = offsetValue;
			}
			else
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Invalid pattern for {fileName}: second element must be an integer",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			// If there's a third element, it overrides the patchHex
			if (pattern.Count >= 3 && pattern[2] is string customPatchHex)
				patchHex = customPatchHex;

			// Format pattern for display - show first part and indicate length
			string displayPattern;
			if (hexPattern.Length > 30)
				displayPattern = $"{hexPattern.Substring(0, 30)}... ({hexPattern.Length} chars)";
			else
				displayPattern = hexPattern;

			progress.Report(new InstallProgressReport
			{
				Message = $"Looking for pattern: {displayPattern}",
				Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
			});

			// Find the pattern in the file
			int position = FindWithMask(fileContent, hexPattern);

			if (position == -1)
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Pattern {displayPattern} not found in {fileName}, skipping...",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			// Check if pattern appears multiple times (should be unique)
			int position2 = FindWithMask(fileContent, hexPattern, position + 1);

			if (position2 != -1)
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Warning: Pattern {displayPattern} found multiple times at 0x{position:X} and 0x{position2:X}, skipping...",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			// Found unique match, apply patch
			position += offset;

			// Validate patch hex
			if (string.IsNullOrEmpty(patchHex) || !IsValidHexString(patchHex))
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Error: Invalid patch hex: {patchHex}",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			byte[] patchBytes = HexStringToByteArray(patchHex);

			// Make sure position is within bounds
			if (position < 0 || position + patchBytes.Length > fileContent.Length)
			{
				progress.Report(new InstallProgressReport
				{
					Message = $"Error: Patch position 0x{position:X} would exceed file length",
					Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
				});
				return false;
			}

			// Get original bytes for detailed logging
			string originalBytes = ByteArrayToHexString(fileContent, position, patchBytes.Length);

			// Copy file content if this is first modification
			if (!modifiedFiles.ContainsKey(fileName))
			{
				modifiedFiles[fileName] = (byte[])fileContent.Clone();
			}

			// Apply patch
			Buffer.BlockCopy(patchBytes, 0, modifiedFiles[fileName], position, patchBytes.Length);

			if (!modifiedFiles.ContainsKey(fileName))
			{
				modifiedFiles[fileName] = (byte[])fileContent.Clone();
			}
			// Log the patch with user-friendly description
			string description = GetPatchDescription(originalBytes, patchHex);

			progress.Report(new InstallProgressReport
			{
				Message = $"Patched at 0x{position:X}:\nOriginal: {FormatHex(originalBytes)}\nNew:      {FormatHex(patchHex)} {description}",
				Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
			});

			return true;
		}
		catch (Exception ex)
		{
			progress.Report(new InstallProgressReport
			{
				Message = $"Error applying pattern to {fileName}: {ex.Message}",
				Percentage = 30 + (int)((float)completedPatches / totalPatches * 60)
			});
			return false;
		}
	}

	// Format hex bytes with spacing for better readability
	private static string FormatHex(string hexString)
	{
		if (string.IsNullOrEmpty(hexString))
			return hexString;

		// Group hex bytes for readability
		const int groupSize = 2; // One byte
		const int groupsPerBlock = 4; // Four bytes per block

		StringBuilder formatted = new StringBuilder();
		for (int i = 0; i < hexString.Length; i += groupSize)
		{
			if (i + groupSize <= hexString.Length)
			{
				formatted.Append(hexString.Substring(i, groupSize));

				// Add space between bytes
				if (i + groupSize < hexString.Length)
				{
					formatted.Append(' ');

					// Add extra space after blocks
					if ((i / groupSize + 1) % groupsPerBlock == 0)
						formatted.Append(' ');
				}
			}
			else
			{
				formatted.Append(hexString.Substring(i));
			}
		}

		return formatted.ToString();
	}

	// Add a helper method to check if a string is valid hex
	private static bool IsValidHexString(string hex)
	{
		return !string.IsNullOrEmpty(hex) && Regex.IsMatch(hex, "^[0-9a-fA-F]+$");
	}

	// Add a helper method to generate user-friendly descriptions of common patch types
	private static string GetPatchDescription(string original, string patch)
	{
		// Common patch: changing a JNZ (75) to JMP (EB)
		if (original.StartsWith("75") && patch == "eb")
			return "(Changed conditional jump to unconditional jump - forces code path)";

		// Common patch: changing a JZ (74) to JMP (EB)
		if (original.StartsWith("74") && patch == "eb")
			return "(Changed zero jump to unconditional jump - always takes branch)";

		// Common patch: changing a JLE/JNG (7E) to JMP (EB)
		if (original.StartsWith("7e") && patch == "eb")
			return "(Changed less-than-or-equal jump to unconditional jump - removes bounds check)";

		// Common patch: NOPs (90)
		if (patch.All(c => c == '9' || c == '0'))
			return "(NOP out instructions - bypasses original code)";

		// Common patch: Return 0 (31C0C3 = xor eax,eax; ret)
		if (patch == "31c0c3")
			return "(Return 0 - skips function execution entirely)";

		// Common patch: force dx9 vtx loading
		if (original == "647838302e767478" && patch == "647839302e767478")
			return "(Changed dx8 vtx to dx9 vtx - forces DirectX 9 vertex format)";

		// m_bForceNoVis patches - client.dll specific
		if (patch == "01" && original.Contains("000000"))
			return "(Force visibility flag to 1 - disables frustum culling)";

		if (patch == "0887")
			return "(Modified MOV instruction for visibility flag - disables culling)";

		// Return true for m_bForceNoVis getter
		if (patch == "b001c3")
			return "(Return true - forces visibility check to always pass)";

		// Zero sized buffer protection
		if (patch.Contains("85c0750") && (patch.Contains("b004") || patch.Contains("f7f8")))
			return "(Added zero-size buffer protection - prevents crash with RTX)";

		// Four hardware lights patch (NOP)
		if (original.StartsWith("480f4ec1") && patch == "90909090")
			return "(Removed max lights limitation - allows more RTX lights)";

		// Four hardware lights patch (NOP) - 32-bit version
		if (original.StartsWith("b80000000f") && patch == "909090")
			return "(Removed max lights limitation - allows more RTX lights)";

		// Brush entity backfaces
		if ((original.StartsWith("753c") || original.StartsWith("db75")) && patch == "eb")
			return "(Enabled brush entity backface rendering - improves RTX lighting)";

		// World backfaces patches
		if ((original.EndsWith("7e") || original.StartsWith("754")) && patch == "eb")
			return "(Enabled world backface rendering - improves lighting for world geometry)";

		// Check if it might be a buffer overrun protection patch
		if (patch.Length > 20 && patch.Contains("85") && patch.Contains("4fc1"))
			return "(Added buffer bounds checking - prevents RTX-related crashes)";

		return "";
	}

	/// <summary>
	/// Finds a pattern in a byte array, supporting wildcard bytes with ??
	/// </summary>
	private static int FindWithMask(byte[] data, string hexPattern, int startIndex = 0)
	{
		// If no wildcards, use normal find
		if (!hexPattern.Contains("??"))
		{
			byte[] pattern = HexStringToByteArray(hexPattern);
			return FindBytes(data, pattern, startIndex);
		}

		// Split by wildcards
		string[] parts = hexPattern.Split(new[] { "??" }, StringSplitOptions.None);

		int currentPosition = startIndex;
		while (true)
		{
			// Find first part
			byte[] firstPart = parts[0] == string.Empty ? new byte[0] : HexStringToByteArray(parts[0]);
			int findPosition = firstPart.Length == 0 ? currentPosition : FindBytes(data, firstPart, currentPosition);

			if (findPosition == -1)
			{
				return -1; // Not found
			}

			// Try to match all parts
			bool goodMatch = true;
			int checkPosition = findPosition;

			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i] == string.Empty)
				{
					checkPosition += 1; // Skip the wildcard byte
					continue;
				}

				byte[] partBytes = HexStringToByteArray(parts[i]);
				if (checkPosition + partBytes.Length > data.Length)
				{
					goodMatch = false;
					break;
				}

				for (int j = 0; j < partBytes.Length; j++)
				{
					if (data[checkPosition + j] != partBytes[j])
					{
						goodMatch = false;
						break;
					}
				}

				if (!goodMatch)
					break;

				checkPosition += partBytes.Length;

				// Add 1 for the wildcard byte unless it's the last part
				if (i < parts.Length - 1)
					checkPosition += 1;
			}

			if (goodMatch)
				return findPosition;

			// Try next position
			currentPosition = findPosition + 1;
		}
	}

	/// <summary>
	/// Basic byte array search
	/// </summary>
	private static int FindBytes(byte[] source, byte[] pattern, int startIndex = 0)
	{
		if (pattern.Length == 0)
			return startIndex;

		for (int i = startIndex; i <= source.Length - pattern.Length; i++)
		{
			bool found = true;
			for (int j = 0; j < pattern.Length; j++)
			{
				if (source[i + j] != pattern[j])
				{
					found = false;
					break;
				}
			}
			if (found)
				return i;
		}
		return -1;
	}

	/// <summary>
	/// Converts a hex string to a byte array
	/// </summary>
	private static byte[] HexStringToByteArray(string hex)
	{
		int length = hex.Length / 2;
		byte[] bytes = new byte[length];
		for (int i = 0; i < length; i++)
		{
			bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
		}
		return bytes;
	}

	/// <summary>
	/// Converts a byte array to a hex string
	/// </summary>
	private static string ByteArrayToHexString(byte[] bytes, int startIndex, int length)
	{
		StringBuilder hex = new StringBuilder(length * 2);
		for (int i = startIndex; i < startIndex + length && i < bytes.Length; i++)
		{
			hex.AppendFormat("{0:x2}", bytes[i]);
		}
		return hex.ToString();
	}

	/// <summary>
	/// Resolves the correct file path for patching based on installation type.
	/// For 32-bit installations, DLLs in bin/ are actually located in garrysmod/bin/
	/// </summary>
	private static string ResolveFilePath(string installPath, string fileName, string installType)
	{
		// Normalize the path separators
		string normalizedFileName = fileName.Replace('/', Path.DirectorySeparatorChar);
		
		// Default path
		string defaultPath = Path.Combine(installPath, normalizedFileName);
		
		// For 32-bit installations, check if the file is in bin/ and should be in garrysmod/bin/
		if (installType == "gmod_main" || installType == "gmod_i386")
		{
			// Check if the path starts with bin\ and contains .dll
			if (normalizedFileName.StartsWith("bin" + Path.DirectorySeparatorChar) && 
			    (normalizedFileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
			     normalizedFileName.EndsWith(".so", StringComparison.OrdinalIgnoreCase)))
			{
				// Try garrysmod/bin/ first
				string garrysmodPath = Path.Combine(installPath, "garrysmod", normalizedFileName);
				if (File.Exists(garrysmodPath))
				{
					return garrysmodPath;
				}
			}
		}
		
		// Return the default path for all other cases
		return defaultPath;
	}
}

