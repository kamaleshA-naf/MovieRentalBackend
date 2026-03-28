using System.Text;
using System.Text.Json;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;

        public ChatbotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> AskAboutMovieAsync(string movieTitle, string question)
        {
            var prompt = $"About the movie '{movieTitle}': {question}";
            var payload = new { prompt = prompt };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/ask", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var answer = result.GetProperty("answer").GetString() ?? "No response";

            return answer;
        }
    }
}