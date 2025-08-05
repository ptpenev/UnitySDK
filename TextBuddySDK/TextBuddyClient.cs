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
    /// This version uses a callback-based pattern for asynchronous operations.
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
            // In a real project, you would switch this to the real ApiClient.
            // _apiClient = new ApiClient(config.ApiBaseUrl, config.GameApiIdKey, config.EnableDebugLogging);
            _apiClient = new FakeApiClient(config.ApiBaseUrl, config.GameApiIdKey, config.EnableDebugLogging);
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

        #region Public API (Callback-based)

        /// <summary>
        /// Starts the player registration process by opening the SMS app.
        /// This is a synchronous operation as it only launches an external app.
        /// </summary>
        public void Register()
        {
            // The verification code should ideally be fetched from a server endpoint first
            // to ensure it's unique and securely associated with this registration attempt.
            string verificationCode = "tb_verify_" + Guid.NewGuid().ToString().Substring(0, 8);

            _smsAppLauncher.OpenSmsApp(_config.TextBuddyPhoneNumber, verificationCode);

            // This can be fire-and-forget
            _ = _analyticsService.SendEventAsync("RegistrationInitialized", new Dictionary<string, object>
            {
                { "verification_code_prefix", verificationCode.Split('_')[0] }
            });
        }

        /// <summary>
        /// Processes a deep link to finalize registration and get an SMSToken.
        /// </summary>
        /// <param name="deepLinkUrl">The full deep link URL received by the app.</param>
        /// <param name="onComplete">Callback invoked with the result, containing the SMSToken on success or an error on failure.</param>
        public void ProcessDeepLink(Uri deepLinkUrl, Action<SmsTokenResult> onComplete)
        {
            // This is a "fire and forget" call from the public API's perspective.
            // The result is handled entirely within the async helper and delivered via the callback.
            _ = ProcessDeepLinkInternalAsync(deepLinkUrl, onComplete);
        }

        /// <summary>
        /// Sends an SMS to the player associated with the provided SMSToken.
        /// </summary>
        /// <param name="smsToken">The token received after successful registration.</param>
        /// <param name="smsBody">The content of the SMS message.</param>
        /// <param name="onComplete">Callback invoked with the result, indicating success or failure.</param>
        public void SendSms(SMSToken smsToken, string smsBody, Action<SendSmsResult> onComplete)
        {
            _ = SendSmsInternalAsync(smsToken, smsBody, onComplete);
        }

        /// <summary>
        /// Unregisters the player's phone number, invalidating the SMSToken.
        /// </summary>
        /// <param name="smsToken">The token to unregister.</param>
        /// <param name="onComplete">Callback invoked with the result, indicating success or failure.</param>
        public void Unregister(SMSToken smsToken, Action<UnregisterResult> onComplete)
        {
            _ = UnregisterInternalAsync(smsToken, onComplete);
        }

        /// <summary>
        /// Checks if a stored SMSToken is still valid and registered.
        /// </summary>
        /// <param name="smsToken">The token to check.</param>
        /// <param name="onComplete">Callback invoked with the result, containing 'true' or 'false' on success, or an error.</param>
        public void IsTokenRegistered(SMSToken smsToken, Action<CheckTokenResult> onComplete)
        {
            _ = IsTokenRegisteredInternalAsync(smsToken, onComplete);
        }

        #endregion

        #region Internal Async Implementation

        /// <summary>
        /// Internal async helper for processing a deep link.
        /// </summary>
        private async Task ProcessDeepLinkInternalAsync(Uri deepLinkUrl, Action<SmsTokenResult> onComplete)
        {
            try
            {
                string verificationCode = ParseVerificationCodeFromDeepLink(deepLinkUrl);

                if (string.IsNullOrWhiteSpace(verificationCode))
                {
                    onComplete?.Invoke(SmsTokenResult.Failure(new Error("InvalidDeepLink", "Verification code not found in deep link URL.")));
                    return;
                }

                var result = await _apiClient.GetSmsTokenAsync(verificationCode);
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                var errorResult = SmsTokenResult.Failure(new Error("SdkException", $"An unexpected error occurred: {e.Message}"));
                onComplete?.Invoke(errorResult);
            }
        }

        /// <summary>
        /// Internal async helper for sending an SMS.
        /// </summary>
        private async Task SendSmsInternalAsync(SMSToken smsToken, string smsBody, Action<SendSmsResult> onComplete)
        {
            try
            {
                if (smsToken == null || smsToken.IsExpired)
                {
                    onComplete?.Invoke(SendSmsResult.Failure(new Error("InvalidToken", "SMS Token is null or expired.")));
                    return;
                }
                if (string.IsNullOrWhiteSpace(smsBody))
                {
                    onComplete?.Invoke(SendSmsResult.Failure(new Error("InvalidBody", "SMS body cannot be empty.")));
                    return;
                }

                var result = await _apiClient.SendSmsToTokenAsync(smsToken, smsBody);
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                var errorResult = SendSmsResult.Failure(new Error("SdkException", $"An unexpected error occurred: {e.Message}"));
                onComplete?.Invoke(errorResult);
            }
        }

        /// <summary>
        /// Internal async helper for unregistering a token.
        /// </summary>
        private async Task UnregisterInternalAsync(SMSToken smsToken, Action<UnregisterResult> onComplete)
        {
            try
            {
                if (smsToken == null)
                {
                    onComplete?.Invoke(UnregisterResult.Failure(new Error("InvalidToken", "SMS Token cannot be null.")));
                    return;
                }
                var result = await _apiClient.UnregisterSmsTokenAsync(smsToken);
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                var errorResult = UnregisterResult.Failure(new Error("SdkException", $"An unexpected error occurred: {e.Message}"));
                onComplete?.Invoke(errorResult);
            }
        }

        /// <summary>
        /// Internal async helper for checking a token's registration status.
        /// </summary>
        private async Task IsTokenRegisteredInternalAsync(SMSToken smsToken, Action<CheckTokenResult> onComplete)
        {
            try
            {
                if (smsToken == null || smsToken.IsExpired)
                {
                    onComplete?.Invoke(CheckTokenResult.Success(false));
                    return;
                }
                var result = await _apiClient.CheckIfSmsTokenIsRegisteredAsync(smsToken);
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                var errorResult = CheckTokenResult.Failure(new Error("SdkException", $"An unexpected error occurred: {e.Message}"));
                onComplete?.Invoke(errorResult);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// A helper function to parse the verification code from a deep link.
        /// </summary>
        private string ParseVerificationCodeFromDeepLink(Uri url)
        {
            // Example deep link: mygame://auth?code=ABCDEFG
            try
            {
                if (url == null)
                {
                    throw new ArgumentNullException(nameof(url), "Deep link URL cannot be null.");
                }

                // HttpUtility.ParseQueryString correctly handles all query string complexities.
                var queryParameters = HttpUtility.ParseQueryString(url.Query);
                var code = queryParameters["code"];

                if (string.IsNullOrWhiteSpace(code))
                {
                    throw new ArgumentException("Verification code was not found in the 'code' query parameter.", nameof(url));
                }

                return code;
            }
            catch (Exception ex)
            {
                // Using System.Diagnostics.Debug for library-safe logging.
                // In Unity, this will print to the Unity Console.
                System.Diagnostics.Debug.WriteLine($"[TextBuddySDK] Error parsing deep link: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
