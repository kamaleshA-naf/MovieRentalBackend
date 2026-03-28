using Microsoft.AspNetCore.Mvc;
using MovieRentalApp.DTOs.Chatbot;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
        {
            var answer = await _chatbotService.AskAboutMovieAsync(
                request.MovieTitle,
                request.Question
            );
            return Ok(new ChatResponseDto { Answer = answer });
        }
    }
}