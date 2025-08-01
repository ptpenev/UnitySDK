using Moq;
using NUnit.Framework;
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

        #region TextBuddyConfig Tests

        [Test]
        public void Config_Constructor_WithValidValues_SetsPropertiesCorrectly()
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
        public void Config_Constructor_WithNullApiKey_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig(null, "url", "phone"));
        }

        [Test]
        public void Config_Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig("key", null, "phone"));
        }

        [Test]
        public void Config_Constructor_WithNullPhone_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TextBuddyConfig("key", "url", null));
        }

        #endregion

        #region SMSToken Tests

        [Test]
        public void SMSToken_Create_WithNullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SMSToken.Create(null, DateTime.UtcNow));
        }

        [Test]
        public void SMSToken_ToString_ReturnsTokenValue()
        {
            // Arrange
            var tokenValue = "my-test-token";
            var token = SMSToken.Create(tokenValue, DateTime.UtcNow);

            // Act & Assert
            Assert.That(token.ToString(), Is.EqualTo(tokenValue));
        }

        #endregion

        #region Result Tests

        [Test]
        public void TextBuddyResult_Success_CreatesSuccessResult()
        {
            // Act
            var result = TextBuddyResult.Success();

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void TextBuddyResult_Failure_CreatesFailureResult()
        {
            // Arrange
            var error = new Error("code", "message");

            // Act
            var result = TextBuddyResult.Failure(error);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void TextBuddyResultT_Success_CreatesSuccessResultWithValue()
        {
            // Arrange
            var value = 123;

            // Act
            var result = TextBuddyResult<int>.Success(value);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(value));
        }

        [Test]
        public void CheckTokenResult_Failure_CreatesFailureResult()
        {
            // Arrange
            var error = new Error("Test", "Test Error");

            // Act
            var result = CheckTokenResult.Failure(error);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void Result_AccessingValueOnFailedResult_ThrowsException()
        {
            // Arrange
            var failedResult = TextBuddyResult<bool>.Failure(new Error("Test", "Test Error"));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var v = failedResult.Value; });
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
            _mockSmsAppLauncher.Verify(launcher => launcher.OpenSmsApp(
                _config.TextBuddyPhoneNumber,
                It.Is<string>(s => s.StartsWith("tb_verify_"))),
                Times.Once);

            _mockAnalyticsService.Verify(analytics => analytics.SendEventAsync(
                "RegistrationInitialized",
                It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        #endregion

        #region ProcessDeepLinkAsync Tests

        [Test]
        public async Task ProcessDeepLinkAsync_WithValidCode_ReturnsSuccessResult()
        {
            // Arrange
            var verificationCode = "good_code";
            var deepLink = new Uri($"mygame://auth?code={verificationCode}");
            var expectedToken = SMSToken.Create("new-sms-token", DateTime.UtcNow.AddDays(1));
            var successResult = SmsTokenResult.Success(expectedToken);

            _mockApiClient
                .Setup(api => api.GetSmsTokenAsync(verificationCode))
                .ReturnsAsync(successResult);

            // Act
            var result = await _textBuddyClient.ProcessDeepLinkAsync(deepLink);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Value, Is.EqualTo(expectedToken.Value));
        }

        [Test]
        public async Task ProcessDeepLinkAsync_WithNoCode_ReturnsFailureResult()
        {
            // Arrange
            var deepLink = new Uri("mygame://auth?otherparam=123");

            // Act
            var result = await _textBuddyClient.ProcessDeepLinkAsync(deepLink);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("InvalidDeepLink"));
            Assert.That(result.Error.Message, Is.EqualTo("Verification code not found in deep link URL."));
            _mockApiClient.Verify(api => api.GetSmsTokenAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region SendSmsAsync Tests

        [Test]
        public async Task SendSmsAsync_WithValidTokenAndBody_ReturnsSuccess()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            var message = "Hello World";
            _mockApiClient
                .Setup(api => api.SendSmsToTokenAsync(validToken, message))
                .ReturnsAsync(SendSmsResult.Success());

            // Act
            var result = await _textBuddyClient.SendSmsAsync(validToken, message);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task SendSmsAsync_WithEmptyBody_ReturnsFailureResult()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));

            // Act
            var result = await _textBuddyClient.SendSmsAsync(validToken, "");

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("InvalidBody"));
            Assert.That(result.Error.Message, Is.EqualTo("SMS body cannot be empty."));
        }

        [Test]
        public async Task SendSmsAsync_WithExpiredToken_ReturnsFailureResult()
        {
            // Arrange
            var expiredToken = SMSToken.Create("some-token", DateTime.UtcNow.AddMinutes(-5));
            var message = "Hello World";

            // Act
            var result = await _textBuddyClient.SendSmsAsync(expiredToken, message);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("InvalidToken"));
            Assert.That(result.Error.Message, Is.EqualTo("SMS Token is null or expired."));
        }

        #endregion

        #region UnregisterAsync Tests

        [Test]
        public async Task UnregisterAsync_WithValidToken_ReturnsSuccess()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            _mockApiClient
                .Setup(api => api.UnregisterSmsTokenAsync(validToken))
                .ReturnsAsync(UnregisterResult.Success());

            // Act
            var result = await _textBuddyClient.UnregisterAsync(validToken);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task UnregisterAsync_WithNullToken_ReturnsFailure()
        {
            // Act
            var result = await _textBuddyClient.UnregisterAsync(null);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("InvalidToken"));
            Assert.That(result.Error.Message, Is.EqualTo("SMS Token cannot be null."));
        }

        [Test]
        public async Task UnregisterAsync_WhenApiFails_ReturnsFailure()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            var apiError = new Error("ApiError", "Failed to connect");
            _mockApiClient
                .Setup(api => api.UnregisterSmsTokenAsync(validToken))
                .ReturnsAsync(UnregisterResult.Failure(apiError));

            // Act
            var result = await _textBuddyClient.UnregisterAsync(validToken);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(apiError));
        }

        #endregion

        #region IsTokenRegisteredAsync Tests

        [Test]
        public async Task IsTokenRegisteredAsync_WithValidAndRegisteredToken_ReturnsTrue()
        {
            // Arrange
            var validToken = SMSToken.Create("valid-token", DateTime.UtcNow.AddDays(1));
            _mockApiClient
                .Setup(api => api.CheckIfSmsTokenIsRegisteredAsync(validToken))
                .ReturnsAsync(CheckTokenResult.Success(true));

            // Act
            var result = await _textBuddyClient.IsTokenRegisteredAsync(validToken);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public async Task IsTokenRegisteredAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var expiredToken = SMSToken.Create("expired-token", DateTime.UtcNow.AddMinutes(-1));

            // Act
            var result = await _textBuddyClient.IsTokenRegisteredAsync(expiredToken);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.False);
            // Verify we didn't even call the API since we can fail fast
            _mockApiClient.Verify(api => api.CheckIfSmsTokenIsRegisteredAsync(It.IsAny<SMSToken>()), Times.Never);
        }

        [Test]
        public async Task IsTokenRegisteredAsync_WithNullToken_ReturnsFalse()
        {
            // Act
            var result = await _textBuddyClient.IsTokenRegisteredAsync(null);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.False);
        }

        #endregion
    }
}
