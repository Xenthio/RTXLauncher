using System.Buffers.Binary;
using System.Text;

namespace RTXLauncher.Core.Utilities;

public enum MaterialSystemFallbackPatchStatus
{
	Applied,
	AlreadyApplied,
	Unsupported
}

public sealed record MaterialSystemFallbackPatchResult(
	MaterialSystemFallbackPatchStatus Status,
	byte[] Bytes,
	string Message);

/// <summary>
/// Applies a defensive x64 materialsystem.dll patch that limits recursive
/// $fallbackmaterial loads during CMaterial::InitializeShader.
///
/// The patch is intentionally all-or-nothing. Every function signature,
/// call target, patch site, and code-cave byte is validated before a cloned
/// image is changed.
/// </summary>
public static class MaterialSystemFallbackPatch
{
	public const string RelativePath = "bin/win64/materialsystem.dll";

	private const int MaximumFallbackLoads = 16;
	private const int MaximumInitializeShaderSize = 0x1000;

	private static readonly byte[] Marker = Encoding.ASCII.GetBytes("GMRTXFB1");
	private static readonly byte[] InitializeShaderPrologue =
	[
		0x40, 0x53, 0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x56,
		0x41, 0x57, 0x48, 0x81, 0xEC, 0x90, 0x0B, 0x00, 0x00
	];
	private static readonly byte[] InitializeCounterPatch =
	[
		0xC6, 0x84, 0x24, 0x80, 0x0B, 0x00, 0x00, 0x00,
		0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90
	];
	private static readonly byte[] LoadVmtFilePrologue =
	[
		0x48, 0x89, 0x5C, 0x24, 0x20, 0x55, 0x56, 0x57, 0x41, 0x56,
		0x41, 0x57, 0x48, 0x81, 0xEC, 0x60, 0x01, 0x00, 0x00
	];
	private static readonly byte[] InitializeShaderEpilogue =
	[
		0x48, 0x81, 0xC4, 0x90, 0x0B, 0x00, 0x00, 0x41, 0x5F,
		0x41, 0x5E, 0x41, 0x5C, 0x5F, 0x5E, 0x5D, 0x5B, 0xC3
	];
	private static readonly byte[] CounterDisplacement = [0x80, 0x0B, 0x00, 0x00];

	private const string InitializeContextPattern =
		"40 32 FF 4C 89 AC 24 88 0B 00 00 89 7C 24 40 " +
		"0F 1F 40 00 66 66 66 0F 1F 84 00 00 00 00 00 " +
		"48 8B 0D ?? ?? ?? ??";
	private const int InitializePatchOffset = 15;

	private const string FallbackCallPattern =
		"48 C7 44 24 20 00 00 00 00 E8 ?? ?? ?? ?? 84 C0 75 ?? 49 8B 06";
	private const int FallbackCallOpcodeOffset = 9;
	private const int FallbackCallReplacementLength = 14;

	private const string PatchedFallbackCallPattern =
		"E8 ?? ?? ?? ?? 90 90 90 90 90 90 90 90 90 84 C0 75 ?? 49 8B 06";

