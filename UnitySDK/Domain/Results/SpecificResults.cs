using TextBuddySDK.Domain.ValueObjects;

namespace TextBuddySDK.Domain.Results
{
    // It's cleaner to have specific result types if they carry unique data,
    // but for this SDK, most results can be represented by the generic TextBuddyResult.
    // We will define a few for clarity and to match the original plan.

    /// <summary>
    /// Result for the operation that processes a deep link to get an SMSToken.
    /// Inherits from TextBuddyResult<SMSToken> to carry the token on success.
    /// </summary>
    public sealed class SmsTokenResult : TextBuddyResult<SMSToken>
    {
        private SmsTokenResult(SMSToken value, bool isSuccess, Error error)
            : base(value, isSuccess, error) { }

        public static SmsTokenResult Success(SMSToken token) => new SmsTokenResult(token, true, null);
        public new static SmsTokenResult Failure(Error error) => new SmsTokenResult(null, false, error);
    }

    /// <summary>
    /// Result for the SendSMS operation. A simple success/failure might be enough.
    /// </summary>
    public sealed class SendSmsResult : TextBuddyResult
    {
        private SendSmsResult(bool isSuccess, Error error)
            : base(isSuccess, error) { }

        public new static SendSmsResult Success() => new SendSmsResult(true, null);
        public new static SendSmsResult Failure(Error error) => new SendSmsResult(false, error);
    }

    /// <summary>
    /// Result for the Unregister operation.
    /// </summary>
    public sealed class UnregisterResult : TextBuddyResult
    {
        private UnregisterResult(bool isSuccess, Error error)
            : base(isSuccess, error) { }

        public new static UnregisterResult Success() => new UnregisterResult(true, null);
        public new static UnregisterResult Failure(Error error) => new UnregisterResult(false, error);
    }

    /// <summary>
    /// Result for checking if a token is registered. Carries a boolean value on success.
    /// </summary>
    public sealed class CheckTokenResult : TextBuddyResult<bool>
    {
        private CheckTokenResult(bool value, bool isSuccess, Error error)
            : base(value, isSuccess, error) { }

        public static CheckTokenResult Success(bool isRegistered) => new CheckTokenResult(isRegistered, true, null);
        public new static CheckTokenResult Failure(Error error) => new CheckTokenResult(false, false, error);
    }
}
