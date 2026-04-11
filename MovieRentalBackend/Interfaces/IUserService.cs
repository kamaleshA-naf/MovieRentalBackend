using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> Register(UserCreateDto dto);
        Task<LoginResponseDto> Login(LoginDto dto);
        Task<IEnumerable<UserResponseDto>> GetAllUsers();
        Task<UserResponseDto> DeleteUser(int id);
    }
}