	public static MaterialSystemFallbackPatchResult Apply(byte[] image)
	{
		ArgumentNullException.ThrowIfNull(image);

		if (!TryReadTextSection(image, out PeTextSection text, out string reason))
			return Unsupported(image, reason);

		int markerOffset = FindUnique(image, text.RawOffset, text.RawSize, Marker, out bool markerAmbiguous);
		if (markerAmbiguous)
			return Unsupported(image, "The static fallback-guard marker appears more than once.");

		if (markerOffset >= 0)
			return ValidateAlreadyApplied(image, text, markerOffset);

		int searchableTextSize = Math.Min(text.VirtualSize, text.RawSize);
		int initializeEntry = FindUnique(
			image,
			text.RawOffset,
			searchableTextSize,
			InitializeShaderPrologue,
			out bool entryAmbiguous);
		if (initializeEntry < 0 || entryAmbiguous)
			return Unsupported(image, entryAmbiguous
				? "CMaterial::InitializeShader prologue is ambiguous."
				: "CMaterial::InitializeShader prologue was not found.");

		int functionSearchLength = Math.Min(
			MaximumInitializeShaderSize,
			text.RawOffset + searchableTextSize - initializeEntry);

		int initializeContext = FindUniqueMasked(
			image,
			initializeEntry,
			functionSearchLength,
			InitializeContextPattern,
			out bool initializeContextAmbiguous);
		if (initializeContext < 0 || initializeContextAmbiguous)
			return Unsupported(image, initializeContextAmbiguous
				? "InitializeShader counter initialization site is ambiguous."
				: "InitializeShader counter initialization site was not found.");

		int initializePatchOffset = initializeContext + InitializePatchOffset;
		if (!Matches(image, initializePatchOffset, ParsePattern(
			"0F 1F 40 00 66 66 66 0F 1F 84 00 00 00 00 00")))
		{
			return Unsupported(image, "InitializeShader no longer has the expected 15-byte NOP patch site.");
		}

		int fallbackCall = FindUniqueMasked(
			image,
			initializeEntry,
			functionSearchLength,
			FallbackCallPattern,
			out bool fallbackCallAmbiguous);
		if (fallbackCall < 0 || fallbackCallAmbiguous)
			return Unsupported(image, fallbackCallAmbiguous
				? "InitializeShader fallback load call is ambiguous."
				: "InitializeShader fallback load call was not found.");

		int originalFallbackCallOpcode = fallbackCall + FallbackCallOpcodeOffset;
		if (fallbackCall <= initializePatchOffset)
			return Unsupported(image, "InitializeShader patch sites are in an unexpected order.");

		int epilogue = FindUnique(
			image,
			initializeEntry,
			functionSearchLength,
			InitializeShaderEpilogue,
			out bool epilogueAmbiguous);
		if (epilogue < 0 || epilogueAmbiguous || epilogue <= fallbackCall)
			return Unsupported(image, "InitializeShader epilogue could not be validated.");

		int counterReference = FindUnique(
			image,
			initializeEntry,
			epilogue + InitializeShaderEpilogue.Length - initializeEntry,
			CounterDisplacement,
			out bool counterReferenceAmbiguous);
		if (counterReference >= 0 || counterReferenceAmbiguous)
			return Unsupported(image, "InitializeShader already references the proposed fallback counter stack slot.");

		if (!TryDecodeRelativeTarget(image, originalFallbackCallOpcode, text, out int loadVmtFileRva))
			return Unsupported(image, "The fallback material load is no longer a direct in-section call.");

		if (!TryRvaToRaw(loadVmtFileRva, text, out int loadVmtFileOffset) ||
			!Matches(image, loadVmtFileOffset, LoadVmtFilePrologue))
		{
			return Unsupported(image, "The fallback call target is not the expected LoadVMTFile function.");
		}

		int stubSectionOffset = AlignUp(text.VirtualSize, 16);
		if (stubSectionOffset < 0 ||
			(long)text.VirtualAddress + stubSectionOffset > int.MaxValue)
		{
			return Unsupported(image, "The .text section layout cannot address a fallback guard.");
		}

		byte[] stub = BuildStub(text.VirtualAddress + stubSectionOffset, loadVmtFileRva);
		int payloadLength = stub.Length + Marker.Length;
		if (stubSectionOffset > text.RawSize - payloadLength)
		{
			return Unsupported(image, "The .text section has no room for the fallback guard.");
		}

		int newVirtualSize = stubSectionOffset + payloadLength;
		int alignedNewVirtualSize = AlignUp(newVirtualSize, text.SectionAlignment);
		if (newVirtualSize > text.RawSize ||
			alignedNewVirtualSize < 0 ||
			alignedNewVirtualSize > text.NextSectionOffset)
		{
			return Unsupported(image, "The fallback guard would overlap another PE section.");
		}

		int stubOffset = text.RawOffset + stubSectionOffset;
		for (int i = 0; i < payloadLength; i++)
		{
			byte value = image[stubOffset + i];
			if (value != 0x00 && value != 0xCC)
				return Unsupported(image, "The proposed .text code cave is not empty.");
		}

		int fallbackCallRva = RawToRva(fallbackCall, text);
		int stubRva = text.VirtualAddress + stubSectionOffset;
		if (!TryGetRelativeDisplacement(fallbackCallRva + 5, stubRva, out int callDisplacement))
			return Unsupported(image, "The fallback guard is outside CALL rel32 range.");

		byte[] patchedImage = (byte[])image.Clone();
		Buffer.BlockCopy(InitializeCounterPatch, 0, patchedImage, initializePatchOffset, InitializeCounterPatch.Length);

		patchedImage[fallbackCall] = 0xE8;
		BinaryPrimitives.WriteInt32LittleEndian(
			patchedImage.AsSpan(fallbackCall + 1, sizeof(int)),
			callDisplacement);
		patchedImage.AsSpan(
			fallbackCall + 5,
			FallbackCallReplacementLength - 5).Fill(0x90);

		Buffer.BlockCopy(stub, 0, patchedImage, stubOffset, stub.Length);
		Buffer.BlockCopy(Marker, 0, patchedImage, stubOffset + stub.Length, Marker.Length);
		BinaryPrimitives.WriteInt32LittleEndian(
			patchedImage.AsSpan(text.SectionHeaderOffset + 8, sizeof(int)),
			newVirtualSize);

		return new MaterialSystemFallbackPatchResult(
			MaterialSystemFallbackPatchStatus.Applied,
			patchedImage,
			$"Installed a {MaximumFallbackLoads}-load $fallbackmaterial recursion guard.");
	}

