// Utilities/DxvkUtility.cs

namespace RTXLauncher.Core.Utilities;

/// <summary>
/// DXVK (doitsujin/dxvk): the shader API proxy runs the game's Direct3D 9 on
/// DXVK's d3d9.dll, which Windows has no copy of (Proton brings its own). The
/// 64-bit one goes in the game's root folder, beside gmod_win64.exe.
/// </summary>
public static class DxvkUtility
{
	public const string D3d9Name = "d3d9.dll";

	// PE machine type of x86-64 images.
	private const ushort MachineAmd64 = 0x8664;

	/// <summary>Where an installation's d3d9.dll goes: its root folder.</summary>
	public static string GetD3d9Path(string installDir) => Path.Combine(installDir, D3d9Name);

	/// <summary>Whether the file at a path is a 64-bit (x86-64) PE image.</summary>
	public static bool IsX64Dll(string path)
	{
		if (!File.Exists(path)) return false;
		try
		{
			using var reader = new BinaryReader(File.OpenRead(path));
			if (reader.BaseStream.Length < 0x40 || reader.ReadUInt16() != 0x5A4D) return false; // "MZ"
			reader.BaseStream.Position = 0x3C;
			var peOffset = reader.ReadInt32();
			if (peOffset < 0 || peOffset + 6 > reader.BaseStream.Length) return false;
			reader.BaseStream.Position = peOffset;
			if (reader.ReadUInt32() != 0x00004550) return false; // "PE\0\0"
			return reader.ReadUInt16() == MachineAmd64;
		}
		catch (IOException)
		{
			return false;
		}
	}

	/// <summary>
	/// The 64-bit d3d9.dll in an extracted DXVK release (x64/d3d9.dll), or null.
	/// </summary>
	public static string? FindX64D3d9(string extractedDir) =>
		Directory.GetFiles(extractedDir, D3d9Name, SearchOption.AllDirectories)
			.OrderByDescending(path => Path.GetFileName(Path.GetDirectoryName(path)) == "x64")
			.FirstOrDefault(IsX64Dll);

	/// <summary>Copies a 64-bit d3d9.dll into an installation's root folder.</summary>
	public static void Deploy(string d3d9Path, string installDir)
	{
		if (!IsX64Dll(d3d9Path))
			throw new InvalidDataException($"{d3d9Path} is not a 64-bit DLL.");
		File.Copy(d3d9Path, GetD3d9Path(installDir), overwrite: true);
	}
}
