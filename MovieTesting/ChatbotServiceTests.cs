using Microsoft.Extensions.Logging;
using Moq;
using MovieRentalApp.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MovieTesting
{
    public class ChatbotServiceTests
    {
        private static ChatbotService MakeService(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
            var logger = new Mock<ILogger<ChatbotService>>().Object;
            return new ChatbotService(client, logger);
        }

        [Fact]
        public async Task AskAboutMovieAsync_EmptyTitle_ReturnsValidationMessage()
        {
            var sut = MakeService(new FakeHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.AskAboutMovieAsync("", "What is it about?");

            Assert.Equal("Please provide a valid movie title and question.", result);
        }

        [Fact]
        public async Task AskAboutMovieAsync_EmptyQuestion_ReturnsValidationMessage()
        {
            var sut = MakeService(new FakeHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.AskAboutMovieAsync("Inception", "");

            Assert.Equal("Please provide a valid movie title and question.", result);
        }

        [Fact]
        public async Task AskAboutMovieAsync_SuccessResponse_ReturnsAnswer()
        {
            var json = JsonSerializer.Serialize(new { answer = "It's about dreams." });
            var sut = MakeService(new FakeHandler(HttpStatusCode.OK, json));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Equal("It's about dreams.", result);
        }

        [Fact]
        public async Task AskAboutMovieAsync_MissingAnswerField_ReturnsUnexpectedFormat()
        {
            var sut = MakeService(new FakeHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Equal("Unexpected response format from AI service.", result);
        }

        [Fact]
        public async Task AskAboutMovieAsync_ServerError_ReturnsErrorMessage()
        {
            var sut = MakeService(new FakeHandler(HttpStatusCode.InternalServerError, ""));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Fake HTTP handler ─────────────────────────────────────
        private class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            public FakeHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
