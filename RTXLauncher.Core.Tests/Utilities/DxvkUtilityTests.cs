using RTXLauncher.Core.Models;
using RTXLauncher.Core.Services;
using RTXLauncher.Core.Utilities;
using System.Formats.Tar;
using System.IO.Compression;

namespace RTXLauncher.Core.Tests.Utilities;

public class DxvkUtilityTests : IDisposable
{
	private readonly string _testDirectory;
	private readonly string _installDir;

	private const ushort MachineAmd64 = 0x8664;
	private const ushort MachineI386 = 0x014C;

	public DxvkUtilityTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"RTXLauncherDxvkTests_{Guid.NewGuid()}");
		_installDir = Path.Combine(_testDirectory, "install");
		Directory.CreateDirectory(Path.Combine(_installDir, "bin", "win64"));
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, true);
		}
	}

	// The smallest file IsX64Dll reads: an MZ header pointing at a PE header
	// with the given machine type.
	private static byte[] FakePe(ushort machine)
	{
		var bytes = new byte[0x100];
		bytes[0] = (byte)'M';
		bytes[1] = (byte)'Z';
		BitConverter.GetBytes(0x80).CopyTo(bytes, 0x3C);
		"PE\0\0"u8.ToArray().CopyTo(bytes, 0x80);
		BitConverter.GetBytes(machine).CopyTo(bytes, 0x84);
		return bytes;
	}

	private string WriteFile(string relativePath, byte[] contents)
	{
		var path = Path.Combine(_testDirectory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllBytes(path, contents);
		return path;
	}

	[Fact]
	public void IsX64Dll_TellsTheArchitecture()
	{
		Assert.True(DxvkUtility.IsX64Dll(WriteFile("x64.dll", FakePe(MachineAmd64))));
		Assert.False(DxvkUtility.IsX64Dll(WriteFile("x32.dll", FakePe(MachineI386))));
		Assert.False(DxvkUtility.IsX64Dll(WriteFile("text.dll", "not a dll"u8.ToArray())));
		Assert.False(DxvkUtility.IsX64Dll(Path.Combine(_testDirectory, "missing.dll")));
	}

	[Fact]
	public void Deploy_RefusesA32BitDll()
	{
		var x32 = WriteFile("x32/d3d9.dll", FakePe(MachineI386));

		Assert.Throws<InvalidDataException>(() => DxvkUtility.Deploy(x32, _installDir));
		Assert.False(File.Exists(DxvkUtility.GetD3d9Path(_installDir)));
	}

	[Fact]
	public async Task InstallDxvkFromLocalArchive_PutsTheX64D3d9InTheGameRoot()
	{
		// As DXVK's releases are laid out: dxvk-<version>/{x32,x64}/d3d9.dll.
		var x64 = FakePe(MachineAmd64);
		WriteFile("release/dxvk-2.7/x32/d3d9.dll", FakePe(MachineI386));
		WriteFile("release/dxvk-2.7/x64/d3d9.dll", x64);
		WriteFile("release/dxvk-2.7/x64/dxgi.dll", FakePe(MachineAmd64));
		var archive = Path.Combine(_testDirectory, "dxvk-2.7.tar.gz");
		using (var file = File.Create(archive))
		using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
		{
			TarFile.CreateFromDirectory(Path.Combine(_testDirectory, "release"), gzip, includeBaseDirectory: false);
		}

		await new PackageInstallService().InstallDxvkFromLocalArchiveAsync(archive, _installDir, new Progress<InstallProgressReport>());

		Assert.Equal(x64, File.ReadAllBytes(DxvkUtility.GetD3d9Path(_installDir)));
		Assert.False(File.Exists(Path.Combine(_installDir, "dxgi.dll")));
		Assert.False(File.Exists(Path.Combine(_installDir, "bin", "win64", "d3d9.dll")));
	}
}
