using TextBuddySDK.Infrastructure;
using UnityEngine; // You will need a reference to UnityEngine.dll

namespace TextBuddySDK.Tests.Infrastructure
{
    [TestFixture]
    public class InfrastructureSmsAppLauncherTests
    {
        [Test]
        public void GenerateSmsUrlForPlatform_WhenPlatformIsIPhonePlayer_UsesCorrectIosFormat()
        {
            // Arrange
            var phoneNumber = "12345";
            var messageBody = "Hello & Welcome";
            // The expected URL uses '&' as a separator and correctly escapes the message.
            var expectedUrl = "sms:12345&body=Hello%20%26%20Welcome";

            // Act
            // We can now directly test the logic by passing the platform we want to simulate.
            var actualUrl = UnitySmsAppLauncher.GenerateSmsUrlForPlatform(phoneNumber, messageBody, RuntimePlatform.IPhonePlayer);

            // Assert
            Assert.That(actualUrl, Is.EqualTo(expectedUrl));
        }

        [Test]
        public void GenerateSmsUrlForPlatform_WhenPlatformIsAndroid_UsesCorrectAndroidFormat()
        {
            // Arrange
            var phoneNumber = "54321";
            var messageBody = "Test message?";
            // The expected URL uses '?' as a separator and correctly escapes the message.
            var expectedUrl = "sms:54321?body=Test%20message%3F";

            // Act
            var actualUrl = UnitySmsAppLauncher.GenerateSmsUrlForPlatform(phoneNumber, messageBody, RuntimePlatform.Android);

            // Assert
            Assert.That(actualUrl, Is.EqualTo(expectedUrl));
        }

        [Test]
        public void GenerateSmsUrlForPlatform_WhenPlatformIsOther_UsesDefaultFormat()
        {
            // Arrange
            var phoneNumber = "999";
            var messageBody = "Default";
            var expectedUrl = "sms:999?body=Default";

            // Act
            // We test with another platform to ensure it falls into the 'else' block.
            var actualUrl = UnitySmsAppLauncher.GenerateSmsUrlForPlatform(phoneNumber, messageBody, RuntimePlatform.WindowsPlayer);

            // Assert
            Assert.That(actualUrl, Is.EqualTo(expectedUrl));
        }
    }
}
