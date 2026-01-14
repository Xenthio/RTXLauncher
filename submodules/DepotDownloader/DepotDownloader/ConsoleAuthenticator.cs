// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace DepotDownloader
{
    // This is practically copied from https://github.com/SteamRE/SteamKit/blob/master/SteamKit2/SteamKit2/Steam/Authentication/UserConsoleAuthenticator.cs
    internal class ConsoleAuthenticator : IAuthenticator
    {
        /// <inheritdoc />
        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            var code = AuthUiBridge.RequestTwoFactorCode(previousCodeWasIncorrect);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(code!);
            }

            if (previousCodeWasIncorrect)
            {
                Console.Error.WriteLine("The previous 2-factor auth code you have provided is incorrect.");
            }

            string fallback;

            do
            {
                Console.Error.Write("STEAM GUARD! Please enter your 2-factor auth code from your authenticator app: ");
                fallback = Console.ReadLine()?.Trim();

                if (fallback == null)
                {
                    break;
                }
            }
            while (string.IsNullOrEmpty(fallback));

            return Task.FromResult(fallback!);
        }

        /// <inheritdoc />
        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            var code = AuthUiBridge.RequestEmailCode(email, previousCodeWasIncorrect);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(code!);
            }

            if (previousCodeWasIncorrect)
            {
                Console.Error.WriteLine("The previous 2-factor auth code you have provided is incorrect.");
            }

            string fallback;

            do
            {
                Console.Error.Write($"STEAM GUARD! Please enter the auth code sent to the email at {email}: ");
                fallback = Console.ReadLine()?.Trim();

                if (fallback == null)
                {
                    break;
                }
            }
            while (string.IsNullOrEmpty(fallback));

            return Task.FromResult(fallback!);
        }

        /// <inheritdoc />
        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            if (ContentDownloader.Config.SkipAppConfirmation)
            {
                return Task.FromResult(false);
            }

            var shouldWait = AuthUiBridge.RequestDeviceConfirmation();
            if (shouldWait)
            {
                Console.Error.WriteLine("STEAM GUARD! Use the Steam Mobile App to confirm your sign in...");
            }

            return Task.FromResult(shouldWait);
        }
    }
}
