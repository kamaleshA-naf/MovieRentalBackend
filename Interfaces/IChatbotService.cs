namespace MovieRentalApp.Interfaces
{
    public interface IChatbotService
    {
        Task<string> AskAboutMovieAsync(string movieTitle, string question);
    }
}