	private static MaterialSystemFallbackPatchResult ValidateAlreadyApplied(
		byte[] image,
		PeTextSection text,
		int markerOffset)
	{
		const int stubLength = 34;
		int stubOffset = markerOffset - stubLength;
		if (stubOffset < text.RawOffset)
			return Unsupported(image, "The static fallback-guard marker has an invalid location.");

		byte[] expectedStubPrefix =
		[
			0x80, 0xBC, 0x24, 0x88, 0x0B, 0x00, 0x00, MaximumFallbackLoads,
			0x73, 0x15,
			0xFE, 0x84, 0x24, 0x88, 0x0B, 0x00, 0x00,
			0x48, 0xC7, 0x44, 0x24, 0x28, 0x00, 0x00, 0x00, 0x00
		];
		if (!Matches(image, stubOffset, expectedStubPrefix) ||
			image[stubOffset + 26] != 0xE9 ||
			!Matches(image, stubOffset + 31, [0x33, 0xC0, 0xC3]))
		{
			return Unsupported(image, "The static fallback-guard marker exists, but its code stub is invalid.");
		}

		int searchableTextSize = Math.Min(text.VirtualSize, text.RawSize);
		int initializeEntry = FindUnique(
			image,
			text.RawOffset,
			searchableTextSize,
			InitializeShaderPrologue,
			out bool entryAmbiguous);
		if (initializeEntry < 0 || entryAmbiguous)
			return Unsupported(image, "The patched InitializeShader function could not be validated.");

		int functionSearchLength = Math.Min(
			MaximumInitializeShaderSize,
			text.RawOffset + searchableTextSize - initializeEntry);
		int counterPatch = FindUnique(
			image,
			initializeEntry,
			functionSearchLength,
			InitializeCounterPatch,
			out bool counterPatchAmbiguous);
		if (counterPatch < 0 || counterPatchAmbiguous)
			return Unsupported(image, "The patched fallback counter initialization is invalid.");

		int patchedCall = FindUniqueMasked(
			image,
			initializeEntry,
			functionSearchLength,
			PatchedFallbackCallPattern,
			out bool patchedCallAmbiguous);
		if (patchedCall < 0 || patchedCallAmbiguous)
			return Unsupported(image, "The patched fallback guard call is invalid.");

		if (!TryDecodeRelativeTarget(image, patchedCall, text, out int callTargetRva) ||
			callTargetRva != RawToRva(stubOffset, text))
		{
			return Unsupported(image, "The patched fallback guard call does not target its code stub.");
		}

		if (!TryDecodeRelativeTarget(image, stubOffset + 26, text, out int loadVmtFileRva) ||
			!TryRvaToRaw(loadVmtFileRva, text, out int loadVmtFileOffset) ||
			!Matches(image, loadVmtFileOffset, LoadVmtFilePrologue))
		{
			return Unsupported(image, "The patched fallback guard no longer targets LoadVMTFile.");
		}

		int payloadEnd = markerOffset + Marker.Length - text.RawOffset;
		if (text.VirtualSize < payloadEnd)
			return Unsupported(image, "The .text virtual size does not cover the fallback guard.");

		return new MaterialSystemFallbackPatchResult(
			MaterialSystemFallbackPatchStatus.AlreadyApplied,
			image,
			"The static $fallbackmaterial recursion guard is already installed.");
	}

