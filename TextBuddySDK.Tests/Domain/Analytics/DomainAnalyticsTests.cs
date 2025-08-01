using TextBuddySDK.Domain.Analytics;

namespace TextBuddySDK.Tests.Domain.Analytics
{
    [TestFixture]
    public class DomainAnalyticsTests
    {
        [Test]
        public void SendEventAsync_WhenLoggingIsDisabled_CompletesWithoutError()
        {
            // Arrange
            // Create the service with logging disabled.
            var analyticsService = new DefaultAnalyticsService("api-key", false);

            // Act & Assert
            // This test passes if the method runs to completion without throwing an exception.
            Assert.DoesNotThrowAsync(async () => await analyticsService.SendEventAsync("TestEvent", null));
        }

        [Test]
        public void SendEventAsync_WhenLoggingIsEnabledWithNullData_CompletesWithoutError()
        {
            // Arrange
            // Create the service with logging enabled.
            var analyticsService = new DefaultAnalyticsService("api-key", true);

            // Act & Assert
            // This test passes if the method runs to completion without throwing an exception.
            Assert.DoesNotThrowAsync(async () => await analyticsService.SendEventAsync("PlayerJumped", null));
        }

        [Test]
        public void SendEventAsync_WhenLoggingIsEnabledWithData_CompletesWithoutError()
        {
            // Arrange
            var analyticsService = new DefaultAnalyticsService("api-key", true);
            var eventData = new Dictionary<string, object>
            {
                { "Height", 10.5 },
                { "PlayerName", "Steve" }
            };

            // Act & Assert
            // This test passes if the method runs to completion without throwing an exception.
            Assert.DoesNotThrowAsync(async () => await analyticsService.SendEventAsync("PlayerAction", eventData));
        }
    }
}
