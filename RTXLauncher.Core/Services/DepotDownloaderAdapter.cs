using DepotDownloader;
using RTXLauncher.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RTXLauncher.Core.Services;

public interface IDepotDownloaderAuthUi
{
    Task<string?> RequestTwoFactorCodeAsync(bool previousIncorrect);
    Task<string?> RequestEmailCodeAsync(string emailHint, bool previousIncorrect);
    Task<bool> RequestDeviceConfirmationAsync();
    Task ShowQrCodeAsync(string challengeUrl);
    void LogOutput(string message, bool isError);
}

public sealed class DepotDownloaderAdapter
{
    public async Task DownloadLegacyDepotAsync(
        DepotDownloadRequest request,
        IProgress<InstallProgressReport> progress,
        IDepotDownloaderAuthUi authUi)
    {
        if (string.IsNullOrWhiteSpace(request.ManifestId))
        {
            throw new ArgumentException("Manifest ID is required.", nameof(request));
        }

        if (!ulong.TryParse(request.ManifestId, out var manifestId))
        {
            throw new ArgumentException("Manifest ID must be numeric.", nameof(request));
        }

        Directory.CreateDirectory(request.OutputPath);

        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var outWriter = new ConsoleForwardingWriter(message => authUi.LogOutput(message, false));
        using var errWriter = new ConsoleForwardingWriter(message => authUi.LogOutput(message, true));

        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            if (AccountSettingsStore.Instance == null)
            {
                AccountSettingsStore.LoadFromFile("account.config");
            }

            Ansi.Init();

            AuthUiBridge.TwoFactorCodeAsync = authUi.RequestTwoFactorCodeAsync;
            AuthUiBridge.EmailCodeAsync = authUi.RequestEmailCodeAsync;
            AuthUiBridge.DeviceConfirmationAsync = authUi.RequestDeviceConfirmationAsync;
            AuthUiBridge.QrCodeAvailable = url => authUi.ShowQrCodeAsync(url).GetAwaiter().GetResult();

            ContentDownloader.Config = new DownloadConfig
            {
                InstallDirectory = request.OutputPath,
                RememberPassword = request.RememberPassword,
                UseQrCode = request.UseQrCode,
                SkipAppConfirmation = request.SkipAppConfirmation,
                MaxDownloads = request.MaxDownloads,
                DownloadAllPlatforms = false,
                DownloadAllArchs = false,
                DownloadAllLanguages = false,
                DownloadManifestOnly = false,
                VerifyAll = false,
                CellID = 0,
                LoginID = null,
            };

            var username = request.UseQrCode ? null : 
                           string.IsNullOrWhiteSpace(request.Username) ? null : request.Username;
            var password = request.UseQrCode ? null : 
                           string.IsNullOrWhiteSpace(request.Password) ? null : request.Password;

            // Validate credentials for non-QR login
            if (!request.UseQrCode && (username == null || password == null))
            {
                throw new ArgumentException("Username and password are required for standard Steam login. Use QR code option for passwordless login.");
            }

            progress.Report(new InstallProgressReport
            {
                Message = "Connecting to Steam...",
                Percentage = 20
            });

            if (!ContentDownloader.InitializeSteam3(username, password))
            {
                throw new Exception("Steam authentication failed. Check your credentials and try again.");
            }

            try
            {
                var depotManifestIds = new List<(uint depotId, ulong manifestId)>
                {
                    (request.DepotId, manifestId)
                };

                await ContentDownloader.DownloadAppAsync(
                    request.AppId,
                    depotManifestIds,
                    request.Branch,
                    request.OperatingSystem,
                    request.Architecture,
                    request.Language,
                    request.LowViolence,
                    isUgc: false
                ).ConfigureAwait(false);
            }
            finally
            {
                ContentDownloader.ShutdownSteam3();
            }

            progress.Report(new InstallProgressReport
            {
                Message = "Depot download completed.",
                Percentage = 90
            });
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);

            AuthUiBridge.TwoFactorCodeAsync = null;
            AuthUiBridge.EmailCodeAsync = null;
            AuthUiBridge.DeviceConfirmationAsync = null;
            AuthUiBridge.QrCodeAvailable = null;
        }
    }

    private sealed class ConsoleForwardingWriter : TextWriter
    {
        private readonly Action<string> _writeLine;
        private readonly StringWriter _buffer = new();

        public ConsoleForwardingWriter(Action<string> writeLine)
        {
            _writeLine = writeLine;
        }

        public override Encoding Encoding => _buffer.Encoding;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                Flush();
                return;
            }

            if (value != '\r')
            {
                _buffer.Write(value);
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            foreach (var ch in value)
            {
                Write(ch);
            }
        }

        public override void Flush()
        {
            var line = _buffer.ToString();
            _buffer.GetStringBuilder().Clear();

            if (!string.IsNullOrWhiteSpace(line))
            {
                _writeLine(line);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
                _buffer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
