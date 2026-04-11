using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(TokenPayloadDto payload);
    }
}