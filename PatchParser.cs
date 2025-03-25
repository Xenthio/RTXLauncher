using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

//--------------------------------------------------------------------------------
// PatchParser.cs (revised excerpt — replace your existing class with this)
// Removes the need for (?<!\\) by using a naive placeholder approach.
//--------------------------------------------------------------------------------


namespace RTXLauncher
{
	/// <summary>
	/// Parses Python-style patch definitions into C# objects
	/// </summary>
	public static class PatchParser
	{
		/// <summary>
		/// Represents a parsed patch dictionary (for 32-bit or 64-bit).
		/// </summary>
		public class PatchDictionary
		{
			// Each file => list of patch entries.
			// Each patch entry => a List<object> that can contain patterns or patch hex.
			public Dictionary<string, List<List<object>>> Patches { get; set; } = new Dictionary<string, List<List<object>>>();
		}

		/// <summary>
		/// Extracts just the patch dictionaries (patches32 and patches64) from a Python script.
		/// </summary>
		public static string ExtractPatchDictionaries(string pythonScript)
		{
			var result = new StringBuilder();

			// Extract the "patches32" block
			ExtractDictionary(pythonScript, "patches32", result);

			// Extract the "patches64" block
			ExtractDictionary(pythonScript, "patches64", result);

			return result.ToString();
		}

		/// <summary>
		/// Parses the combined patch string into two PatchDictionaries: (patches32, patches64).
		/// </summary>
		public static (PatchDictionary, PatchDictionary) ParsePatches(string patchString)
		{
			// 1) Find the body for patches32:
			string dict32 = FindDictionaryBody(patchString, "patches32");

			// 2) Find the body for patches64:
			string dict64 = FindDictionaryBody(patchString, "patches64");

			// 3) Parse each into a PatchDictionary:
			PatchDictionary patches32 = ParseSingleDictionary(dict32);
			PatchDictionary patches64 = ParseSingleDictionary(dict64);

			return (patches32, patches64);
		}

		#region Dictionary Extraction Helpers

		/// <summary>
		/// Extracts the dictionary assignment block (e.g. patches32 = {...}) and appends to StringBuilder.
		/// </summary>
		private static void ExtractDictionary(string pythonScript, string dictName, StringBuilder sb)
		{
			// We look for something like:
			//   patches32 = {
			//     'bin/file.dll': [...]
			//   }
			// We'll attempt to return the whole chunk from "patches32 =" up to the matching "}".
			string block = ExtractBlock(pythonScript, dictName + " =", '{', '}');
			if (!string.IsNullOrEmpty(block))
			{
				sb.AppendLine(block);
				sb.AppendLine(); // extra spacing
			}
		}

		/// <summary>
		/// From the combined patch string, finds just the body of a dictionary ("patches32" or "patches64").
		/// Returns the text from '{' up to the corresponding '}'.
		/// </summary>
		private static string FindDictionaryBody(string patchString, string dictName)
		{
			string block = ExtractBlock(patchString, dictName + " =", '{', '}');
			if (string.IsNullOrEmpty(block)) return block;

			// Remove everything up to the first '{', so the result starts at '{'.
			int brace = block.IndexOf('{');
			if (brace >= 0)
				block = block.Substring(brace);

			return block;
		}

		/// <summary>
		/// Naive "find the matching braces" extraction starting from startMarker, 
		/// then from the next occurrence of openingBrace to its matching closingBrace.
		/// </summary>
		private static string ExtractBlock(string text, string startMarker, char openingBrace, char closingBrace)
		{
			int startIndex = text.IndexOf(startMarker);
			if (startIndex < 0) return null;

			// Find the {
			int braceStart = text.IndexOf(openingBrace, startIndex);
			if (braceStart < 0) return null;

			int depth = 0;
			int pos = braceStart;
			for (; pos < text.Length; pos++)
			{
				if (text[pos] == openingBrace) depth++;
				else if (text[pos] == closingBrace) depth--;
				if (depth == 0) break; // matched
			}
			if (depth != 0) return null; // no matching brace

			int length = (pos - startIndex) + 1; // includes the closingBrace
			return text.Substring(startIndex, length);
		}

		#endregion

		#region Parsing the Python Dictionary Blocks

