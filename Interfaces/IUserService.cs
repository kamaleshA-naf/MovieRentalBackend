using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> Register(UserCreateDto dto);
        Task<LoginResponseDto> Login(LoginDto dto);
        Task<UserResponseDto> GetUser(int id);
        Task<IEnumerable<UserResponseDto>> GetAllUsers();
        Task<UserResponseDto> UpdateUser(int id, UserUpdateDto dto);
        Task<UserResponseDto> DeleteUser(int id);
        Task<string> ChangePassword(ChangePasswordDto dto);
    }
}