	private static byte[] BuildStub(int stubRva, int loadVmtFileRva)
	{
		// The replacement CALL pushes its return address, so the caller's unused
		// [rsp+B80] byte is [rsp+B88] here. Likewise, LoadVMTFile's fifth stack
		// argument belongs at [rsp+28]. A tail JMP preserves its original return
		// address and avoids introducing a new unwind frame.
		byte[] stub =
		[
			0x80, 0xBC, 0x24, 0x88, 0x0B, 0x00, 0x00, MaximumFallbackLoads,
			0x73, 0x15,
			0xFE, 0x84, 0x24, 0x88, 0x0B, 0x00, 0x00,
			0x48, 0xC7, 0x44, 0x24, 0x28, 0x00, 0x00, 0x00, 0x00,
			0xE9, 0x00, 0x00, 0x00, 0x00,
			0x33, 0xC0, 0xC3
		];

		if (!TryGetRelativeDisplacement(stubRva + 31, loadVmtFileRva, out int displacement))
			throw new InvalidOperationException("LoadVMTFile is outside JMP rel32 range.");

		BinaryPrimitives.WriteInt32LittleEndian(stub.AsSpan(27, sizeof(int)), displacement);
		return stub;
	}

	private static MaterialSystemFallbackPatchResult Unsupported(byte[] image, string reason)
	{
		return new MaterialSystemFallbackPatchResult(
			MaterialSystemFallbackPatchStatus.Unsupported,
			image,
			reason);
	}

	private static bool TryReadTextSection(
		byte[] image,
		out PeTextSection text,
		out string reason)
	{
		text = default;
		reason = string.Empty;

		if (image.Length < 0x40 ||
			BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(0, 2)) != 0x5A4D)
		{
			reason = "File is not a valid PE image.";
			return false;
		}

