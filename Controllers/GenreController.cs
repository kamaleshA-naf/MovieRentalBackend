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
            return Ok(await _genreService.GetAllGenres());
        }

        [Authorize(Roles = "Admin,ContentManager")]
        [HttpPost]
        public async Task<ActionResult> AddGenre(GenreCreateDto dto)
        {
            var result = await _genreService.AddGenre(dto);
            return Created($"api/Genre/{result.Id}", result);
        }
    }
}
