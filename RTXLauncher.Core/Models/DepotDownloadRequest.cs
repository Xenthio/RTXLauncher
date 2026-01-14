namespace RTXLauncher.Core.Models;

public sealed class DepotDownloadRequest
{
    public uint AppId { get; init; }
    public uint DepotId { get; init; }
    public string ManifestId { get; init; } = string.Empty;
    public string Branch { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseQrCode { get; init; }
    public bool RememberPassword { get; init; }
    public bool SkipAppConfirmation { get; init; }
    public int MaxDownloads { get; init; } = 8;
    public string? OperatingSystem { get; init; }
    public string? Architecture { get; init; }
    public string? Language { get; init; }
    public bool LowViolence { get; init; }
}