		int peOffset = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(0x3C, 4));
		if (!IsRangeValid(peOffset, 24, image.Length) ||
			BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(peOffset, 4)) != 0x00004550)
		{
			reason = "PE header is invalid.";
			return false;
		}

		ushort machine = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(peOffset + 4, 2));
		ushort sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(peOffset + 6, 2));
		ushort optionalHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(peOffset + 20, 2));
		int optionalHeaderOffset = peOffset + 24;
		if (machine != 0x8664 ||
			sectionCount == 0 ||
			sectionCount > 96 ||
			!IsRangeValid(optionalHeaderOffset, optionalHeaderSize, image.Length) ||
			optionalHeaderSize < 0x70 ||
			BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(optionalHeaderOffset, 2)) != 0x20B)
		{
			reason = "Only a valid AMD64 PE32+ image is supported.";
			return false;
		}

		int sectionAlignment = BinaryPrimitives.ReadInt32LittleEndian(
			image.AsSpan(optionalHeaderOffset + 32, 4));
		if (sectionAlignment <= 0)
		{
			reason = "PE section alignment is invalid.";
			return false;
		}

		int sectionTableOffset = optionalHeaderOffset + optionalHeaderSize;
		if (!IsRangeValid(sectionTableOffset, sectionCount * 40, image.Length))
		{
			reason = "PE section table is invalid.";
			return false;
		}

		int textSectionHeader = -1;
		int textVirtualSize = 0;
		int textVirtualAddress = 0;
		int textRawSize = 0;
		int textRawOffset = 0;
		int nextSectionOffset = int.MaxValue;

		for (int i = 0; i < sectionCount; i++)
		{
			int sectionHeader = sectionTableOffset + (i * 40);
			string name = Encoding.ASCII.GetString(image, sectionHeader, 8).TrimEnd('\0');
			int virtualSize = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(sectionHeader + 8, 4));
			int virtualAddress = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(sectionHeader + 12, 4));
			int rawSize = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(sectionHeader + 16, 4));
			int rawOffset = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(sectionHeader + 20, 4));
			uint characteristics = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(sectionHeader + 36, 4));

			if (name == ".text")
			{
				if (textSectionHeader >= 0)
				{
					reason = "PE image contains multiple .text sections.";
					return false;
				}

				if (virtualSize <= 0 ||
					virtualAddress <= 0 ||
					rawSize <= 0 ||
					rawOffset < 0 ||
					(long)virtualAddress + rawSize > int.MaxValue ||
					!IsRangeValid(rawOffset, rawSize, image.Length) ||
					(characteristics & 0x20000000) == 0)
				{
					reason = "PE .text section is invalid or not executable.";
					return false;
				}

				textSectionHeader = sectionHeader;
				textVirtualSize = virtualSize;
				textVirtualAddress = virtualAddress;
				textRawSize = rawSize;
				textRawOffset = rawOffset;
			}
		}

		if (textSectionHeader < 0)
		{
			reason = "PE image has no .text section.";
			return false;
		}

		for (int i = 0; i < sectionCount; i++)
		{
			int sectionHeader = sectionTableOffset + (i * 40);
			int virtualAddress = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(sectionHeader + 12, 4));
			if (virtualAddress > textVirtualAddress)
				nextSectionOffset = Math.Min(nextSectionOffset, virtualAddress - textVirtualAddress);
		}

		if (nextSectionOffset == int.MaxValue)
			nextSectionOffset = AlignUp(textRawSize, sectionAlignment);
		if (nextSectionOffset <= 0)
		{
			reason = "PE .text section has an invalid virtual extent.";
			return false;
		}

		text = new PeTextSection(
			textSectionHeader,
			textVirtualSize,
			textVirtualAddress,
			textRawSize,
			textRawOffset,
			sectionAlignment,
			nextSectionOffset);
		return true;
	}

	private static int FindUnique(
		byte[] data,
		int start,
		int length,
		byte[] pattern,
		out bool ambiguous)
	{
		ambiguous = false;
		if (!IsRangeValid(start, length, data.Length) || pattern.Length == 0 || pattern.Length > length)
			return -1;

		int found = -1;
		int end = start + length - pattern.Length;
		for (int offset = start; offset <= end; offset++)
		{
			if (!Matches(data, offset, pattern))
				continue;

			if (found >= 0)
			{
				ambiguous = true;
				return found;
			}

			found = offset;
		}

		return found;
	}

	private static int FindUniqueMasked(
		byte[] data,
		int start,
		int length,
		string patternText,
		out bool ambiguous)
	{
		MaskedByte[] pattern = ParseMaskedPattern(patternText);
		ambiguous = false;
		if (!IsRangeValid(start, length, data.Length) || pattern.Length == 0 || pattern.Length > length)
			return -1;

		int found = -1;
		int end = start + length - pattern.Length;
		for (int offset = start; offset <= end; offset++)
		{
			bool matches = true;
			for (int i = 0; i < pattern.Length; i++)
			{
				if (pattern[i].IsWildcard)
					continue;

				if (data[offset + i] != pattern[i].Value)
				{
					matches = false;
					break;
				}
			}

			if (!matches)
				continue;

			if (found >= 0)
			{
				ambiguous = true;
				return found;
			}

			found = offset;
		}

		return found;
	}

	private static bool Matches(byte[] data, int offset, byte[] pattern)
	{
		if (!IsRangeValid(offset, pattern.Length, data.Length))
			return false;

		return data.AsSpan(offset, pattern.Length).SequenceEqual(pattern);
	}

	private static byte[] ParsePattern(string patternText)
	{
		return patternText
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(token => Convert.ToByte(token, 16))
			.ToArray();
	}

	private static MaskedByte[] ParseMaskedPattern(string patternText)
	{
		return patternText
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(token => token == "??"
				? new MaskedByte(0, true)
				: new MaskedByte(Convert.ToByte(token, 16), false))
			.ToArray();
	}

	private static bool TryDecodeRelativeTarget(
		byte[] image,
		int instructionOffset,
		PeTextSection text,
		out int targetRva)
	{
		targetRva = 0;
		if (!IsRangeValid(instructionOffset, 5, image.Length) ||
			(image[instructionOffset] != 0xE8 && image[instructionOffset] != 0xE9))
		{
			return false;
		}

		int displacement = BinaryPrimitives.ReadInt32LittleEndian(
			image.AsSpan(instructionOffset + 1, sizeof(int)));
		long nextRva = (long)RawToRva(instructionOffset, text) + 5;
		long target = nextRva + displacement;
		if (target < text.VirtualAddress ||
			target >= (long)text.VirtualAddress + text.RawSize ||
			target > int.MaxValue)
		{
			return false;
		}

		targetRva = (int)target;
		return true;
	}

	private static bool TryRvaToRaw(int rva, PeTextSection text, out int rawOffset)
	{
		long sectionOffset = (long)rva - text.VirtualAddress;
		if (sectionOffset < 0 || sectionOffset >= text.RawSize)
		{
			rawOffset = 0;
			return false;
		}

		rawOffset = text.RawOffset + (int)sectionOffset;
		return true;
	}

	private static int RawToRva(int rawOffset, PeTextSection text)
	{
		return text.VirtualAddress + (rawOffset - text.RawOffset);
	}

	private static bool TryGetRelativeDisplacement(int nextInstructionRva, int targetRva, out int displacement)
	{
		long value = (long)targetRva - nextInstructionRva;
		if (value < int.MinValue || value > int.MaxValue)
		{
			displacement = 0;
			return false;
		}

		displacement = (int)value;
		return true;
	}

	private static int AlignUp(int value, int alignment)
	{
		if (value < 0 || alignment <= 0)
			return -1;

		long aligned = ((long)value + alignment - 1) / alignment * alignment;
		return aligned > int.MaxValue ? -1 : (int)aligned;
	}

	private static bool IsRangeValid(int offset, int length, int totalLength)
	{
		return offset >= 0 && length >= 0 && offset <= totalLength - length;
	}

	private readonly record struct MaskedByte(byte Value, bool IsWildcard);

	private readonly record struct PeTextSection(
		int SectionHeaderOffset,
		int VirtualSize,
		int VirtualAddress,
		int RawSize,
		int RawOffset,
		int SectionAlignment,
		int NextSectionOffset);
}
