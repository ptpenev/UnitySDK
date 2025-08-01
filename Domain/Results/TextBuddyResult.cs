namespace TextBuddySDK.Domain.Results
{
    /// <summary>
    /// A generic result class for TextBuddy operations.
    /// It indicates whether an operation was successful and can hold an error.
    /// </summary>
    public class TextBuddyResult
    {
        /// <summary>
        /// Indicates if the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Indicates if the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Contains details about the error if the operation failed.
        /// Will be null on success.
        /// </summary>
        public Error Error { get; }

        protected TextBuddyResult(bool isSuccess, Error error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a success result.
        /// </summary>
        public static TextBuddyResult Success() => new TextBuddyResult(true, null);

        /// <summary>
        /// Creates a failure result with a specific error.
        /// </summary>
        public static TextBuddyResult Failure(Error error) => new TextBuddyResult(false, error);
    }

    /// <summary>
    /// A generic result class that carries a value on success.
    /// </summary>
    /// <typeparam name="TValue">The type of the value returned on success.</typeparam>
    public class TextBuddyResult<TValue> : TextBuddyResult
    {
        private readonly TValue _value;

        /// <summary>
        /// The value returned by the successful operation.
        /// Accessing this on a failed result will throw an exception.
        /// </summary>
        public TValue Value => IsSuccess
            ? _value
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

        protected TextBuddyResult(TValue value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a success result with a value.
        /// </summary>
        public static TextBuddyResult<TValue> Success(TValue value) => new TextBuddyResult<TValue>(value, true, null);

        /// <summary>
        /// Creates a failure result with an error.
        /// </summary>
        public new static TextBuddyResult<TValue> Failure(Error error) => new TextBuddyResult<TValue>(default, false, error);
    }

    /// <summary>
    /// Represents an error with a code and a descriptive message.
    /// </summary>
    public sealed class Error
    {
        /// <summary>
        /// A code representing the error type (e.g., "NetworkError", "InvalidToken").
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// A descriptive message explaining the error.
        /// </summary>
        public string Message { get; }

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
