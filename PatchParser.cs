using System.Text;
using System.Text.RegularExpressions;

namespace RTXLauncher
{

	/// <summary>
	/// Parses Python-style patch definitions into C# objects
	/// </summary>
	public static class PatchParser
	{
		/// <summary>
		/// Represents a parsed patch dictionary (for 32-bit or 64-bit)
		/// </summary>
		public class PatchDictionary
		{
			public Dictionary<string, List<List<object>>> Patches { get; set; } = new Dictionary<string, List<List<object>>>();
		}
		private static void DebugLog(string message, Action<string, int> progressCallback = null)
		{
			Console.WriteLine(message);
			progressCallback?.Invoke(message, 10);
		}
		/// <summary>
		/// Parse the Python-style patch string into patch dictionaries
		/// </summary>
		public static (PatchDictionary Patches32, PatchDictionary Patches64) ParsePatches(
	string patchString, Action<string, int> progressCallback = null)
		{
			var patches32 = new PatchDictionary();
			var patches64 = new PatchDictionary();

			try
			{
				// Process the patch string line by line
				string[] allLines = patchString.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
				DebugLog($"Starting to parse {allLines.Length} lines", progressCallback);

				// Remove comments from lines
				List<string> lines = allLines
					.Select(line => line.Contains('#') ? line.Substring(0, line.IndexOf('#')).Trim() : line.Trim())
					.Where(line => !string.IsNullOrWhiteSpace(line))
					.ToList();

				DebugLog($"After removing comments: {lines.Count} lines", progressCallback);

				string currentDict = null;
				string currentFile = null;

				// Parse each line
				for (int i = 0; i < lines.Count; i++)
				{
					string line = lines[i];

					// Detect dictionary start
					if (line.StartsWith("patches32") || line.StartsWith("patches64"))
					{
						currentDict = line.StartsWith("patches32") ? "patches32" : "patches64";
						DebugLog($"Found dictionary: {currentDict}", progressCallback);
						continue;
					}

					// Detect file name
					if (line.Contains("':") || line.EndsWith("',"))
					{
						// Extract file name between quotes
						Match fileMatch = Regex.Match(line, @"'([^']+)'");
						if (fileMatch.Success)
						{
							currentFile = fileMatch.Groups[1].Value;
							DebugLog($"Found file: {currentFile} in {currentDict}", progressCallback);

							// Initialize the file list in the appropriate dictionary
							if (currentDict == "patches32")
								patches32.Patches[currentFile] = new List<List<object>>();
							else if (currentDict == "patches64")
								patches64.Patches[currentFile] = new List<List<object>>();
						}
						continue;
					}

					try
					{
						// Parse patch entry
						if ((line.StartsWith("[[(") || line.StartsWith("    [[(")) ||
							(line.StartsWith("[(") || line.StartsWith("    [(")) ||
							(line.StartsWith("[('") || line.StartsWith("    [('")) ||
							(line.StartsWith("[[") || line.StartsWith("    [[")))
						{
							// This is a patch line - it could be multi-line
							string patchBlockText = line;

							// Check if the patch continues on next lines
							while (!patchBlockText.EndsWith("],") && i + 1 < lines.Count)
							{
								i++;
								patchBlockText += " " + lines[i].Trim();
							}

							// Parse the patch block
							List<object> patch = ParsePatchBlock(patchBlockText, progressCallback);
							if (patch != null)
							{
								if (currentDict == "patches32" && currentFile != null)
								{
									patches32.Patches[currentFile].Add(patch);
									DebugLog($"Added patch for {currentFile} in patches32: {patchBlockText.Substring(0, Math.Min(30, patchBlockText.Length))}...", progressCallback);
								}
								else if (currentDict == "patches64" && currentFile != null)
								{
									patches64.Patches[currentFile].Add(patch);
									DebugLog($"Added patch for {currentFile} in patches64: {patchBlockText.Substring(0, Math.Min(30, patchBlockText.Length))}...", progressCallback);
								}
							}
							else
							{
								DebugLog($"Failed to parse patch block: {patchBlockText}", progressCallback);
							}
						}
					}
					catch (Exception ex)
					{
						DebugLog($"Error parsing line '{line}': {ex.Message}", progressCallback);
						// Continue with the next line
					}
				}

				// Print summary
				DebugLog($"Parsed {patches32.Patches.Count} files for 32-bit patches", progressCallback);
				foreach (var file in patches32.Patches.Keys)
				{
					DebugLog($"  - {file}: {patches32.Patches[file].Count} patches", progressCallback);
				}

				DebugLog($"Parsed {patches64.Patches.Count} files for 64-bit patches", progressCallback);
				foreach (var file in patches64.Patches.Keys)
				{
					DebugLog($"  - {file}: {patches64.Patches[file].Count} patches", progressCallback);
				}
			}
			catch (Exception ex)
			{
				DebugLog($"Error parsing patches: {ex.Message}", progressCallback);
			}

			return (patches32, patches64);
		}

