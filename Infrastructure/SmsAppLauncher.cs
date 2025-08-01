namespace TextBuddySDK.Infrastructure
{
    /// <summary>
    /// Defines the contract for launching the native SMS application.
    /// This allows for platform-specific implementations (e.g., for iOS, Android).
    /// </summary>
    public interface ISmsAppLauncher
    {
        /// <summary>
        /// Opens the default SMS application with a pre-filled phone number and message body.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="messageBody">The pre-filled text message.</param>
        void OpenSmsApp(string phoneNumber, string messageBody);
    }

    /// <summary>
    /// A concrete implementation of ISmsAppLauncher for Unity.
    /// This uses the Application.OpenURL method which works on most mobile platforms.
    /// </summary>
    public sealed class UnitySmsAppLauncher : ISmsAppLauncher
    {
        public void OpenSmsApp(string phoneNumber, string messageBody)
        {
            // The sms: URI scheme is widely supported on mobile devices.
            // We need to handle platform-specific separators for the message body.
            // '?' is standard for Android, '&' for iOS. To be safe, we can check the platform.

            // Note: UnityEngine.Application is not available in a standard .NET library.
            // This code assumes it will be run within a Unity environment.
            // A common practice is to use #if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
            // to guard Unity-specific code.

#if UNITY_IOS
            string url = $"sms:{phoneNumber}&body={System.Uri.EscapeDataString(messageBody)}";
#else // Default to Android/other format
            string url = $"sms:{phoneNumber}?body={System.Uri.EscapeDataString(messageBody)}";
#endif

            UnityEngine.Application.OpenURL(url);
        }
    }
}
