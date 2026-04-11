using Microsoft.Extensions.Logging;
using Moq;
using MovieRentalApp.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MovieTesting
{
    /// <summary>
    /// Additional coverage for ChatbotService retry/error paths.
    /// </summary>
    public class ChatbotServiceAdditionalTests
    {
        private static ChatbotService MakeService(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
            return new ChatbotService(client, new Mock<ILogger<ChatbotService>>().Object);
        }

        // ── Connection refused → retries then returns unavailable ─

        [Fact]
        public async Task AskAboutMovieAsync_ConnectionRefused_ReturnsUnavailableMessage()
        {
            var sut = MakeService(new ThrowingHandler(
                new HttpRequestException("Connection actively refused")));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Contains("unavailable", result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Timeout → retries then returns timeout message ────────

        [Fact]
        public async Task AskAboutMovieAsync_Timeout_ReturnsTimeoutMessage()
        {
            var sut = MakeService(new ThrowingHandler(new TaskCanceledException("timeout")));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Contains("too long", result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Unexpected exception → returns generic error ──────────

        [Fact]
        public async Task AskAboutMovieAsync_UnexpectedException_ReturnsGenericError()
        {
            var sut = MakeService(new ThrowingHandler(new InvalidOperationException("boom")));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Contains("wrong", result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Null answer field → returns "No response" ─────────────

        [Fact]
        public async Task AskAboutMovieAsync_NullAnswerValue_ReturnsNoResponse()
        {
            var json = JsonSerializer.Serialize(new { answer = (string?)null });
            var sut  = MakeService(new FakeHandler(HttpStatusCode.OK, json));

            var result = await sut.AskAboutMovieAsync("Inception", "What is it about?");

            Assert.Equal("No response from AI.", result);
        }

        // ── Whitespace title ──────────────────────────────────────

        [Fact]
        public async Task AskAboutMovieAsync_WhitespaceTitle_ReturnsValidationMessage()
        {
            var sut = MakeService(new FakeHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.AskAboutMovieAsync("   ", "What is it about?");

            Assert.Equal("Please provide a valid movie title and question.", result);
        }

        // ── Helpers ───────────────────────────────────────────────

        private class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public FakeHandler(HttpStatusCode status, string body) { _status = status; _body = body; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
                => Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
        }

        private class ThrowingHandler : HttpMessageHandler
        {
            private readonly Exception _ex;
            public ThrowingHandler(Exception ex) { _ex = ex; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
                => Task.FromException<HttpResponseMessage>(_ex);
        }
    }
}