		/// <summary>
		/// Parse a single patch block line
		/// </summary>
		private static List<object> ParsePatchBlock(string blockText, Action<string, int> progressCallback = null)
		{
			try
			{
				List<object> result = new List<object>();

				// Remove leading/trailing whitespace and the trailing comma
				blockText = blockText.Trim();
				if (blockText.EndsWith(","))
					blockText = blockText.Substring(0, blockText.Length - 1);

				// Remove the outer brackets if they exist
				if (blockText.StartsWith("[") && blockText.EndsWith("]"))
					blockText = blockText.Substring(1, blockText.Length - 2).Trim();

				// Check if this is a multi-pattern block (starts with another '[')
				if (blockText.StartsWith("["))
				{
					// This is a list of patterns or a complex pattern

					// Find the position of the first '],' which ends the pattern list
					int endPos = blockText.IndexOf("],");

					if (endPos != -1)
					{
						// There's a replacement after the pattern list
						string patternListText = blockText.Substring(0, endPos + 1);
						string replacementText = blockText.Substring(endPos + 2).Trim();

						// Parse the pattern list
						List<object> patternList = new List<object>();

						// Is it a multi-pattern? (multi-pattern contains multiple '(' characters)
						if (patternListText.Count(c => c == '(') > 1)
						{
							// Split the pattern list into individual patterns
							string[] patternTexts = SplitPatternList(patternListText);
							foreach (string patternText in patternTexts)
							{
								List<object> pattern = ParsePatternTuple(patternText, progressCallback);
								if (pattern != null)
									patternList.Add(pattern);
							}
						}
						else
						{
							// It's a single pattern
							List<object> pattern = ParsePatternTuple(patternListText, progressCallback);
							if (pattern != null)
								patternList.Add(pattern);
						}

						// Add the pattern list and replacement
						result.Add(patternList);

						// Parse replacement (remove quotes)
						replacementText = replacementText.Trim('\'', '"');
						result.Add(replacementText);
					}
					else
					{
						// The entire block is a pattern list
						List<object> patternList = new List<object>();

						// Split the pattern list into individual patterns
						string[] patternTexts = SplitPatternList(blockText);
						foreach (string patternText in patternTexts)
						{
							List<object> pattern = ParsePatternTuple(patternText, progressCallback);
							if (pattern != null)
								patternList.Add(pattern);
						}

						// Add the pattern list
						result.Add(patternList);
					}
				}
				else
				{
					// This is a simple pattern and replacement
					string[] parts = blockText.Split(new string[] { "), " }, StringSplitOptions.None);

					if (parts.Length == 2)
					{
						// Parse pattern tuple
						List<object> patternTuple = ParsePatternTuple(parts[0] + ")", progressCallback);
						if (patternTuple != null)
							result.Add(patternTuple);

						// Parse replacement (remove quotes)
						string replacementText = parts[1].Trim('\'', '"');
						result.Add(replacementText);
					}
				}

				return result.Count > 0 ? result : null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error parsing patch block '{blockText}': {ex.Message}");
				progressCallback?.Invoke($"Error parsing patch block: {ex.Message}", 10);
				return null;
			}
		}

		/// <summary>
		/// Split a pattern list into individual patterns
		/// </summary>
		private static string[] SplitPatternList(string patternListText)
		{
			// Remove outer brackets
			patternListText = patternListText.Trim();
			if (patternListText.StartsWith("[") && patternListText.EndsWith("]"))
				patternListText = patternListText.Substring(1, patternListText.Length - 2).Trim();

			// Current tuple
			StringBuilder currentTuple = new StringBuilder();
			List<string> tuples = new List<string>();

			// Track nesting level
			int parenthesisLevel = 0;

			for (int i = 0; i < patternListText.Length; i++)
			{
				char c = patternListText[i];

				if (c == '(')
					parenthesisLevel++;
				else if (c == ')')
					parenthesisLevel--;

				// Add character to current tuple
				currentTuple.Append(c);

				// If we've completed a tuple and are at a comma, add it to the list
				if (parenthesisLevel == 0 && (c == ',' || i == patternListText.Length - 1))
				{
					// Check if we've reached the end of a tuple
					string tuple = currentTuple.ToString().Trim();
					if (tuple.EndsWith(","))
						tuple = tuple.Substring(0, tuple.Length - 1).Trim();

					if (!string.IsNullOrWhiteSpace(tuple))
						tuples.Add(tuple);

					currentTuple.Clear();
				}
			}

			return tuples.ToArray();
		}

