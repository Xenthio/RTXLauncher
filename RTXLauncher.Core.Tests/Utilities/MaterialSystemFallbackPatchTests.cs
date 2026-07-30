using System.Buffers.Binary;
using RTXLauncher.Core.Utilities;

namespace RTXLauncher.Core.Tests.Utilities;

public class MaterialSystemFallbackPatchTests
{
	private const int PeOffset = 0x80;
	private const int OptionalHeaderSize = 0xF0;
	private const int SectionTableOffset = PeOffset + 24 + OptionalHeaderSize;
	private const int TextSectionHeaderOffset = SectionTableOffset;
	private const int TextRawOffset = 0x400;
	private const int TextRawSize = 0x3000;
	private const int TextVirtualAddress = 0x1000;
	private const int TextVirtualSize = 0x2F69;

	private const int InitializeEntryOffset = TextRawOffset + 0x100;
	private const int InitializeContextOffset = TextRawOffset + 0x180;
	private const int FallbackCallOffset = TextRawOffset + 0x220;
	private const int FallbackCallOpcodeOffset = FallbackCallOffset + 9;
	private const int EpilogueOffset = TextRawOffset + 0x300;
	private const int LoadVmtFileOffset = TextRawOffset + 0x600;

	[Fact]
	public void Apply_InstallsCompleteGuard()
	{
		byte[] original = BuildSupportedImage();

		MaterialSystemFallbackPatchResult result = MaterialSystemFallbackPatch.Apply(original);

		Assert.Equal(MaterialSystemFallbackPatchStatus.Applied, result.Status);
		Assert.NotSame(original, result.Bytes);
		Assert.Equal(0x2F9A, BinaryPrimitives.ReadInt32LittleEndian(
			result.Bytes.AsSpan(TextSectionHeaderOffset + 8, 4)));

		byte[] expectedCounterPatch =
		[
			0xC6, 0x84, 0x24, 0x80, 0x0B, 0x00, 0x00, 0x00,
			0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90
		];
		Assert.Equal(
			expectedCounterPatch,
			result.Bytes.AsSpan(InitializeContextOffset + 15, expectedCounterPatch.Length).ToArray());

		int stubOffset = TextRawOffset + 0x2F70;
		Assert.Equal(0xE8, result.Bytes[FallbackCallOffset]);
		Assert.Equal(
			RawToRva(stubOffset),
			DecodeRelativeTarget(result.Bytes, FallbackCallOffset));
		Assert.All(
			result.Bytes.AsSpan(FallbackCallOffset + 5, 9).ToArray(),
			value => Assert.Equal(0x90, value));

		Assert.Equal(
			[0x80, 0xBC, 0x24, 0x88, 0x0B, 0x00, 0x00, 0x10],
			result.Bytes.AsSpan(stubOffset, 8).ToArray());
		Assert.Equal(
			RawToRva(LoadVmtFileOffset),
			DecodeRelativeTarget(result.Bytes, stubOffset + 26));
		Assert.Equal(
			"GMRTXFB1",
			System.Text.Encoding.ASCII.GetString(result.Bytes, stubOffset + 34, 8));

		Assert.Equal(0, original[stubOffset]);
		Assert.Equal(TextVirtualSize, BinaryPrimitives.ReadInt32LittleEndian(
			original.AsSpan(TextSectionHeaderOffset + 8, 4)));
	}

	[Fact]
	public void Apply_IsIdempotent()
	{
		MaterialSystemFallbackPatchResult first =
			MaterialSystemFallbackPatch.Apply(BuildSupportedImage());

		MaterialSystemFallbackPatchResult second =
			MaterialSystemFallbackPatch.Apply(first.Bytes);

		Assert.True(
			second.Status == MaterialSystemFallbackPatchStatus.AlreadyApplied,
			second.Message);
		Assert.Same(first.Bytes, second.Bytes);
	}

	[Fact]
	public void Apply_RejectsAmbiguousInitializeShaderWithoutChangingInput()
	{
		byte[] image = BuildSupportedImage();
		Buffer.BlockCopy(
			image,
			InitializeEntryOffset,
			image,
			TextRawOffset + 0x700,
			18);
		byte[] before = (byte[])image.Clone();

		MaterialSystemFallbackPatchResult result = MaterialSystemFallbackPatch.Apply(image);

		Assert.Equal(MaterialSystemFallbackPatchStatus.Unsupported, result.Status);
		Assert.Same(image, result.Bytes);
		Assert.Equal(before, image);
	}

