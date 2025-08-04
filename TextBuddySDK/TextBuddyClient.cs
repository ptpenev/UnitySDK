using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using TextBuddySDK.Configuration;
using TextBuddySDK.Domain.Analytics;
using TextBuddySDK.Domain.Results;
using TextBuddySDK.Domain.ValueObjects;
using TextBuddySDK.Infrastructure;

namespace TextBuddySDK
{
    /// <summary>
    /// The main entry point for interacting with the TextBuddy SDK.
    /// </summary>
    public sealed class TextBuddyClient
    {
        private readonly TextBuddyConfig _config;
        private readonly IApiClient _apiClient;
        private readonly IAnalyticsService _analyticsService;
        private readonly ISmsAppLauncher _smsAppLauncher;

        /// <summary>
        /// Initializes the TextBuddy client. This should be a singleton in your game.
        /// </summary>
        /// <param name="config">The configuration object containing API keys and URLs.</param>
        /// <param name="smsAppLauncher">An optional platform-specific SMS app launcher. If null, a default Unity launcher is used.</param>
        public TextBuddyClient(TextBuddyConfig config, ISmsAppLauncher smsAppLauncher = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _apiClient = new ApiClient(config.ApiBaseUrl, config.GameApiIdKey, config.EnableDebugLogging);
            _analyticsService = new DefaultAnalyticsService(config.GameApiIdKey, config.EnableDebugLogging);
            _smsAppLauncher = smsAppLauncher ?? new UnitySmsAppLauncher();
        }

        /// <summary>
        /// Public constructor for dependency injection during testing.
        /// </summary>
        public TextBuddyClient(TextBuddyConfig config, IApiClient apiClient, IAnalyticsService analyticsService, ISmsAppLauncher smsAppLauncher)
        {
            _config = config;
            _apiClient = apiClient;
            _analyticsService = analyticsService;
            _smsAppLauncher = smsAppLauncher;
        }

        /// <summary>
        /// Starts the player registration process by opening the SMS app.
        /// The player needs to send the pre-filled message to register their phone number.
        /// </summary>
        public void Register()
        {
            // The verification code should be fetched from the server
            // to ensure it's unique and securely associated with this registration attempt.
            // For this example, we generate a simple one on the client.
            string verificationCode = "tb_verify_" + System.Guid.NewGuid().ToString().Substring(0, 8);

            _smsAppLauncher.OpenSmsApp(_config.TextBuddyPhoneNumber, verificationCode);

            _analyticsService.SendEventAsync("RegistrationInitialized", new Dictionary<string, object>
            {
                { "verification_code_prefix", verificationCode.Split('_')[0] }
            });
        }

        /// <summary>
        /// Processes a deep link received by the application to finalize registration.
        /// The deep link should contain the verification code.
        /// </summary>
        /// <param name="deepLinkUrl">The full deep link URL.</param>
        /// <returns>A result object containing the SMSToken on success, or an error on failure.</returns>
        public async Task<SmsTokenResult> ProcessDeepLinkAsync(System.Uri deepLinkUrl)
        {
            // This parsing logic is highly dependent on the deep link structure.
            // Example structure: mygame://textbuddy-auth?code=ABCDEFG
            string verificationCode = ParseVerificationCodeFromDeepLink(deepLinkUrl);

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                return SmsTokenResult.Failure(new Error("InvalidDeepLink", "Verification code not found in deep link URL."));
            }

            return await _apiClient.GetSmsTokenAsync(verificationCode);
        }

        /// <summary>
        /// Sends an SMS to the player associated with the provided SMSToken.
        /// </summary>
        /// <param name="smsToken">The token received after successful registration.</param>
        /// <param name="smsBody">The content of the SMS message.</param>
        /// <returns>A result object indicating success or failure.</returns>
        public async Task<SendSmsResult> SendSmsAsync(SMSToken smsToken, string smsBody)
        {
            if (smsToken == null || smsToken.IsExpired)
            {
                return SendSmsResult.Failure(new Error("InvalidToken", "SMS Token is null or expired."));
            }
            if (string.IsNullOrWhiteSpace(smsBody))
            {
                return SendSmsResult.Failure(new Error("InvalidBody", "SMS body cannot be empty."));
            }

            return await _apiClient.SendSmsToTokenAsync(smsToken, smsBody);
        }

        /// <summary>
        /// Unregisters the player's phone number, invalidating the SMSToken.
        /// </summary>
        /// <param name="smsToken">The token to unregister.</param>
        /// <returns>A result object indicating success or failure.</returns>
        public async Task<UnregisterResult> UnregisterAsync(SMSToken smsToken)
        {
            if (smsToken == null)
            {
                return UnregisterResult.Failure(new Error("InvalidToken", "SMS Token cannot be null."));
            }
            return await _apiClient.UnregisterSmsTokenAsync(smsToken);
        }

        /// <summary>
        /// Checks if a stored SMSToken is still valid and registered with the TextBuddy service.
        /// </summary>
        /// <param name="smsToken">The token to check.</param>
        /// <returns>A result object containing 'true' or 'false' on success, or an error.</returns>
        public async Task<CheckTokenResult> IsTokenRegisteredAsync(SMSToken smsToken)
        {
            if (smsToken == null || smsToken.IsExpired)
            {
                return CheckTokenResult.Success(false);
            }
            return await _apiClient.CheckIfSmsTokenIsRegisteredAsync(smsToken);
        }

        /// <summary>
        /// A helper function to parse the verification code from a deep link.
        /// This should be adapted to the specific deep link format.
        /// </summary>
        private string ParseVerificationCodeFromDeepLink(Uri url)
        {
            // Example: mygame://auth?code=ABCDEFG
            // In Unity, you use Application.deepLinkActivated and parse the URL.
            try
            {
                // HttpUtility.ParseQueryString correctly handles all query string complexities.
                var queryParameters = HttpUtility.ParseQueryString(url.Query);

                // Access the 'code' parameter directly by its key.
                var code = queryParameters["code"];

                if (code == null)
                {
                    throw new ArgumentNullException(code, "Verification code was not found.");
                }

                return code;
            }
            catch
            {
                // Log error if needed, I will skip it for now.
                return null;
            }
        }
    }
}