		/// <summary>
		/// Parse a pattern tuple like "('pattern', 0, 'replacement')"
		/// </summary>
		private static List<object> ParsePatternTuple(string tupleText, Action<string, int> progressCallback = null)
		{
			List<object> result = new List<object>();

			// Remove the parentheses
			tupleText = tupleText.Trim();
			if (tupleText.StartsWith("(") && tupleText.EndsWith(")"))
				tupleText = tupleText.Substring(1, tupleText.Length - 2).Trim();

			// Split by commas, keeping track of string literals
			List<string> parts = new List<string>();
			StringBuilder currentPart = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < tupleText.Length; i++)
			{
				char c = tupleText[i];

				if ((c == '\'' || c == '"') && (i == 0 || tupleText[i - 1] != '\\'))
					inQuotes = !inQuotes;

				if (c == ',' && !inQuotes)
				{
					parts.Add(currentPart.ToString().Trim());
					currentPart.Clear();
				}
				else
				{
					currentPart.Append(c);
				}
			}

			// Add the last part
			if (currentPart.Length > 0)
				parts.Add(currentPart.ToString().Trim());

			// Parse each part
			foreach (string part in parts)
			{
				try
				{
					if (part.StartsWith("'") || part.StartsWith("\""))
					{
						// String literal - remove quotes
						result.Add(part.Trim('\'', '"'));
					}
					else
					{
						// Try to parse as integer
						string trimmedPart = part.Trim();
						if (int.TryParse(trimmedPart, out int intValue))
						{
							result.Add(intValue);
						}
						else
						{
							// Log the error but continue with a default value (0)
							Console.WriteLine($"Warning: Could not parse '{trimmedPart}' as integer in tuple {tupleText}, using 0");
							progressCallback?.Invoke($"Warning: Could not parse '{trimmedPart}' as integer, using 0", 10);
							result.Add(0);
						}
					}
				}
				catch (Exception ex)
				{
					// Log the error but continue
					Console.WriteLine($"Error parsing part '{part}' in tuple {tupleText}: {ex.Message}");
					progressCallback?.Invoke($"Error parsing part '{part}': {ex.Message}", 10);
				}
			}

			return result.Count > 0 ? result : null;
		}

		public static string ExtractPatchDictionaries(string pythonFileContent)
		{
			StringBuilder extractedPatches = new StringBuilder();

			// Extract patches32 dictionary
			int patches32Start = pythonFileContent.IndexOf("patches32 = {");
			if (patches32Start >= 0)
			{
				int braceLevel = 0;
				bool inDictionary = false;
				int startPos = patches32Start;

				// Find the end of the dictionary by tracking braces
				for (int i = startPos; i < pythonFileContent.Length; i++)
				{
					char c = pythonFileContent[i];

					if (c == '{')
					{
						if (!inDictionary) inDictionary = true;
						braceLevel++;
					}
					else if (c == '}')
					{
						braceLevel--;
						if (braceLevel == 0 && inDictionary)
						{
							// Found the end of the dictionary, extract it
							string patches32Dict = pythonFileContent.Substring(startPos, i - startPos + 1);
							extractedPatches.AppendLine("# 32-bit patches");
							extractedPatches.AppendLine("patches32 = " + patches32Dict);
							extractedPatches.AppendLine();
							break;
						}
					}
				}
			}

			// Extract patches64 dictionary
			int patches64Start = pythonFileContent.IndexOf("patches64 = {");
			if (patches64Start >= 0)
			{
				int braceLevel = 0;
				bool inDictionary = false;
				int startPos = patches64Start;

				// Find the end of the dictionary by tracking braces
				for (int i = startPos; i < pythonFileContent.Length; i++)
				{
					char c = pythonFileContent[i];

					if (c == '{')
					{
						if (!inDictionary) inDictionary = true;
						braceLevel++;
					}
					else if (c == '}')
					{
						braceLevel--;
						if (braceLevel == 0 && inDictionary)
						{
							// Found the end of the dictionary, extract it
							string patches64Dict = pythonFileContent.Substring(startPos, i - startPos + 1);
							extractedPatches.AppendLine("# 64-bit patches");
							extractedPatches.AppendLine("patches64 = " + patches64Dict);
							break;
						}
					}
				}
			}

			// For debugging, print what we extracted
			string extracted = extractedPatches.ToString();
			Console.WriteLine($"Extracted patch dictionaries ({extracted.Length} chars)");

			// If we couldn't extract patches, throw an exception
			if (extracted.Length == 0)
			{
				throw new Exception("Could not find patch dictionaries in the Python file.");
			}

			return extracted;
		}
	}
}
