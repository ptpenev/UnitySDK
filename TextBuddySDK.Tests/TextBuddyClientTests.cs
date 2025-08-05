using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextBuddySDK.Configuration;
using TextBuddySDK.Domain.Analytics;
using TextBuddySDK.Domain.Results;
using TextBuddySDK.Domain.ValueObjects;
using TextBuddySDK.Infrastructure;

namespace TextBuddySDK.Tests
{
    [TestFixture]
    public class TextBuddyClientTests
    {
        #region Initialization
        // Mocks for all dependencies
        private Mock<IApiClient> _mockApiClient;
        private Mock<IAnalyticsService> _mockAnalyticsService;
        private Mock<ISmsAppLauncher> _mockSmsAppLauncher;

        // The class we are testing
        private TextBuddyClient _textBuddyClient;
        private TextBuddyConfig _config;

        [SetUp]
        public void Setup()
        {
            // This method runs before each test

            // 1. Create the configuration
            _config = new TextBuddyConfig(
                gameApiIdKey: "test_api_key",
                apiBaseUrl: "https://fake-api.textbuddy.com",
                textBuddyPhoneNumber: "+35950001111"
            );

            // 2. Create mocks of the dependency interfaces.
            _mockApiClient = new Mock<IApiClient>();
            _mockAnalyticsService = new Mock<IAnalyticsService>();
            _mockSmsAppLauncher = new Mock<ISmsAppLauncher>();

            // 3. Create the instance of the class under test, injecting the mocks
            _textBuddyClient = new TextBuddyClient(
                _config,
                _mockApiClient.Object,
                _mockAnalyticsService.Object,
                _mockSmsAppLauncher.Object
            );
        }
        #endregion

        #region Register Tests

        [Test]
        public void Register_WhenCalled_OpensSmsAppAndSendsAnalytics()
        {
            // Arrange (Setup is already done)

            // Act
            _textBuddyClient.Register();

            // Assert
            // Verify that the SMS app was opened with the correct phone number and a valid verification code.
            _mockSmsAppLauncher.Verify(launcher => launcher.OpenSmsApp(
                _config.TextBuddyPhoneNumber,
                It.Is<string>(s => s.StartsWith("tb_verify_"))),
                Times.Once);

            // Verify that the analytics event for registration initialization was sent.
            _mockAnalyticsService.Verify(analytics => analytics.SendEventAsync(
                "RegistrationInitialized",
                It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        #endregion

        #region ProcessDeepLink Tests

        [Test]
        public void ProcessDeepLink_WithValidCode_InvokesCallbackWithSuccessResult()
        {
            // Arrange
            var verificationCode = "good_code";
            var deepLink = new Uri($"mygame://auth?code={verificationCode}");
            var expectedToken = SMSToken.Create("new-sms-token", DateTime.UtcNow.AddDays(1));
            var successResult = SmsTokenResult.Success(expectedToken);

            _mockApiClient
                .Setup(api => api.GetSmsTokenAsync(verificationCode))
                .ReturnsAsync(successResult);

            // Act & Assert
            _textBuddyClient.ProcessDeepLink(deepLink, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.Value, Is.EqualTo(expectedToken.Value));
            });
        }

        [Test]
        public void ProcessDeepLink_WithNoCode_InvokesCallbackWithFailureResult()
        {
            // Arrange
            var deepLink = new Uri("mygame://auth?otherparam=123");

            // Act & Assert
            _textBuddyClient.ProcessDeepLink(deepLink, (result) =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo("InvalidDeepLink"));
                Assert.That(result.Error.Message, Is.EqualTo("Verification code not found in deep link URL."));
            });

            // Verify the API was never called because the client failed fast.
            _mockApiClient.Verify(api => api.GetSmsTokenAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region SendSms Tests

        [Test]
        public void SendSms_WithValidTokenAndBody_InvokesCallbackWithSuccess()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            var message = "Hello World";
            _mockApiClient
                .Setup(api => api.SendSmsToTokenAsync(validToken, message))
                .ReturnsAsync(SendSmsResult.Success());

            // Act & Assert
            _textBuddyClient.SendSms(validToken, message, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
            });
        }

        [Test]
        public void SendSms_WithEmptyBody_InvokesCallbackWithFailureResult()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));

            // Act & Assert
            _textBuddyClient.SendSms(validToken, "", (result) =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo("InvalidBody"));
                Assert.That(result.Error.Message, Is.EqualTo("SMS body cannot be empty."));
            });
        }

        [Test]
        public void SendSms_WithExpiredToken_InvokesCallbackWithFailureResult()
        {
            // Arrange
            var expiredToken = SMSToken.Create("some-token", DateTime.UtcNow.AddMinutes(-5));
            var message = "Hello World";

            // Act & Assert
            _textBuddyClient.SendSms(expiredToken, message, (result) =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo("InvalidToken"));
                Assert.That(result.Error.Message, Is.EqualTo("SMS Token is null or expired."));
            });
        }

        #endregion

        #region Unregister Tests

        [Test]
        public void Unregister_WithValidToken_InvokesCallbackWithSuccess()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            _mockApiClient
                .Setup(api => api.UnregisterSmsTokenAsync(validToken))
                .ReturnsAsync(UnregisterResult.Success());

            // Act & Assert
            _textBuddyClient.Unregister(validToken, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
            });
        }

        [Test]
        public void Unregister_WithNullToken_InvokesCallbackWithFailure()
        {
            // Act & Assert
            _textBuddyClient.Unregister(null, (result) =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo("InvalidToken"));
                Assert.That(result.Error.Message, Is.EqualTo("SMS Token cannot be null."));
            });
        }

        [Test]
        public void Unregister_WhenApiFails_InvokesCallbackWithFailure()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            var apiError = new Error("ApiError", "Failed to connect");
            _mockApiClient
                .Setup(api => api.UnregisterSmsTokenAsync(validToken))
                .ReturnsAsync(UnregisterResult.Failure(apiError));

            // Act & Assert
            _textBuddyClient.Unregister(validToken, (result) =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error, Is.EqualTo(apiError));
            });
        }

        #endregion

        #region IsTokenRegistered Tests

        [Test]
        public void IsTokenRegistered_WithValidAndRegisteredToken_InvokesCallbackWithTrue()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            _mockApiClient
                .Setup(api => api.CheckIfSmsTokenIsRegisteredAsync(validToken))
                .ReturnsAsync(CheckTokenResult.Success(true));

            // Act & Assert
            _textBuddyClient.IsTokenRegistered(validToken, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.True);
            });
        }

        [Test]
        public void IsTokenRegistered_WithExpiredToken_InvokesCallbackWithFalse()
        {
            // Arrange
            var expiredToken = SMSToken.Create("expired-token", DateTime.UtcNow.AddMinutes(-1));

            // Act & Assert
            _textBuddyClient.IsTokenRegistered(expiredToken, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.False);
            });

            // Verify we didn't even call the API since we can fail fast.
            _mockApiClient.Verify(api => api.CheckIfSmsTokenIsRegisteredAsync(It.IsAny<SMSToken>()), Times.Never);
        }

        [Test]
        public void IsTokenRegistered_WithNullToken_InvokesCallbackWithFalse()
        {
            // Act & Assert
            _textBuddyClient.IsTokenRegistered(null, (result) =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.False);
            });
        }

        #endregion
    }
}
