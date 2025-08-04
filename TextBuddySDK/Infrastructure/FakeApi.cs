using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TextBuddySDK.Domain.Results;
using TextBuddySDK.Domain.ValueObjects;

namespace TextBuddySDK.Infrastructure
{
    /// <summary>
    /// This class is responsible for all communication with the TextBuddy backend API.
    /// It implements the IApiClient interface.
    /// </summary>

    public sealed class FakeApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _gameApiIdKey;
        private readonly bool _enableDebugLogging;

        /// <summary>
        /// Public constructor for production use.
        /// </summary>
        public FakeApiClient(string apiBaseUrl, string gameApiIdKey, bool enableDebugLogging)
            : this(new HttpClient { BaseAddress = new System.Uri(apiBaseUrl) }, gameApiIdKey, enableDebugLogging)
        {
            // This constructor calls the internal one below, setting up a real HttpClient.
            // We can add default headers here that apply to all requests.
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Public constructor for testing, allowing injection of a mocked HttpClient.
        /// </summary>
        public FakeApiClient(HttpClient httpClient, string gameApiIdKey, bool enableDebugLogging)
        {
            _httpClient = httpClient;
            _gameApiIdKey = gameApiIdKey;
            _enableDebugLogging = enableDebugLogging;
        }

        /// <summary>
        /// Exchanges a verification code (from a deep link) for a secure SMSToken.
        /// </summary>
        public async Task<SmsTokenResult> GetSmsTokenAsync(string verificationCode)
        {
            var requestBody = $"{{\"verificationCode\":\"{verificationCode}\"}}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            string fakeTokenValue = "fake-sms-token-" + System.Guid.NewGuid().ToString();
            System.DateTime fakeExpiration = System.DateTime.UtcNow.AddDays(30);

            var smsToken = SMSToken.Create(fakeTokenValue, fakeExpiration);
            return await Task.FromResult(SmsTokenResult.Success(smsToken));
        }

        /// <summary>
        /// Sends an SMS to the phone number associated with the given SMSToken.
        /// </summary>
        public async Task<SendSmsResult> SendSmsToTokenAsync(SMSToken smsToken, string smsBody)
        {
            var requestBody = $"{{\"smsBody\":\"{smsBody}\"}}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            if (smsToken.IsExpired)
            {
                return await Task.FromResult(SendSmsResult.Failure(new Error("ApiError", $"Failed to send SMS.")));
            }
            return await Task.FromResult(SendSmsResult.Success());
        }

        /// <summary>
        /// Unregisters an SMSToken, invalidating it for future use.
        /// </summary>
        public async Task<UnregisterResult> UnregisterSmsTokenAsync(SMSToken smsToken)
        {
            if (smsToken.IsExpired)
            {
                return await Task.FromResult(UnregisterResult.Failure(new Error("ApiError", $"Failed to unregister token.")));
            }
            return await Task.FromResult(UnregisterResult.Success());
        }

        /// <summary>
        /// Checks with the backend if a given SMSToken is still valid and registered.
        /// </summary>
        public async Task<CheckTokenResult> CheckIfSmsTokenIsRegisteredAsync(SMSToken smsToken)
        {
            if (smsToken.IsExpired)
            {
                return await Task.FromResult(CheckTokenResult.Failure(new Error("ApiError", $"API check failed")));
            }

            return await Task.FromResult(CheckTokenResult.Success(true));
        }

        // Helper methods
        internal async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, string playerToken = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _gameApiIdKey);

                if (playerToken != null)
                {
                    requestMessage.Headers.Add("X-Sms-Token", playerToken);
                }

                requestMessage.Content = content;
                LogRequest(requestMessage);
                return await _httpClient.SendAsync(requestMessage);
            }
        }

        internal async Task<HttpResponseMessage> GetAsync(string url, string playerToken = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _gameApiIdKey);
                if (playerToken != null)
                {
                    requestMessage.Headers.Add("X-Sms-Token", playerToken);
                }
                LogRequest(requestMessage);
                return await _httpClient.SendAsync(requestMessage);
            }
        }

        private void LogRequest(HttpRequestMessage request)
        {
            if (!_enableDebugLogging) return;
            System.Diagnostics.Debug.WriteLine($"[TextBuddy API] Request: {request.Method} {request.RequestUri}");
        }
    }
}
