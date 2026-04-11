using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenreController : ControllerBase
    {
        private readonly IGenreService _genreService;

        public GenreController(IGenreService genreService)
        {
            _genreService = genreService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetAllGenres()
        {
            try
            {
                return Ok(await _genreService.GetAllGenres());
            }
            catch { throw; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> AddGenre(GenreCreateDto dto)
        {
            try
            {
                var result = await _genreService.AddGenre(dto);
                return Created($"api/Genre/{result.Id}", result);
            }
            catch { throw; }
        }
    }
}
