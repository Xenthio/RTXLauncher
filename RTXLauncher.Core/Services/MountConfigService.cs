using System.Text;
using System.Text.RegularExpressions;

namespace RTXLauncher.Core.Services;

public static class MountConfigService
{
	public const string ManagedPrefix = "rtxlauncher_hl2rtx";

	private static readonly Regex ManagedEntryPattern = new(
		$@"(?m)^[ \t]*""{ManagedPrefix}[^""]*""[ \t]+""[^""]*""[ \t]*(?:\r?\n|$)",
		RegexOptions.CultureInvariant);

	public static string UpdateManagedEntries(
		string content,
		IEnumerable<KeyValuePair<string, string>> entries)
	{
		var newline = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";
		var withoutManagedEntries = ManagedEntryPattern.Replace(content, string.Empty);
		var closingBrace = FindMountCfgClosingBrace(withoutManagedEntries);
		if (closingBrace < 0)
		{
			throw new InvalidDataException(
				"mount.cfg does not contain a complete mountcfg block.");
		}

		var orderedEntries = entries.ToArray();
		if (orderedEntries.Length == 0)
		{
			return withoutManagedEntries;
		}

		var block = new StringBuilder();
		if (closingBrace > 0 && withoutManagedEntries[closingBrace - 1] != '\n')
		{
			block.Append(newline);
		}
		foreach (var entry in orderedEntries)
		{
			ValidateEntry(entry.Key, entry.Value);
			block.Append('\t')
				.Append('"').Append(entry.Key).Append('"')
				.Append("\t\t")
				.Append('"').Append(entry.Value).Append('"')
				.Append(newline);
		}

		return withoutManagedEntries.Insert(closingBrace, block.ToString());
	}

	public static string RemoveManagedEntries(string content) =>
		ManagedEntryPattern.Replace(content, string.Empty);

	public static bool ContainsManagedEntry(string content, string key) =>
		TryGetEntryValue(content, key, out _);

	public static bool TryGetEntryValue(
		string content, string key, out string value)
	{
		var match = Regex.Match(
			content,
			$@"(?m)^[ \t]*""{Regex.Escape(key)}""[ \t]+""(?<value>[^""]*)""[ \t]*\r?$",
			RegexOptions.CultureInvariant);
		value = match.Success ? match.Groups["value"].Value : string.Empty;
		return match.Success;
	}

	public static byte[] ReadBytesOrDefault(string path)
	{
		if (File.Exists(path))
		{
			return File.ReadAllBytes(path);
		}

		return new UTF8Encoding(false).GetBytes(
			"\"mountcfg\"\r\n{\r\n}\r\n");
	}

	public static string Decode(byte[] bytes, out Encoding encoding)
	{
		if (bytes.Length >= 3 && bytes[0] == 0xEF &&
			bytes[1] == 0xBB && bytes[2] == 0xBF)
		{
			encoding = new UTF8Encoding(true);
			return encoding.GetString(bytes, 3, bytes.Length - 3);
		}
		if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
		{
			encoding = new UnicodeEncoding(false, true);
			return encoding.GetString(bytes, 2, bytes.Length - 2);
		}
		if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
		{
			encoding = new UnicodeEncoding(true, true);
			return encoding.GetString(bytes, 2, bytes.Length - 2);
		}

		encoding = new UTF8Encoding(false);
		return encoding.GetString(bytes);
	}

	public static byte[] Encode(string content, Encoding encoding)
	{
		var payload = encoding.GetBytes(content);
		var preamble = encoding.GetPreamble();
		if (preamble.Length == 0)
		{
			return payload;
		}

		var result = new byte[preamble.Length + payload.Length];
		Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
		Buffer.BlockCopy(payload, 0, result, preamble.Length, payload.Length);
		return result;
	}

	public static void WriteAtomic(string path, byte[] bytes)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var temporaryPath = path + $".rtxlauncher-{Guid.NewGuid():N}.tmp";
		try
		{
			File.WriteAllBytes(temporaryPath, bytes);
			File.Move(temporaryPath, path, true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	private static void ValidateEntry(string key, string value)
	{
		if (!key.StartsWith(ManagedPrefix, StringComparison.Ordinal) ||
			key.Contains('"') || value.Contains('"') ||
			key.Contains('\r') || key.Contains('\n') ||
			value.Contains('\r') || value.Contains('\n'))
		{
			throw new InvalidDataException("Invalid managed mount.cfg entry.");
		}
	}

	private static int FindMountCfgClosingBrace(string content)
	{
		var header = Regex.Match(
			content,
			@"""mountcfg""\s*\{",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!header.Success)
		{
			return -1;
		}

		var openingBrace = content.IndexOf('{', header.Index);
		var depth = 0;
		var inString = false;
		var inLineComment = false;
		for (var index = openingBrace; index < content.Length; index++)
		{
			var character = content[index];
			if (inLineComment)
			{
				if (character == '\r' || character == '\n')
				{
					inLineComment = false;
				}
				continue;
			}

			if (!inString && character == '/' &&
				index + 1 < content.Length && content[index + 1] == '/')
			{
				inLineComment = true;
				index++;
				continue;
			}

			if (character == '"')
			{
				inString = !inString;
				continue;
			}
			if (inString)
			{
				continue;
			}

			if (character == '{')
			{
				depth++;
			}
			else if (character == '}' && --depth == 0)
			{
				return index;
			}
		}

		return -1;
	}
}
