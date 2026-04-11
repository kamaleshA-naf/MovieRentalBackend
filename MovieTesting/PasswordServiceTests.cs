using MovieRentalApp.Services;

namespace MovieTesting
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _sut = new();

        [Fact]
        public void HashPassword_NewPassword_ReturnsHashAndKey()
        {
            var hash = _sut.HashPassword("Secret1!", null, out byte[]? key);

            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotNull(key);
        }

        [Fact]
        public void HashPassword_SamePasswordAndKey_ProducesSameHash()
        {
            var hash1 = _sut.HashPassword("Secret1!", null, out byte[]? key);
            var hash2 = _sut.HashPassword("Secret1!", key, out _);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
        {
            var hash1 = _sut.HashPassword("Secret1!", null, out byte[]? key);
            var hash2 = _sut.HashPassword("Other1!", key, out _);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_EmptyPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.HashPassword("", null, out _));
        }

        [Fact]
        public void HashPassword_WithExistingKey_DoesNotOutputNewKey()
        {
            _sut.HashPassword("Secret1!", null, out byte[]? key);
            _sut.HashPassword("Secret1!", key, out byte[]? secondKey);

            Assert.Null(secondKey);
        }
    }
}
