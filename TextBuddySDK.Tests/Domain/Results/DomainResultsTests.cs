using TextBuddySDK.Domain.Results;

namespace TextBuddySDK.Tests.Domain.Results
{
    [TestFixture]
    public class DomainResultsTests
    {
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
    }
}
