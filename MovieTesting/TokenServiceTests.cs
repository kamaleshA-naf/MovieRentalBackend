using Microsoft.Extensions.Configuration;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;
using System.IdentityModel.Tokens.Jwt;

namespace MovieTesting
{
    public class TokenServiceTests
    {
        private static TokenService MakeSut(string key = "SuperSecretKey1234567890ABCDEF!!")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Keys:Jwt"] = key })
                .Build();
            return new TokenService(config);
        }

        [Fact]
        public void CreateToken_ValidPayload_ReturnsNonEmptyToken()
        {
            var sut = MakeSut();
            var result = sut.CreateToken(new TokenPayloadDto { UserId = 1, UserName = "Alice", Role = "Customer" });
            Assert.NotEmpty(result);
        }

        [Fact]
        public void CreateToken_ContainsCorrectClaims()
        {
            var sut = MakeSut();
            var token = sut.CreateToken(new TokenPayloadDto { UserId = 5, UserName = "Bob", Role = "Admin" });

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal("5", jwt.Claims.First(c => c.Type == "nameid" || c.Type.EndsWith("nameidentifier")).Value);
            Assert.Equal("Bob", jwt.Claims.First(c => c.Type == "unique_name" || c.Type.EndsWith("/name")).Value);
        }

        [Fact]
        public void CreateToken_MissingJwtKey_ThrowsInvalidOperationException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();
            var sut = new TokenService(config);

            Assert.Throws<InvalidOperationException>(() =>
                sut.CreateToken(new TokenPayloadDto { UserId = 1, UserName = "X", Role = "Customer" }));
        }

        [Fact]
        public void CreateToken_DifferentUsers_ProduceDifferentTokens()
        {
            var sut = MakeSut();
            var t1 = sut.CreateToken(new TokenPayloadDto { UserId = 1, UserName = "Alice", Role = "Customer" });
            var t2 = sut.CreateToken(new TokenPayloadDto { UserId = 2, UserName = "Bob", Role = "Admin" });
            Assert.NotEqual(t1, t2);
        }
    }
}
