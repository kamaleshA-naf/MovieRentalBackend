using Microsoft.IdentityModel.Tokens;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MovieRentalApp.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(TokenPayloadDto payload)
        {
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Keys:Jwt"]!));

            var credentials = new SigningCredentials(
                signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    payload.UserId.ToString()),
                new Claim(ClaimTypes.Name,
                    payload.UserName),
                new Claim(ClaimTypes.Role,
                    payload.Role)
            };

            var token = new JwtSecurityToken(
                issuer: "MovieRentalApp",
                audience: "MovieRentalApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}