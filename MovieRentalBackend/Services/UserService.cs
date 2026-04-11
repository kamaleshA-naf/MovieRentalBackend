using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;

namespace MovieRentalApp.Services
{
    [DebuggerNonUserCode]
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;
        private readonly AuditLogService _auditLog;

        public UserService(
            IRepository<int, User> userRepository,
            ITokenService tokenService,
            IPasswordService passwordService,
            AuditLogService auditLog)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordService = passwordService;
            _auditLog = auditLog;
        }

        // ── REGISTER ──────────────────────────────────────────────
        public async Task<UserResponseDto> Register(UserCreateDto dto)
        {
            var existing = await _userRepository
                .FindAsync(u => u.UserEmail == dto.Email);
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

            await _auditLog.LogAsync(
                created.UserId,
                created.UserName,
                created.Role.ToString(),
                $"New account registered with email '{dto.Email}'.",
                "");

            return MapToDto(created);
        }

        // ── LOGIN ─────────────────────────────────────────────────
        public async Task<LoginResponseDto> Login(LoginDto dto)
        {
            var users = await _userRepository
                .FindAsync(u => u.UserEmail == dto.Email);
            var user = users.FirstOrDefault();

            if (user == null)
                throw new UnauthorizedException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedException(
                    "Your account has been deactivated. Please contact support.");

            var hashed = _passwordService
                .HashPassword(dto.Password, user.PasswordSaltValue, out _);

            if (!hashed.SequenceEqual(user.Password))
                throw new UnauthorizedException("Invalid email or password.");

            var token = _tokenService.CreateToken(new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role.ToString()
            });

            // ✅ Audit: successful login
            await _auditLog.LogAsync(
                user.UserId,
                user.UserName,
                user.Role.ToString(),
                $"User '{user.UserName}' ({user.Role}) logged in successfully.",
                "");

            return new LoginResponseDto
            {
                Token = token
            };
        }

        // ── GET USER — REMOVED (not used in frontend) ─────────────

        // ── GET ALL ───────────────────────────────────────────────
        public async Task<IEnumerable<UserResponseDto>> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        // ── UPDATE — REMOVED (not used in frontend) ───────────────

        // ── DELETE ────────────────────────────────────────────────
        public async Task<UserResponseDto> DeleteUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new EntityNotFoundException("User", id);

            await _userRepository.DeleteAsync(id);
            return MapToDto(user);
        }

        // ── MAPPER ────────────────────────────────────────────────
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