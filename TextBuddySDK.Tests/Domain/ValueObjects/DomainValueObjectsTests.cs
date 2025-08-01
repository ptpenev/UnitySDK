using TextBuddySDK.Domain.ValueObjects;

namespace TextBuddySDK.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class DomainValueObjectsTests
    {
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
    }
}