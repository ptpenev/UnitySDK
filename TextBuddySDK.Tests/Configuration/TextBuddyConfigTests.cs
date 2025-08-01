using TextBuddySDK.Configuration;

namespace TextBuddySDK.Tests.Configuration
{
    [TestFixture]
    public class TextBuddyConfigTests
    {
        [Test]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            // Arrange
            var apiKey = "my-key";
            var url = "https://my-url.com";
            var phone = "+12345";
            var debug = true;

            // Act
            var config = new TextBuddyConfig(apiKey, url, phone, debug);

            // Assert
            Assert.That(config.GameApiIdKey, Is.EqualTo(apiKey));
            Assert.That(config.ApiBaseUrl, Is.EqualTo(url));
            Assert.That(config.TextBuddyPhoneNumber, Is.EqualTo(phone));
            Assert.That(config.EnableDebugLogging, Is.EqualTo(debug));
        }

        [Test]
        public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig(null, "url", "phone"));
        }

        [Test]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig("key", null, "phone"));
        }

        [Test]
        public void Constructor_WithNullPhone_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig("key", "url", null));
        }
    }
}