	[Fact]
	public void Apply_RejectsInsufficientCodeCaveWithoutChangingInput()
	{
		byte[] image = BuildSupportedImage();
		BinaryPrimitives.WriteInt32LittleEndian(
			image.AsSpan(TextSectionHeaderOffset + 8, 4),
			TextRawSize - 8);
		byte[] before = (byte[])image.Clone();

		MaterialSystemFallbackPatchResult result = MaterialSystemFallbackPatch.Apply(image);

		Assert.Equal(MaterialSystemFallbackPatchStatus.Unsupported, result.Status);
		Assert.Same(image, result.Bytes);
		Assert.Equal(before, image);
	}

	private static byte[] BuildSupportedImage()
	{
		byte[] image = new byte[0x3600];

		BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(0, 2), 0x5A4D);
		BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x3C, 4), PeOffset);

		BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(PeOffset, 4), 0x00004550);
		BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(PeOffset + 4, 2), 0x8664);
		BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(PeOffset + 6, 2), 2);
		BinaryPrimitives.WriteUInt16LittleEndian(
			image.AsSpan(PeOffset + 20, 2),
			OptionalHeaderSize);

		int optionalHeaderOffset = PeOffset + 24;
		BinaryPrimitives.WriteUInt16LittleEndian(
			image.AsSpan(optionalHeaderOffset, 2),
			0x20B);
		BinaryPrimitives.WriteInt32LittleEndian(
			image.AsSpan(optionalHeaderOffset + 32, 4),
			0x1000);

		WriteSection(
			image,
			TextSectionHeaderOffset,
			".text",
			TextVirtualSize,
			TextVirtualAddress,
			TextRawSize,
			TextRawOffset,
			0x60000020);
		WriteSection(
			image,
			TextSectionHeaderOffset + 40,
			".rdata",
			0x100,
			0x4000,
			0x200,
			0x3400,
			0x40000040);

		WriteBytes(image, InitializeEntryOffset,
			"40 53 55 56 57 41 54 41 56 41 57 48 81 EC 90 0B 00 00");
		WriteBytes(image, InitializeContextOffset,
			"40 32 FF 4C 89 AC 24 88 0B 00 00 89 7C 24 40 " +
			"0F 1F 40 00 66 66 66 0F 1F 84 00 00 00 00 00 " +
			"48 8B 0D 11 22 33 44");
		WriteBytes(image, FallbackCallOffset,
			"48 C7 44 24 20 00 00 00 00 E8 00 00 00 00 84 C0 75 36 49 8B 06");
		BinaryPrimitives.WriteInt32LittleEndian(
			image.AsSpan(FallbackCallOpcodeOffset + 1, 4),
			RawToRva(LoadVmtFileOffset) - (RawToRva(FallbackCallOpcodeOffset) + 5));
		WriteBytes(image, EpilogueOffset,
			"48 81 C4 90 0B 00 00 41 5F 41 5E 41 5C 5F 5E 5D 5B C3");
		WriteBytes(image, LoadVmtFileOffset,
			"48 89 5C 24 20 55 56 57 41 56 41 57 48 81 EC 60 01 00 00");

		return image;
	}

	private static void WriteSection(
		byte[] image,
		int offset,
		string name,
		int virtualSize,
		int virtualAddress,
		int rawSize,
		int rawOffset,
		uint characteristics)
	{
		System.Text.Encoding.ASCII.GetBytes(name).CopyTo(image, offset);
		BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(offset + 8, 4), virtualSize);
		BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(offset + 12, 4), virtualAddress);
		BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(offset + 16, 4), rawSize);
		BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(offset + 20, 4), rawOffset);
		BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(offset + 36, 4), characteristics);
	}

	private static void WriteBytes(byte[] image, int offset, string hex)
	{
		byte[] bytes = hex
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(value => Convert.ToByte(value, 16))
			.ToArray();
		Buffer.BlockCopy(bytes, 0, image, offset, bytes.Length);
	}

	private static int DecodeRelativeTarget(byte[] image, int instructionOffset)
	{
		int displacement = BinaryPrimitives.ReadInt32LittleEndian(
			image.AsSpan(instructionOffset + 1, 4));
		return RawToRva(instructionOffset) + 5 + displacement;
	}

	private static int RawToRva(int rawOffset)
	{
		return TextVirtualAddress + rawOffset - TextRawOffset;
	}
}