		/// <summary>
		/// Tries to parse a single dictionary block "{ 'bin/...': [...], ... }" into a PatchDictionary.
		/// </summary>
		private static PatchDictionary ParseSingleDictionary(string dictText)
		{
			PatchDictionary result = new PatchDictionary();
			if (string.IsNullOrWhiteSpace(dictText))
				return result; // no data

			// 1) Remove comments
			dictText = RemovePythonComments(dictText);

			// 2) Convert Python syntax -> JSON
			string asJson = PythonDictToJson(dictText);

			// 3) Parse the JSON
			try
			{
				using (var doc = JsonDocument.Parse(asJson))
				{
					if (doc.RootElement.ValueKind != JsonValueKind.Object)
						return result;

					foreach (var fileProp in doc.RootElement.EnumerateObject())
					{
						string fileName = fileProp.Name;
						if (fileProp.Value.ValueKind != JsonValueKind.Array)
							continue;

						// Each item in this array is itself an array describing a patch or set of patches
						var patchList = new List<List<object>>();
						foreach (var patchEntry in fileProp.Value.EnumerateArray())
						{
							if (patchEntry.ValueKind == JsonValueKind.Array)
							{
								// Convert JSON array -> List<object>
								var patchData = JsonArrayToListOfObjects(patchEntry);
								patchList.Add(patchData);
							}
						}
						// Store in dictionary
						if (patchList.Count > 0)
						{
							result.Patches[fileName] = patchList;
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"ParseSingleDictionary error: {ex.Message}");
			}
			return result;
		}

		/// <summary>
		/// Converts a JSON array element into a List<object> recursively.
		/// This matches what PatchingSystem expects (list-of-lists).
		/// </summary>
		private static List<object> JsonArrayToListOfObjects(JsonElement arr)
		{
			var list = new List<object>();
			foreach (var item in arr.EnumerateArray())
			{
				switch (item.ValueKind)
				{
					case JsonValueKind.Array:
						// Nested array => recurse
						list.Add(JsonArrayToListOfObjects(item));
						break;
					case JsonValueKind.String:
						list.Add(item.GetString());
						break;
					case JsonValueKind.Number:
						if (item.TryGetInt32(out int iVal)) list.Add(iVal);
						else if (item.TryGetInt64(out long lVal)) list.Add(lVal);
						else list.Add(item.GetDouble());
						break;
					case JsonValueKind.True:
					case JsonValueKind.False:
						list.Add(item.GetBoolean());
						break;
					case JsonValueKind.Undefined:
					case JsonValueKind.Null:
						list.Add(null);
						break;
					default:
						// Object or other stuff we don't expect in a patch definition
						break;
				}
			}
			return list;
		}

		#endregion

		#region Helpers: Remove Comments & Convert Python→JSON

		/// <summary>
		/// Removes everything from '#' to the end of each line.
		/// </summary>
		private static string RemovePythonComments(string text)
		{
			var sb = new StringBuilder();
			using (var reader = new StringReader(text))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					int cmt = line.IndexOf('#');
					if (cmt >= 0) line = line.Substring(0, cmt);
					if (!string.IsNullOrWhiteSpace(line))
						sb.AppendLine(line);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Converts a Python dictionary snippet (with single quotes, tuples, etc.)
		/// into a rough JSON equivalent. Avoids lookbehind to prevent regex errors.
		/// </summary>
		private static string PythonDictToJson(string pythonDict)
		{
			// Trim whitespace
			pythonDict = pythonDict.Trim();

			// 1) Replace escaped single-quotes ('\') with placeholder to avoid collision
			//    Then replace unescaped single quotes with double quotes, restore placeholders as escaped quotes.
			pythonDict = ReplaceSingleQuotesNaive(pythonDict);

			// 2) Convert Python tuples ( ) → JSON arrays [ ]
			pythonDict = pythonDict.Replace('(', '[').Replace(')', ']');

			// 3) Remove any trailing commas before a ] or }
			pythonDict = Regex.Replace(pythonDict, @",(\s*[\]\}])", "$1");

			// In principle, we should now have a JSON-like object that starts with '{' and ends with '}'.
			return pythonDict;
		}

		/// <summary>
		/// Simple approach to convert single quotes → double quotes so JSON parsing can work.
		/// Avoids negative lookbehind by using placeholders for \'
		/// </summary>
		private static string ReplaceSingleQuotesNaive(string text)
		{
			// Step 1: replace all instances of \' with a placeholder, e.g. [ESC_QUOTE]
			text = text.Replace("\\'", "[ESC_QUOTE]");

			// Step 2: replace all ' with "
			text = text.Replace("'", "\"");

			// Step 3: restore [ESC_QUOTE] to \"
			text = text.Replace("[ESC_QUOTE]", "\\\"");

			return text;
		}

		#endregion
	}
}