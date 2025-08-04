namespace TextBuddySDK.Configuration
{
    /// <summary>
    /// Holds the configuration settings for the TextBuddy SDK.
    /// An instance of this class is required to initialize the main TextBuddy client.
    /// </summary>
    public sealed class TextBuddyConfig     // Not inheritable
    {
        /// <summary>
        /// The API key for the game, provided by the TextBuddy service.
        /// This key is used to authenticate requests to the TextBuddy API.
        /// It should be kept secret and only used in server-to-server or secure client-to-server communication.
        /// </summary>
        public string GameApiIdKey { get; }

        /// <summary>
        /// The base URL for the TextBuddy API.
        /// Example: "https://api.textbuddy.com/v1"
        /// </summary>
        public string ApiBaseUrl { get; }

        /// <summary>
        /// The phone number that players will send the verification SMS to.
        /// This should be in international format, like this one -> "+35951234567".
        /// </summary>
        public string TextBuddyPhoneNumber { get; }

        /// <summary>
        /// Optional flag to enable verbose logging for debugging purposes.
        /// Defaults to false.
        /// </summary>
        public bool EnableDebugLogging { get; }

        /// <summary>
        /// Initializes a new instance of the TextBuddyConfig class.
        /// </summary>
        /// <param name="gameApiIdKey">The unique API key for the game.</param>
        /// <param name="apiBaseUrl">The base URL of the TextBuddy API.</param>
        /// <param name="textBuddyPhoneNumber">The destination phone number for verification.</param>
        /// <param name="enableDebugLogging">Whether to enable verbose logging. Optional.</param>
        public TextBuddyConfig(string gameApiIdKey, string apiBaseUrl, string textBuddyPhoneNumber, bool enableDebugLogging = false)
        {
            if (string.IsNullOrWhiteSpace(gameApiIdKey))
                throw new System.ArgumentNullException(nameof(gameApiIdKey), "Game API Key cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                throw new System.ArgumentNullException(nameof(apiBaseUrl), "API Base URL cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(textBuddyPhoneNumber))
                throw new System.ArgumentNullException(nameof(textBuddyPhoneNumber), "TextBuddy Phone Number cannot be null or empty.");

            GameApiIdKey = gameApiIdKey;
            ApiBaseUrl = apiBaseUrl;
            TextBuddyPhoneNumber = textBuddyPhoneNumber;
            EnableDebugLogging = enableDebugLogging;
        }
    }
}
