using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenreController : ControllerBase
    {
        private readonly IGenreService _genreService;

        public GenreController(IGenreService genreService)
        {
            _genreService = genreService;
        }

        // GET /api/Genre
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllGenres()
        {
            try { return Ok(await _genreService.GetAllGenres()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /api/Genre
        [HttpPost]
        [Authorize(Roles = "Admin,ContentManager")]
        public async Task<IActionResult> AddGenre([FromBody] GenreCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _genreService.AddGenre(dto)); }
            catch (DuplicateEntityException ex) { return Conflict(new { message = ex.Message }); }
            catch (BusinessRuleViolationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
