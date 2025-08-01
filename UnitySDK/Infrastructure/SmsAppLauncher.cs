// TextBuddySDK/Infrastructure/SmsAppLauncher.cs

using UnityEngine;

namespace TextBuddySDK.Infrastructure
{
    /// <summary>
    /// Defines the contract for launching the native SMS application.
    /// </summary>
    public interface ISmsAppLauncher
    {
        void OpenSmsApp(string phoneNumber, string messageBody);
    }

    /// <summary>
    /// This is the new "Wrapper" interface. It defines a contract for the
    /// specific Unity Application methods we need to call.
    /// This interface is the key to making the code testable.
    /// </summary>
    public interface IApplicationWrapper
    {
        RuntimePlatform Platform { get; }
        void OpenURL(string url);
    }

    /// <summary>
    /// The real implementation of the wrapper that calls the actual Unity engine.
    /// This class will be used in the real game.
    /// </summary>
    public class UnityApplicationWrapper : IApplicationWrapper
    {
        public RuntimePlatform Platform => Application.platform;
        public void OpenURL(string url) => Application.OpenURL(url);
    }

    /// <summary>
    /// The main implementation of ISmsAppLauncher for Unity.
    /// </summary>
    public sealed class UnitySmsAppLauncher : ISmsAppLauncher
    {
        private readonly IApplicationWrapper _application;

        /// <summary>
        /// The public constructor used in production. It creates the real Unity wrapper.
        /// </summary>
        public UnitySmsAppLauncher() : this(new UnityApplicationWrapper())
        {
        }

        /// <summary>
        /// An internal constructor for testing, allowing us to inject a mock wrapper.
        /// </summary>
        internal UnitySmsAppLauncher(IApplicationWrapper applicationWrapper)
        {
            _application = applicationWrapper;
        }

        public void OpenSmsApp(string phoneNumber, string messageBody)
        {
            // Now we call our wrapper instead of the static Application class.
            string url = GenerateSmsUrlForPlatform(phoneNumber, messageBody, _application.Platform);
            _application.OpenURL(url);
        }

        public static string GenerateSmsUrlForPlatform(string phoneNumber, string messageBody, RuntimePlatform platform)
        {
            var escapedBody = System.Uri.EscapeDataString(messageBody);
            if (platform == RuntimePlatform.IPhonePlayer)
            {
                return $"sms:{phoneNumber}&body={escapedBody}";
            }
            else
            {
                return $"sms:{phoneNumber}?body={escapedBody}";
            }
        }
    }
}
