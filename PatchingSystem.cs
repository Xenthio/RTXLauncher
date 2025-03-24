using System.Text;
using System.Text.RegularExpressions;

namespace RTXLauncher
{
	/// <summary>
	/// Applies binary patches to game files
	/// </summary>
	public static class PatchingSystem
	{
		/// <summary>
		/// Applies binary patches to the game files based on the installation type
		/// </summary>
		public static async Task ApplyPatches(string installPath, string patchString = null, Action<string, int> progressCallback = null)
		{
			progressCallback?.Invoke("Starting patching process...", 0);

			// Parse patches
			progressCallback?.Invoke("Parsing patch definitions...", 5);
			var (patches32, patches64) = PatchParser.ParsePatches(patchString);

			// Determine installation type
			string installType = GarrysModInstallSystem.GetInstallType(installPath);
			PatchParser.PatchDictionary patchesToUse;

			// Select appropriate patches based on install type
			if (installType == "gmod_x86-64")
			{
				patchesToUse = patches64;
				progressCallback?.Invoke("Detected 64-bit installation. Using 64-bit patches.", 10);
			}
			else if (installType == "gmod_main")
			{
				patchesToUse = patches32;
				progressCallback?.Invoke("Detected 32-bit installation. Using 32-bit patches.", 10);
			}
			else
			{
				throw new Exception($"Unknown installation type: {installType}");
			}

			// Create a patched directory
			string patchedDir = Path.Combine(installPath, "patched");
			Directory.CreateDirectory(patchedDir);

			// Load all the files to be patched
			Dictionary<string, byte[]> fileContents = new Dictionary<string, byte[]>();
			Dictionary<string, byte[]> modifiedFiles = new Dictionary<string, byte[]>();
			List<string> missingFiles = new List<string>();

			progressCallback?.Invoke("Loading files to patch...", 15);

			int fileCount = patchesToUse.Patches.Keys.Count;
			int currentFileIndex = 0;

			// Check for client.dll in game directories if not in base dir
			bool needToFindClientDll = false;
			string clientDllPath = "bin/client.dll";
			string clientDll64Path = "bin/win64/client.dll";

			if (patchesToUse.Patches.ContainsKey(clientDllPath) && !File.Exists(Path.Combine(installPath, clientDllPath)))
			{
				needToFindClientDll = true;
			}
			else if (patchesToUse.Patches.ContainsKey(clientDll64Path) && !File.Exists(Path.Combine(installPath, clientDll64Path)))
			{
				needToFindClientDll = true;
				clientDllPath = clientDll64Path; // Use the 64-bit path for searching
			}

			// If client.dll is missing, search for it in game directories
			if (needToFindClientDll)
			{
				bool found = false;
				foreach (string dir in Directory.GetDirectories(installPath))
				{
					string dirName = Path.GetFileName(dir);
					string testPath = Path.Combine(dir, "bin", "client.dll");
					string test64Path = Path.Combine(dir, "bin", "win64", "client.dll");

					if (File.Exists(testPath) && patchesToUse.Patches.ContainsKey(clientDllPath))
					{
						string newPath = $"{dirName}/bin/client.dll".Replace('\\', '/');
						patchesToUse.Patches[newPath] = patchesToUse.Patches[clientDllPath];
						patchesToUse.Patches.Remove(clientDllPath);
						found = true;
						progressCallback?.Invoke($"Found client.dll in {newPath}", 15);
						break;
					}
					else if (File.Exists(test64Path) && patchesToUse.Patches.ContainsKey(clientDll64Path))
					{
						string newPath = $"{dirName}/bin/win64/client.dll".Replace('\\', '/');
						patchesToUse.Patches[newPath] = patchesToUse.Patches[clientDll64Path];
						patchesToUse.Patches.Remove(clientDll64Path);
						found = true;
						progressCallback?.Invoke($"Found client.dll in {newPath}", 15);
						break;
					}
				}

				if (!found && patchesToUse.Patches.ContainsKey(clientDllPath))
				{
					missingFiles.Add(clientDllPath);
				}
			}

			// Load the files
			foreach (string fileName in patchesToUse.Patches.Keys)
			{
				string filePath = Path.Combine(installPath, fileName.Replace('/', Path.DirectorySeparatorChar));

				// Check if file exists
				if (!File.Exists(filePath))
				{
					missingFiles.Add(fileName);
					continue;
				}

				try
				{
					byte[] fileContent = File.ReadAllBytes(filePath);
					fileContents[fileName] = fileContent;
				}
				catch (Exception ex)
				{
					throw new Exception($"Error reading file {fileName}: {ex.Message}");
				}

				progressCallback?.Invoke($"Loaded file: {fileName}", 15 + (int)((float)++currentFileIndex / fileCount * 15));
			}

			// Check if any files are missing
			if (missingFiles.Count > 0)
			{
				throw new Exception($"Missing files: {string.Join(", ", missingFiles)}");
			}

			// Apply patches
			progressCallback?.Invoke("Applying patches...", 30);

			int totalPatches = patchesToUse.Patches.Sum(p => p.Value.Count);
			int completedPatches = 0;
			bool hasProblems = false;

			foreach (var filePair in patchesToUse.Patches)
			{
				string fileName = filePair.Key;
				List<List<object>> patches = filePair.Value;
				byte[] fileContent = fileContents[fileName];
				bool fileModified = false;

				progressCallback?.Invoke($"Patching {fileName}...", 30 + (int)((float)completedPatches / totalPatches * 60));

				foreach (List<object> patchData in patches)
				{
					bool patched = false;

					if (patchData.Count >= 2 && patchData[0] is List<object> patterns)
					{
						string patchHex = patchData[1] as string;

						// Check if it's a single pattern or multiple patterns
						if (patterns.Count > 0 && patterns[0] is List<object>)
						{
							// Multiple patterns - try each one
							for (int i = 0; i < patterns.Count; i++)
							{
								List<object> pattern = patterns[i] as List<object>;
								if (TryApplyPattern(fileName, fileContent, pattern, ref modifiedFiles, ref fileModified, patchHex, progressCallback, completedPatches, totalPatches))
								{
									patched = true;
									break;
								}
							}
						}
						else
						{
							// Single pattern
							patched = TryApplyPattern(fileName, fileContent, patterns, ref modifiedFiles, ref fileModified, patchHex, progressCallback, completedPatches, totalPatches);
						}
					}

					if (!patched)
					{
						hasProblems = true;
						progressCallback?.Invoke($"Warning: Failed to apply patch in {fileName}", 30 + (int)((float)completedPatches / totalPatches * 60));
					}

					completedPatches++;
				}
			}

			// Save patched files
			// Modify the file handling section in ApplyPatches in PatchingSystem.cs:

			// Save patched files
			progressCallback?.Invoke("Starting to apply patches to original files...", 90);

			// First create a backup directory with timestamp
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string backupDir = Path.Combine(installPath, $"backup_{timestamp}");
			Directory.CreateDirectory(backupDir);

			int savedFiles = 0;
			if (modifiedFiles.Count == 0)
			{
				progressCallback?.Invoke("No files were modified during patching. Verify patch patterns match the game files.", 95);
			}
			else
			{
				foreach (var filePair in modifiedFiles)
				{
					try
					{
						string fileName = filePair.Key;
						byte[] modifiedContent = filePair.Value;

						// Path to original file
						string originalPath = Path.Combine(installPath, fileName.Replace('/', Path.DirectorySeparatorChar));

						// Path for backup
						string backupPath = Path.Combine(backupDir, fileName.Replace('/', Path.DirectorySeparatorChar));
						string backupDirPath = Path.GetDirectoryName(backupPath);

						// Create backup directory structure if it doesn't exist
						if (!Directory.Exists(backupDirPath))
						{
							Directory.CreateDirectory(backupDirPath);
						}

						// Backup the original file
						progressCallback?.Invoke($"Backing up original file: {fileName}", 90 + (int)((float)savedFiles / modifiedFiles.Count * 4));
						File.Copy(originalPath, backupPath, true);

						// Make sure the original file is not read-only
						FileAttributes attributes = File.GetAttributes(originalPath);
						if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
						{
							attributes &= ~FileAttributes.ReadOnly;
							File.SetAttributes(originalPath, attributes);
						}

						// Replace original with patched version - do it properly with explicit file handles
						progressCallback?.Invoke($"Replacing with patched version: {fileName}", 94 + (int)((float)savedFiles / modifiedFiles.Count * 6));

						// Use File.Delete and File.WriteAllBytes to ensure the file is fully replaced
						File.Delete(originalPath);
						File.WriteAllBytes(originalPath, modifiedContent);

						// Verify that the file was actually replaced
						byte[] verifyContent = File.ReadAllBytes(originalPath);
						bool verified = verifyContent.Length == modifiedContent.Length;
						for (int i = 0; verified && i < verifyContent.Length; i++)
						{
							if (verifyContent[i] != modifiedContent[i])
							{
								verified = false;
								break;
							}
						}

						if (!verified)
						{
							throw new Exception("Error: File was not properly replaced. Try running the launcher as administrator.");
						}

						savedFiles++;
					}
					catch (Exception ex)
					{
						progressCallback?.Invoke($"Error applying patch to {filePair.Key}: {ex.Message}", 95);

						// If we have issues with permissions, suggest running as admin
						if (ex.Message.Contains("access") || ex.Message.Contains("denied"))
						{
							progressCallback?.Invoke("You may need to run the launcher as administrator to modify game files.", 95);
						}
					}
				}
			}

			if (savedFiles > 0)
			{
				progressCallback?.Invoke($"Patching completed successfully. Patched {savedFiles} files directly. Backups saved to {backupDir}.", 100);
			}
			else
			{
				progressCallback?.Invoke("Patching completed but no files were modified. Check for patch pattern matches.", 100);
			}
		}

