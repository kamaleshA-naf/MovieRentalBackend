using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;

        public UserService(
            IRepository<int, User> userRepository,
            ITokenService tokenService,
            IPasswordService passwordService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordService = passwordService;
        }

        public async Task<UserResponseDto> Register(UserCreateDto dto)
        {
            var existing = await _userRepository.FindAsync(u => u.UserEmail == dto.Email);
            if (existing.Any())
                throw new DuplicateEntityException(
                    $"A user with email '{dto.Email}' already exists.");

            var hashed = _passwordService
                .HashPassword(dto.Password, null, out byte[]? salt);

            var user = new User
            {
                UserName = dto.Name,
                UserEmail = dto.Email,
                Password = hashed,
                PasswordSaltValue = salt!,
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _userRepository.AddAsync(user);
            return MapToDto(created);
        }

        public async Task<LoginResponseDto> Login(LoginDto dto)
        {
            var users = await _userRepository
                .FindAsync(u => u.UserEmail == dto.Email);
            var user = users.FirstOrDefault();

            if (user == null)
                throw new EntityNotFoundException(
                    "Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedException(
                    "Your account has been deactivated.");

            var hashed = _passwordService
                .HashPassword(dto.Password, user.PasswordSaltValue, out _);

            if (!hashed.SequenceEqual(user.Password))
                throw new UnauthorizedException(
                    "Invalid email or password.");

            var token = _tokenService.CreateToken(new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role.ToString()
            });

            return new LoginResponseDto
            {
                Token = token,
                Name = user.UserName,
                Email = user.UserEmail,
                Role = user.Role.ToString(),
                Message = "Login successful."
            };
        }

        public async Task<UserResponseDto> GetUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new EntityNotFoundException("User", id);
            return MapToDto(user);
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserResponseDto> UpdateUser(
            int id, UserUpdateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new EntityNotFoundException("User", id);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                user.UserName = dto.Name;

            var updated = await _userRepository.UpdateAsync(id, user);
            return MapToDto(updated!);
        }

        public async Task<UserResponseDto> DeleteUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new EntityNotFoundException("User", id);

            await _userRepository.DeleteAsync(id);
            return MapToDto(user);
        }

        public async Task<string> ChangePassword(ChangePasswordDto dto)
        {
            if (dto.UserId <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid user ID.");

            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            // Verify old password matches
            var hashedOld = _passwordService
                .HashPassword(dto.OldPassword,
                              user.PasswordSaltValue, out _);

            if (!hashedOld.SequenceEqual(user.Password))
                throw new UnauthorizedException(
                    "Current password is incorrect.");

            if (dto.OldPassword == dto.NewPassword)
                throw new BusinessRuleViolationException(
                    "New password cannot be the same as current password.");

            if (dto.NewPassword.Length < 6)
                throw new BusinessRuleViolationException(
                    "New password must be at least 6 characters.");

            var hashedNew = _passwordService
                .HashPassword(dto.NewPassword, null, out byte[]? newSalt);

            user.Password = hashedNew;
            user.PasswordSaltValue = newSalt!;
            await _userRepository.UpdateAsync(user.UserId, user);

            return "Password changed successfully.";
        }

        private static UserResponseDto MapToDto(User u) => new()
        {
            Id = u.UserId,
            Name = u.UserName,
            Email = u.UserEmail,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}