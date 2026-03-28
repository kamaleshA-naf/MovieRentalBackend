namespace MovieRentalApp.DTOs.Chatbot
{
    public class ChatRequestDto
    {
        public string MovieTitle { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}