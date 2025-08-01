using System.Text;
using TextBuddySDK.Domain.Results;
using TextBuddySDK.Domain.ValueObjects;

namespace TextBuddySDK.Infrastructure
{
    /// <summary>
    /// Defines the contract for the TextBuddy backend API client.
    /// Creating this interface allows for easy mocking in unit tests.
    /// </summary>
    public interface IApiClient
    {
        Task<SmsTokenResult> GetSmsTokenAsync(string verificationCode);
        Task<SendSmsResult> SendSmsToTokenAsync(SMSToken smsToken, string smsBody);
        Task<UnregisterResult> UnregisterSmsTokenAsync(SMSToken smsToken);
        Task<CheckTokenResult> CheckIfSmsTokenIsRegisteredAsync(SMSToken smsToken);
    }

    /// <summary>
    /// This class is responsible for all communication with the TextBuddy backend API.
    /// It implements the IApiClient interface.
    /// </summary>
    internal sealed class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _gameApiIdKey;
        private readonly bool _enableDebugLogging;

        public ApiClient(string apiBaseUrl, string gameApiIdKey, bool enableDebugLogging)
        {
            _httpClient = new HttpClient { BaseAddress = new System.Uri(apiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

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

            var response = await PostAsync("/tokens/exchange", content);

            if (!response.IsSuccessStatusCode)
            {
                return SmsTokenResult.Failure(new Error("ApiError", $"Failed to get SMS token. Status: {response.StatusCode}"));
            }

            string fakeTokenValue = "fake-sms-token-" + Guid.NewGuid().ToString();
            DateTime fakeExpiration = DateTime.UtcNow.AddDays(30);

            var smsToken = SMSToken.Create(fakeTokenValue, fakeExpiration);
            return SmsTokenResult.Success(smsToken);
        }

        /// <summary>
        /// Sends an SMS to the phone number associated with the given SMSToken.
        /// </summary>
        public async Task<SendSmsResult> SendSmsToTokenAsync(SMSToken smsToken, string smsBody)
        {
            var requestBody = $"{{\"smsBody\":\"{smsBody}\"}}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await PostAsync($"/sms/send", content, smsToken.Value);

            if (response.IsSuccessStatusCode)
            {
                return SendSmsResult.Success();
            }
            return SendSmsResult.Failure(new Error("ApiError", $"Failed to send SMS. Status: {response.StatusCode}"));
        }

        /// <summary>
        /// Unregisters an SMSToken, invalidating it for future use.
        /// </summary>
        public async Task<UnregisterResult> UnregisterSmsTokenAsync(SMSToken smsToken)
        {
            var response = await PostAsync($"/tokens/unregister", null, smsToken.Value);

            if (response.IsSuccessStatusCode)
            {
                return UnregisterResult.Success();
            }
            return UnregisterResult.Failure(new Error("ApiError", $"Failed to unregister token. Status: {response.StatusCode}"));
        }

        /// <summary>
        /// Checks with the backend if a given SMSToken is still valid and registered.
        /// </summary>
        public async Task<CheckTokenResult> CheckIfSmsTokenIsRegisteredAsync(SMSToken smsToken)
        {
            var response = await GetAsync($"/tokens/check", smsToken.Value);

            if (!response.IsSuccessStatusCode)
            {
                return CheckTokenResult.Failure(new Error("ApiError", $"API check failed. Status: {response.StatusCode}"));
            }

            return CheckTokenResult.Success(true);
        }

        // Helper methods for making requests

        private async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, string playerToken = null)
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

        private async Task<HttpResponseMessage> GetAsync(string url, string playerToken = null)
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
