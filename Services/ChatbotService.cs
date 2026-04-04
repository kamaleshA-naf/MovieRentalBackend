using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MovieRentalApp.Interfaces;

namespace MovieRentalApp.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;

        private const int MaxRetries = 2;
        private const int RetryDelayMs = 1000;

        public ChatbotService(HttpClient httpClient, ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> AskAboutMovieAsync(string movieTitle, string question)
        {
            if (string.IsNullOrWhiteSpace(movieTitle) || string.IsNullOrWhiteSpace(question))
                return "Please provide a valid movie title and question.";


            var prompt  = $"About the movie '{movieTitle}': {question}";
            var payload = new { prompt };
            var json    = JsonSerializer.Serialize(payload);

            for (int attempt = 1; attempt <= MaxRetries + 1; attempt++)
            {
                // Recreate content each retry — StringContent can only be sent once
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    _logger.LogInformation(
                        "[Chatbot] Attempt {Attempt}/{Max} for movie '{Movie}'",
                        attempt, MaxRetries + 1, movieTitle);

                    var response = await _httpClient.PostAsync("/ask", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "[Chatbot] Status {Status} on attempt {Attempt}",
                            response.StatusCode, attempt);

                        if (attempt <= MaxRetries)
                        {
                            await Task.Delay(RetryDelayMs);
                            continue;
                        }

                        return "The AI service returned an error. Please try again later.";
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result       = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    if (result.TryGetProperty("answer", out var answerProp))
                        return answerProp.GetString() ?? "No response from AI.";

                    _logger.LogWarning("[Chatbot] Missing 'answer' field: {Json}", responseJson);
                    return "Unexpected response format from AI service.";
                }
                catch (HttpRequestException ex) when (
                    ex.Message.Contains("refused") ||
                    ex.Message.Contains("actively refused") ||
                    ex.Message.Contains("No connection"))
                {
                    _logger.LogWarning(
                        "[Chatbot] Connection refused on attempt {Attempt}. " +
                        "AI service may not be running at {Base}",
                        attempt, _httpClient.BaseAddress);

                    if (attempt <= MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs);
                        continue;
                    }

                    return "AI service is currently unavailable. Please try again later.";
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("[Chatbot] Timeout on attempt {Attempt}", attempt);

                    if (attempt <= MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs);
                        continue;
                    }

                    return "The AI service took too long to respond. Please try again.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Chatbot] Unexpected error on attempt {Attempt}", attempt);
                    return "Something went wrong with the AI service. Please try again later.";
                }
            }

            return "AI service is currently unavailable. Please try again later.";
        }
    }
}
