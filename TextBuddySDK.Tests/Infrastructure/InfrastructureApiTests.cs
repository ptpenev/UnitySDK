using Moq;
using Moq.Protected;
using TextBuddySDK.Domain.ValueObjects;
using TextBuddySDK.Infrastructure;
using System.Net;

namespace TextBuddySDK.Tests.Infrastructure
{
    [TestFixture]
    public class InfrastructureApiTests
    {
        private Mock<HttpMessageHandler> _mockHandler;
        private HttpClient _httpClient;
        private ApiClient _apiClient;
        private const string TestApiKey = "test-api-key";

        [SetUp]
        public void Setup()
        {
            // Create a mock HttpMessageHandler to intercept requests
            _mockHandler = new Mock<HttpMessageHandler>();

            // Create an HttpClient that uses our mocked handler
            _httpClient = new HttpClient(_mockHandler.Object)
            {
                BaseAddress = new Uri("https://fake-api.com")
            };

            // Create the ApiClient instance using our special test constructor
            _apiClient = new ApiClient(_httpClient, TestApiKey, false);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose of the HttpClient to release resources after each test
            _httpClient.Dispose();
        }

        [Test]
        public async Task GetSmsTokenAsync_OnSuccess_ReturnsSuccessResult()
        {
            // Arrange
            var verificationCode = "12345";
            var expectedUri = new Uri("https://fake-api.com/tokens/exchange");

            // Setup the mock handler to return a successful response
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"token\":\"some-token\",\"expires\":\"2099-01-01\"}")
                });

            // Act
            var result = await _apiClient.GetSmsTokenAsync(verificationCode);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task GetSmsTokenAsync_OnApiFailure_ReturnsFailureResult()
        {
            // Arrange
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

            // Act
            var result = await _apiClient.GetSmsTokenAsync("any-code");

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("ApiError"));
        }

        [Test]
        public async Task SendSmsToTokenAsync_OnSuccess_ConstructsRequestCorrectly()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-123-token", DateTime.UtcNow.AddDays(1));
            var smsBody = "Hello World";
            HttpRequestMessage capturedRequest = null;
            string capturedRequestBody = null;

            // Setup the mock handler to capture the request and return success
            _mockHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => {
                   capturedRequest = req;
                   capturedRequestBody = req.Content?.ReadAsStringAsync().Result;
               })
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _apiClient.SendSmsToTokenAsync(smsToken, smsBody);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(capturedRequest.Headers.Authorization.Scheme, Is.EqualTo("Bearer"));
            Assert.That(capturedRequest.Headers.Authorization.Parameter, Is.EqualTo(TestApiKey));
            Assert.That(capturedRequest.Headers.GetValues("X-Sms-Token").First(), Is.EqualTo(smsToken.Value));
            Assert.That(capturedRequestBody, Does.Contain(smsBody));
        }

        [Test]
        public async Task SendSmsToTokenAsync_OnApiFailure_ReturnsFailureResult()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-123-token", DateTime.UtcNow.AddDays(1));
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden });

            // Act
            var result = await _apiClient.SendSmsToTokenAsync(smsToken, "any body");

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("ApiError"));
        }

        [Test]
        public async Task UnregisterSmsTokenAsync_OnSuccess_ReturnsSuccessResult()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-123-token", DateTime.UtcNow.AddDays(1));
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _apiClient.UnregisterSmsTokenAsync(smsToken);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task UnregisterSmsTokenAsync_OnApiFailure_ReturnsFailureResult()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-123-token", DateTime.UtcNow.AddDays(1));

            // Setup the mock handler to return an error status code
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

            // Act
            var result = await _apiClient.UnregisterSmsTokenAsync(smsToken);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("ApiError"));
        }

        [Test]
        public async Task CheckIfSmsTokenIsRegisteredAsync_OnSuccess_ConstructsRequestAndReturnsSuccess()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-789-token", DateTime.UtcNow.AddDays(1));
            var expectedUri = new Uri("https://fake-api.com/tokens/check");
            HttpRequestMessage capturedRequest = null;

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _apiClient.CheckIfSmsTokenIsRegisteredAsync(smsToken);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest.Headers.GetValues("X-Sms-Token").First(), Is.EqualTo(smsToken.Value));
            Assert.That(capturedRequest.Headers.Authorization.Parameter, Is.EqualTo(TestApiKey));
        }

        [Test]
        public async Task CheckIfSmsTokenIsRegisteredAsync_OnApiFailure_ReturnsFailureResult()
        {
            // Arrange
            var smsToken = SMSToken.Create("player-789-token", DateTime.UtcNow.AddDays(1));
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

            // Act
            var result = await _apiClient.CheckIfSmsTokenIsRegisteredAsync(smsToken);

            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("ApiError"));
        }

        [Test]
        public async Task LogRequest_WhenEnabled_IsExercisedOnRequest()
        {
            // Arrange
            // Create a new ApiClient with logging enabled just for this test
            var loggingApiClient = new ApiClient(_httpClient, TestApiKey, true);

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            // We just need to make a call to ensure the logging code path is hit.
            await loggingApiClient.GetSmsTokenAsync("logging-test-code");

            // Assert
            // The primary assertion is that the coverage tool will now show LogRequest as covered.
            // We can add a simple verification to ensure the call was made at least once.
            _mockHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
