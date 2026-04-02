using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // POST /api/User/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _userService.Register(dto)); }
            catch (DuplicateEntityException ex) { return Conflict(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/User/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _userService.Login(dto)); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedException ex) { return Unauthorized(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /api/User
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try { return Ok(await _userService.GetAllUsers()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // DELETE /api/User/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid user ID." });
            try { return Ok(new { message = "User deleted.", data = await _userService.DeleteUser(id) }); }
            catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

    }
}
