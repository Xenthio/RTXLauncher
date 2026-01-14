// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

#nullable enable

using System;
using System.Threading.Tasks;

namespace DepotDownloader
{
    internal static class AuthUiBridge
    {
        public static Func<bool, Task<string?>>? TwoFactorCodeAsync { get; set; }
        public static Func<string, bool, Task<string?>>? EmailCodeAsync { get; set; }
        public static Func<Task<bool>>? DeviceConfirmationAsync { get; set; }
        public static Action<string>? QrCodeAvailable { get; set; }

        public static string? RequestTwoFactorCode(bool previousIncorrect)
        {
            if (TwoFactorCodeAsync == null)
            {
                return null;
            }

            return TwoFactorCodeAsync(previousIncorrect).GetAwaiter().GetResult();
        }

        public static string? RequestEmailCode(string email, bool previousIncorrect)
        {
            if (EmailCodeAsync == null)
            {
                return null;
            }

            return EmailCodeAsync(email, previousIncorrect).GetAwaiter().GetResult();
        }

        public static bool RequestDeviceConfirmation()
        {
            if (DeviceConfirmationAsync == null)
            {
                return true;
            }

            return DeviceConfirmationAsync().GetAwaiter().GetResult();
        }

        public static void NotifyQrCode(string challengeUrl)
        {
            QrCodeAvailable?.Invoke(challengeUrl);
        }
    }
}
