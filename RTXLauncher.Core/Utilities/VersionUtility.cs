using System.Reflection;

namespace RTXLauncher.Core.Utilities;

public static class VersionUtility
{
	public static string GetCurrentAssemblyVersion()
	{
		var infoAttr = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
		if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
		{
			return $"dev-{infoAttr.InformationalVersion}";
		}

		var assemblyName = Assembly.GetEntryAssembly()?.GetName();
		var version = assemblyName?.Version ?? new Version(0, 0);
		return $"v{version.Major}.{version.Minor}.{version.Build}";
	}

	public static int CompareVersions(string version1, string version2)
	{
		version1 = version1.TrimStart('v');
		version2 = version2.TrimStart('v');

		if (Version.TryParse(version1, out var v1) && Version.TryParse(version2, out var v2))
		{
			return v1.CompareTo(v2);
		}

		bool isDev1 = version1.StartsWith("dev-");
		bool isDev2 = version2.StartsWith("dev-");

		if (isDev1 && !isDev2) return 1;
		if (!isDev1 && isDev2) return -1;

		// Other comparisons can be added here if needed
		return string.Compare(version1, version2, StringComparison.Ordinal);
	}
}