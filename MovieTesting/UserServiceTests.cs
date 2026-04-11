using Moq;
using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Services;

namespace MovieTesting
{
    // Stub that swallows all log calls without needing a real DB context
    internal class FakeAuditLogService : AuditLogService
    {
        public FakeAuditLogService() : base(null!) { }
        public override Task LogAsync(int userId, string userName, string role, string message, string errorNumber = "")
            => Task.CompletedTask;
    }

    public class UserServiceTests
    {
        private readonly Mock<IRepository<int, User>> _userRepo = new();
        private readonly Mock<ITokenService> _tokenService = new();
        private readonly Mock<IPasswordService> _passwordService = new();
        private readonly AuditLogService _auditLog = new FakeAuditLogService();
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(
                _userRepo.Object,
                _tokenService.Object,
                _passwordService.Object,
                _auditLog);
        }

        // ── Register ──────────────────────────────────────────────

        [Fact]
        public async Task Register_NewUser_ReturnsUserResponseDto()
        {
            var dto = new UserCreateDto { Name = "Alice", Email = "alice@test.com", Password = "Pass1!" };
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<User>());
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                            .Returns((string pw, byte[]? key, out byte[]? salt) =>
                            {
                                salt = new byte[] { 1, 2, 3 };
                                return new byte[] { 4, 5, 6 };
                            });
            var created = new User { UserId = 1, UserName = "Alice", UserEmail = "alice@test.com", Role = UserRole.Customer, IsActive = true, CreatedAt = DateTime.UtcNow };
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(created);

            var result = await _sut.Register(dto);

            Assert.Equal("Alice", result.Name);
            Assert.Equal("alice@test.com", result.Email);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsDuplicateEntityException()
        {
            var dto = new UserCreateDto { Name = "Alice", Email = "alice@test.com", Password = "Pass1!" };
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(new[] { new User { UserEmail = "alice@test.com" } });

            await Assert.ThrowsAsync<DuplicateEntityException>(() => _sut.Register(dto));
        }

        // ── Login ─────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var salt = new byte[] { 1, 2, 3 };
            var hash = new byte[] { 4, 5, 6 };
            var user = new User { UserId = 1, UserName = "Alice", UserEmail = "alice@test.com", Password = hash, PasswordSaltValue = salt, Role = UserRole.Customer, IsActive = true };

            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(new[] { user });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), salt, out It.Ref<byte[]?>.IsAny))
                            .Returns((string pw, byte[]? key, out byte[]? s) => { s = null; return hash; });
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");

            var result = await _sut.Login(new LoginDto { Email = "alice@test.com", Password = "Pass1!" });

            Assert.Equal("jwt-token", result.Token);
        }

        [Fact]
        public async Task Login_UserNotFound_ThrowsUnauthorizedException()
        {
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<User>());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _sut.Login(new LoginDto { Email = "x@x.com", Password = "pass" }));
        }

        [Fact]
        public async Task Login_InactiveUser_ThrowsUnauthorizedException()
        {
            var user = new User { UserId = 1, UserEmail = "x@x.com", IsActive = false, Password = Array.Empty<byte>(), PasswordSaltValue = Array.Empty<byte>() };
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(new[] { user });

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _sut.Login(new LoginDto { Email = "x@x.com", Password = "pass" }));
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsUnauthorizedException()
        {
            var salt = new byte[] { 1, 2, 3 };
            var storedHash = new byte[] { 4, 5, 6 };
            var wrongHash = new byte[] { 7, 8, 9 };
            var user = new User { UserId = 1, UserEmail = "x@x.com", Password = storedHash, PasswordSaltValue = salt, IsActive = true };

            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(new[] { user });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), salt, out It.Ref<byte[]?>.IsAny))
                            .Returns((string pw, byte[]? key, out byte[]? s) => { s = null; return wrongHash; });

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _sut.Login(new LoginDto { Email = "x@x.com", Password = "wrong" }));
        }

        // ── GetAllUsers ───────────────────────────────────────────

        [Fact]
        public async Task GetAllUsers_ReturnsAllMapped()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new User { UserId = 1, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true },
                new User { UserId = 2, UserName = "Bob",   UserEmail = "b@b.com", Role = UserRole.Admin,    IsActive = true }
            });

            var result = await _sut.GetAllUsers();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllUsers_Empty_ReturnsEmpty()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<User>());

            var result = await _sut.GetAllUsers();

            Assert.Empty(result);
        }

        [Fact]
        public async Task Register_MapsRoleCorrectly()
        {
            var dto = new UserCreateDto { Name = "Admin", Email = "admin@test.com", Password = "Pass1!", Role = UserRole.Admin };
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(Enumerable.Empty<User>());
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                            .Returns((string pw, byte[]? key, out byte[]? salt) =>
                            {
                                salt = new byte[] { 1, 2, 3 };
                                return new byte[] { 4, 5, 6 };
                            });
            var created = new User { UserId = 2, UserName = "Admin", UserEmail = "admin@test.com", Role = UserRole.Admin, IsActive = true, CreatedAt = DateTime.UtcNow };
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(created);

            var result = await _sut.Register(dto);

            Assert.Equal("Admin", result.Role);
        }

        // ── DeleteUser ────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_ExistingId_ReturnsDto()
        {
            var user = new User { UserId = 1, UserName = "Alice", UserEmail = "a@a.com", Role = UserRole.Customer, IsActive = true };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.DeleteUser(1);

            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task DeleteUser_NotFound_ThrowsEntityNotFoundException()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.DeleteUser(99));
        }
    }
}
