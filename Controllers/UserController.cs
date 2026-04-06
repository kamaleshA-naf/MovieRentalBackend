using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(UserCreateDto dto)
        {
            var result = await _userService.Register(dto);
            return Created($"api/User/{result.Id}", result);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _userService.Login(dto);
                return Ok(result);
            }
            catch (EntityNotFoundException)
            {
                // Invalid credentials - return 401 without throwing so debugger won't break on first-chance exceptions
                return Unauthorized(new { message = "Invalid email or password." });
            }
            catch (UnauthorizedException ex)
            {
                // Account inactive or other auth-related issues
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetAllUsers()
        {
            return Ok(await _userService.GetAllUsers());
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            return Ok(await _userService.DeleteUser(id));
        }
    }
}
