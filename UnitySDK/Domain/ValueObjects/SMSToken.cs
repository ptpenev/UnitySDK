namespace TextBuddySDK.Domain.ValueObjects
{
    /// <summary>
    /// Represents a secure token that is bound to a player's phone number.
    /// This token is used to authorize actions like sending an SMS on behalf of the player.
    /// It should be stored securely on the client device.
    /// </summary>
    public sealed class SMSToken
    {
        /// <summary>
        /// The actual token value. This is the string that will be sent with API requests.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The timestamp when this token expires. After this time, it's no longer valid.
        /// </summary>
        public DateTime Expiration { get; }

        /// <summary>
        /// Checks if the token is expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= Expiration;

        /// <summary>
        /// Private constructor to enforce creation through the factory method.
        /// </summary>
        /// <param name="value">The token string.</param>
        /// <param name="expiration">The expiration date of the token.</param>
        private SMSToken(string value, DateTime expiration)
        {
            Value = value;
            Expiration = expiration;
        }

        /// <summary>
        /// Factory method to create a new SMSToken instance.
        /// This provides a single point of validation.
        /// </summary>
        /// <param name="value">The token string received from the API.</param>
        /// <param name="expiration">The expiration date received from the API.</param>
        /// <returns>A new SMSToken instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the token value is null or empty.</exception>
        public static SMSToken Create(string value, DateTime expiration)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value), "SMS Token value cannot be null or empty.");
            }

            return new SMSToken(value, expiration);
        }

        /// <summary>
        /// Provides a string representation of the token's value.
        /// </summary>
        public override string ToString() => Value;
    }
}
