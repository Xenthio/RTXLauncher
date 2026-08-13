using System.Text;
using RTXLauncher.Core.Services;

namespace RTXLauncher.Core.Tests.Services;

public class MountConfigServiceTests
{
	[Fact]
	public void UpdateManagedEntries_PreservesRequestedPriorityAndManualEntries()
	{
		const string original =
			"\"mountcfg\"\r\n{\r\n" +
			"\t\"episodic\"\t\t\"C:\\Games\\HL2\\episodic\"\r\n" +
			"\t\"rtxlauncher_hl2rtx_old\"\t\t\"C:\\Old\"\r\n" +
			"}\r\n" +
			"// DO NOT start comments in between { and }, because braces here are not syntax\r\n";
		KeyValuePair<string, string>[] entries =
		[
			new("rtxlauncher_hl2rtx_custom_zeta", "C:\\HL2RTX\\custom\\zeta"),
			new(Hl2RtxMountService.BasePathId, "C:\\HL2RTX\\hl2rtx"),
			new(Hl2RtxMountService.Hl2MiscPathId, "C:\\HL2RTX\\hl2")
		];

		var updated = MountConfigService.UpdateManagedEntries(original, entries);

		Assert.Contains("\"episodic\"", updated);
		Assert.DoesNotContain("rtxlauncher_hl2rtx_old", updated);
		Assert.True(
			updated.IndexOf($"\"{entries[0].Key}\"", StringComparison.Ordinal) <
			updated.IndexOf($"\"{entries[1].Key}\"", StringComparison.Ordinal));
		Assert.True(
			updated.IndexOf($"\"{entries[1].Key}\"", StringComparison.Ordinal) <
			updated.IndexOf($"\"{entries[2].Key}\"", StringComparison.Ordinal));
		Assert.True(
			updated.IndexOf($"\"{entries[2].Key}\"", StringComparison.Ordinal) <
			updated.IndexOf("}\r\n// DO NOT", StringComparison.Ordinal));
		Assert.EndsWith(
			"// DO NOT start comments in between { and }, because braces here are not syntax\r\n",
			updated);
		Assert.DoesNotContain("\n", updated.Replace("\r\n", string.Empty));
	}

	[Fact]
	public void UpdateManagedEntries_IsIdempotent()
	{
		const string original = "\"mountcfg\"\n{\n}\n";
		KeyValuePair<string, string>[] entries =
		[
			new(Hl2RtxMountService.BasePathId, "/games/hl2rtx")
		];

		var first = MountConfigService.UpdateManagedEntries(original, entries);
		var second = MountConfigService.UpdateManagedEntries(first, entries);

		Assert.Equal(first, second);
	}

	[Fact]
	public void RemoveManagedEntries_LeavesUnmanagedMounts()
	{
		const string original =
			"\"mountcfg\"\n{\n" +
			"\t\"ep2\" \"C:/HL2/ep2\"\n" +
			"\t\"rtxlauncher_hl2rtx\" \"C:/HL2RTX/hl2rtx\"\n" +
			"}\n";

		var updated = MountConfigService.RemoveManagedEntries(original);

		Assert.Contains("\"ep2\"", updated);
		Assert.DoesNotContain(MountConfigService.ManagedPrefix, updated);
	}

	[Fact]
	public void ContainsManagedEntry_AcceptsCrLfConfig()
	{
		const string content =
			"\"mountcfg\"\r\n{\r\n" +
			"\t\"rtxlauncher_hl2rtx\" \"C:/HL2RTX/hl2rtx\"\r\n" +
			"}\r\n";

		Assert.True(MountConfigService.ContainsManagedEntry(
			content, Hl2RtxMountService.BasePathId));
	}

	[Fact]
	public void TryGetEntryValue_ReturnsConfiguredPath()
	{
		const string content =
			"\"mountcfg\"\r\n{\r\n" +
			"\t\"rtxlauncher_hl2rtx\" \"C:/HL2RTX/hl2rtx\"\r\n" +
			"}\r\n";

		var found = MountConfigService.TryGetEntryValue(
			content, Hl2RtxMountService.BasePathId, out var value);

		Assert.True(found);
		Assert.Equal("C:/HL2RTX/hl2rtx", value);
	}

	[Fact]
	public void EncodeDecode_PreservesUtf8Bom()
	{
		var encoding = new UTF8Encoding(true);
		var bytes = MountConfigService.Encode("\"mountcfg\"\r\n{\r\n}\r\n", encoding);

		var decoded = MountConfigService.Decode(bytes, out var decodedEncoding);
		var encodedAgain = MountConfigService.Encode(decoded, decodedEncoding);

		Assert.Equal(encoding.GetPreamble(), bytes[..encoding.GetPreamble().Length]);
		Assert.Equal(bytes, encodedAgain);
	}

	[Fact]
	public void UpdateManagedEntries_RejectsMalformedConfig()
	{
		Assert.Throws<InvalidDataException>(() =>
			MountConfigService.UpdateManagedEntries(
				"\"mountcfg\"\n{\n",
				[new(Hl2RtxMountService.BasePathId, "C:/HL2RTX")]));
	}
}
