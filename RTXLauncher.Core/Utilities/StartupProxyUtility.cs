using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTXLauncher.Core.Utilities;

/// <summary>Installs the optional, renderer-specific Windows x64 startup loader.</summary>
public static partial class StartupProxyUtility
{
	private sealed record Receipt(string RendererSha256, string[] ProxySha256);
	private sealed record Contract(
		[property: JsonPropertyName("version")] int Version,
		[property: JsonPropertyName("renderer_sha256")] string RendererSha256,
		[property: JsonPropertyName("renderer_bytes")] long RendererBytes,
		[property: JsonPropertyName("original_name")] string OriginalName);
	[JsonSerializable(typeof(Contract))]
	[JsonSerializable(typeof(Receipt))]
	private partial class ProxyJsonContext : JsonSerializerContext { }

	public static string Install(string installDirectory)
	{
		// Older packages and 32-bit installs do not carry this component. Do not
		// wrap Proton: this loader discovers subscriptions through Windows Steam.
		if (!OperatingSystem.IsWindows() || GarrysModUtility.GetInstallType(installDirectory) != "gmod_x86-64")
			return "Startup loader: not applicable to this installation.";
		var binary = Path.Combine(installDirectory, "bin", "win64");
		var package = Path.Combine(binary, "astra-startup");
		var proxy = Path.Combine(package, "d3d9.dll");
		var contractPath = Path.Combine(package, "proxy_renderer_exports.json");
		if (!File.Exists(proxy) && !File.Exists(contractPath))
			return "Startup loader: not included in this package.";
		if (!File.Exists(proxy) || !File.Exists(contractPath))
			throw new InvalidDataException("The startup loader package is incomplete. Reinstall the RTX fixes package.");

		var contract = JsonSerializer.Deserialize(File.ReadAllText(contractPath), ProxyJsonContext.Default.Contract);
		if (contract is null || contract.Version != 1 || !IsHash(contract.RendererSha256) ||
			contract.RendererBytes <= 0 || contract.OriginalName != "d3d9_astra_renderer.dll")
			throw new InvalidDataException("Invalid startup loader renderer contract.");
		var active = Path.Combine(binary, "d3d9.dll");
		// Preserve Remix's disabled state; toggling it back on restores this file.
		if (!File.Exists(active)) active += ".disabled";
		if (!File.Exists(active)) return "Startup loader: waiting for RTX Remix to be installed.";
		var original = Path.Combine(binary, "d3d9_astra_renderer.dll");
		var receiptPath = Path.Combine(binary, "astra-startup-installed.json");
		Receipt? receipt = null;
		if (File.Exists(receiptPath))
		{
			receipt = JsonSerializer.Deserialize(File.ReadAllText(receiptPath), ProxyJsonContext.Default.Receipt);
			if (receipt is null || !IsHash(receipt.RendererSha256) || receipt.ProxySha256 is null ||
				receipt.ProxySha256.Length is < 1 or > 2 || !receipt.ProxySha256.All(IsHash))
				throw new InvalidDataException("Invalid startup loader installation receipt.");
		}
		var activeHash = Hash(active);
		var proxyHash = Hash(proxy);
		var wrapped = receipt?.ProxySha256.Contains(activeHash, StringComparer.OrdinalIgnoreCase) == true;
		if (!wrapped && Equal(activeHash, proxyHash))
			throw new InvalidDataException("Startup proxy found without its installation receipt. Restore the original renderer before installing.");
		var renderer = wrapped ? original : active;
		if (wrapped && (!File.Exists(original) || !Equal(Hash(original), receipt!.RendererSha256)))
			throw new InvalidDataException("The startup loader's original renderer is missing or changed. Reinstall RTX Remix.");
		var rendererHash = wrapped ? receipt!.RendererSha256 : activeHash;
		if (!Equal(rendererHash, contract.RendererSha256) || new FileInfo(renderer).Length != contract.RendererBytes)
		{
			// A new fixes package may target a different Remix release. Restore the
			// verified original instead of leaving the previous proxy active.
			if (wrapped)
			{
				RequireGameClosed(binary);
				Replace(original, active);
			}
			return "Startup loader NOT enabled: this RTX Remix renderer does not match the fixes package. Install a matching Remix release for Workshop PBR assets.";
		}
		if (wrapped && Equal(activeHash, proxyHash)) return "Startup loader already installed and verified.";
		RequireGameClosed(binary);

		if (!wrapped && File.Exists(original))
		{
			var existingHash = Hash(original);
			if (!Equal(existingHash, rendererHash) && (receipt is null || !Equal(existingHash, receipt.RendererSha256)))
				throw new IOException("Refusing to overwrite an unrecognized d3d9_astra_renderer.dll. Back it up outside the game before installing.");
		}
		if (!wrapped) Replace(active, original);
		// Record both proxies while swapping, so an interrupted upgrade remains
		// recoverable with either the old or the new active DLL.
		var hashes = wrapped && !Equal(activeHash, proxyHash) ? new[] { proxyHash, activeHash } : new[] { proxyHash };
		WriteReceipt(receiptPath, new Receipt(rendererHash, hashes));
		Replace(proxy, active);
		return "Startup loader installed: Workshop PBR assets will be prepared before Remix starts.";
	}

	private static bool IsHash(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
	private static void RequireGameClosed(string binary)
	{
		var game = Path.GetFullPath(Path.Combine(binary, "gmod.exe"));
		foreach (var process in System.Diagnostics.Process.GetProcessesByName("gmod"))
		{
			using (process)
			{
				string? path;
				try { path = process.MainModule?.FileName; }
				catch (InvalidOperationException) { continue; } // Exited during enumeration.
				catch (System.ComponentModel.Win32Exception) { continue; }
				if (string.Equals(path, game, StringComparison.OrdinalIgnoreCase))
					throw new IOException("Close the target Garry's Mod game before installing the startup loader.");
			}
		}
	}
	private static bool Equal(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
	private static string Hash(string path)
	{
		using var input = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(input));
	}
	private static void Replace(string source, string destination)
	{
		var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.Copy(source, temporary);
			File.Move(temporary, destination, overwrite: true);
		}
		finally { if (File.Exists(temporary)) File.Delete(temporary); }
	}
	private static void WriteReceipt(string path, Receipt receipt)
	{
		var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(temporary, JsonSerializer.Serialize(receipt, ProxyJsonContext.Default.Receipt));
			File.Move(temporary, path, overwrite: true);
		}
		finally { if (File.Exists(temporary)) File.Delete(temporary); }
	}
}
