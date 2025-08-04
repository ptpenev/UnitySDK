using System.Collections.Generic;
using System.Threading.Tasks;

namespace TextBuddySDK.Domain.Analytics
{
    /// <summary>
    /// Defines the contract for an analytics service.
    /// This allows for different analytics providers to be plugged in.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Sends an analytics event.
        /// </summary>
        /// <param name="eventName">The name of the event (for example -> "RegistrationInitialized").</param>
        /// <param name="eventData">A dictionary of data associated with the event.</param>
        Task SendEventAsync(string eventName, Dictionary<string, object> eventData);
    }

    /// <summary>
    /// A default implementation of the analytics service.
    /// In the real SDK, this might send data to a specific analytics backend.
    /// For this example, it will just log to the console if debug logging is enabled.
    /// </summary>
    public sealed class DefaultAnalyticsService : IAnalyticsService
    {
        private readonly bool _enableDebugLogging;
        private readonly string _gameApiIdKey;

        public DefaultAnalyticsService(string gameApiIdKey, bool enableDebugLogging)
        {
            _gameApiIdKey = gameApiIdKey;
            _enableDebugLogging = enableDebugLogging;
        }

        public Task SendEventAsync(string eventName, Dictionary<string, object> eventData)
        {
            if (_enableDebugLogging)
            {
                // In the real implementation, this would be a non-blocking HTTP call to an analytics service.
                // The GameApiIdKey would be sent in a secure header, not as part of the public event data.
                System.Diagnostics.Debug.WriteLine($"[TextBuddy Analytics] Event: '{eventName}'");
                if (eventData != null)
                {
                    foreach (var item in eventData)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {item.Key}: {item.Value}");
                    }
                }
            }

            // Return a completed task as this is a "fire and forget" operation.
            return Task.CompletedTask;
        }
    }
}
