// Utilities/ProxyUtility.cs

using System.Text;

namespace RTXLauncher.Core.Utilities;

/// <summary>
/// The OpenRemix shader API proxy (sambow23/garrys-mod-rtx-remixed-2): its
/// shaderapidx9.dll takes the place of the game's own in bin/win64, which it
/// loads as shaderapidx9_orig.dll. The proxy is told from the game's DLL by
/// that name, which only the proxy contains.
/// </summary>
public static class ProxyUtility
{
	public const string ProxyName = "shaderapidx9.dll";
	public const string OriginalName = "shaderapidx9_orig.dll";
	// The proxy while it is off: the game's own DLL is back under its name.
	public const string DisabledName = "shaderapidx9_openremix.dll.disabled";

	private static readonly byte[] Marker = Encoding.ASCII.GetBytes(OriginalName);

	/// <summary>The bin/win64 folder of an installation.</summary>
	public static string GetWin64Folder(string installDir) => Path.Combine(installDir, "bin", "win64");

	/// <summary>Whether the DLL at a path is the proxy (it names shaderapidx9_orig.dll).</summary>
	public static bool IsProxy(string dllPath)
	{
		if (!File.Exists(dllPath)) return false;
		var bytes = File.ReadAllBytes(dllPath);
		return bytes.AsSpan().IndexOf(Marker) >= 0;
	}

	/// <summary>Whether the proxy is in place, wrapping the game's own DLL.</summary>
	public static bool IsDeployed(string win64Dir) =>
		File.Exists(Path.Combine(win64Dir, OriginalName)) && IsProxy(Path.Combine(win64Dir, ProxyName));

	/// <summary>Whether the proxy is installed but turned off (SetEnabled).</summary>
	public static bool IsDisabled(string win64Dir) => File.Exists(Path.Combine(win64Dir, DisabledName));

	/// <summary>
	/// Puts the proxy in place. A shaderapidx9.dll that is not the proxy is the
	/// game's own (a fresh install, or one a game update replaced): it becomes
	/// shaderapidx9_orig.dll, replacing an older one.
	/// </summary>
	public static void Deploy(string proxyDllPath, string win64Dir)
	{
		var current = Path.Combine(win64Dir, ProxyName);
		var original = Path.Combine(win64Dir, OriginalName);
		var disabled = Path.Combine(win64Dir, DisabledName);

		if (File.Exists(current) && !IsProxy(current))
		{
			File.Move(current, original, overwrite: true);
		}
		if (!File.Exists(original))
		{
			throw new FileNotFoundException(
				$"The game's own {ProxyName} was not found in {win64Dir}: the proxy has nothing to wrap. " +
				"Verify the game's files and run the setup again.");
		}
		if (File.Exists(disabled))
		{
			File.Delete(disabled);
		}
		File.Copy(proxyDllPath, current, overwrite: true);
	}

	/// <summary>
	/// Turns the proxy off (the game's own DLL back under its name) or on again.
	/// </summary>
	public static void SetEnabled(string win64Dir, bool enabled)
	{
		var current = Path.Combine(win64Dir, ProxyName);
		var original = Path.Combine(win64Dir, OriginalName);
		var disabled = Path.Combine(win64Dir, DisabledName);

		if (enabled)
		{
			if (!File.Exists(disabled)) return;
			// The game's own DLL, possibly updated while the proxy was off.
			if (File.Exists(current) && !IsProxy(current))
			{
				File.Move(current, original, overwrite: true);
			}
			File.Move(disabled, current, overwrite: true);
		}
		else
		{
			if (!IsDeployed(win64Dir)) return;
			File.Move(current, disabled, overwrite: true);
			File.Move(original, current, overwrite: true);
		}
	}

	/// <summary>
	/// Turns off an older RTX Remix runtime (dxvk-remix's d3d9.dll), which would
	/// hook the game's rendering alongside OpenRemix.
	/// </summary>
	public static void DisableLegacyRemix(string win64Dir)
	{
		var d3d9 = Path.Combine(win64Dir, "d3d9.dll");
		if (File.Exists(d3d9))
		{
			File.Move(d3d9, d3d9 + ".disabled", overwrite: true);
		}
	}
}