		/// <summary>
		/// Try to apply a pattern to a file
		/// </summary>
		private static bool TryApplyPattern(
	string fileName,
	byte[] fileContent,
	List<object> pattern,
	ref Dictionary<string, byte[]> modifiedFiles,
	ref bool fileModified,
	string patchHex,
	Action<string, int> progressCallback,
	int completedPatches,
	int totalPatches)
		{
			try
			{
				if (pattern == null || pattern.Count < 2)
				{
					progressCallback?.Invoke("Invalid pattern (too few elements)", 30);
					return false;
				}

				// Get pattern and offset
				if (!(pattern[0] is string hexPattern))
				{
					progressCallback?.Invoke("Invalid pattern (first element not a string)", 30);
					return false;
				}

				int offset;
				if (pattern[1] is int offsetValue)
				{
					offset = offsetValue;
				}
				else
				{
					progressCallback?.Invoke("Invalid pattern (second element not an integer)", 30);
					return false;
				}

				// If there's a third element, it overrides the patchHex
				if (pattern.Count >= 3 && pattern[2] is string customPatchHex)
					patchHex = customPatchHex;

				progressCallback?.Invoke($"Looking for pattern: {hexPattern.Substring(0, Math.Min(20, hexPattern.Length))}...", 30);

				// Find the pattern in the file
				int position = FindWithMask(fileContent, hexPattern);

				if (position == -1)
				{
					progressCallback?.Invoke($"Pattern not found: {hexPattern.Substring(0, Math.Min(20, hexPattern.Length))}...", 30);
					return false;
				}

				// Check if pattern appears multiple times (should be unique)
				int position2 = FindWithMask(fileContent, hexPattern, position + 1);

				if (position2 != -1)
				{
					progressCallback?.Invoke($"Pattern found multiple times at 0x{position:X} and 0x{position2:X}, skipping...", 30);
					return false;
				}

				// Found unique match, apply patch
				position += offset;

				// Validate patch hex
				if (string.IsNullOrEmpty(patchHex) || !IsValidHexString(patchHex))
				{
					progressCallback?.Invoke($"Invalid patch hex: {patchHex}", 30);
					return false;
				}

				byte[] patchBytes = HexStringToByteArray(patchHex);

				// Make sure position is within bounds
				if (position < 0 || position + patchBytes.Length > fileContent.Length)
				{
					progressCallback?.Invoke($"Error: Patch position 0x{position:X} would exceed file length", 30);
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
				fileModified = true;

				// Log the patch with user-friendly description
				string description = GetPatchDescription(originalBytes, patchHex);
				progressCallback?.Invoke($"Patched at 0x{position:X}: {originalBytes} -> {patchHex} {description}",
					30 + (int)((float)completedPatches / totalPatches * 60));

				return true;
			}
			catch (Exception ex)
			{
				progressCallback?.Invoke($"Error applying pattern: {ex.Message}", 30);
				return false;
			}
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
				return "(Changed conditional jump to unconditional jump)";

			// Common patch: changing a JZ (74) to JMP (EB)
			if (original.StartsWith("74") && patch == "eb")
				return "(Changed zero jump to unconditional jump)";

			// Common patch: NOPs (90)
			if (patch.All(c => c == '9' || c == '0'))
				return "(NOP out instructions)";

			// Common patch: Return 0 (31C0C3 = xor eax,eax; ret)
			if (patch == "31c0c3")
				return "(Return 0 - function skip)";

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
	}
}
