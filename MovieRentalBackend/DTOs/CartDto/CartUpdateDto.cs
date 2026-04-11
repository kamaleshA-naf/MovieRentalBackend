using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class CartUpdateDto
    {
        [Range(1, 30,
            ErrorMessage = "Duration must be between 1 and 30 days.")]
        public int DurationDays { get; set; } = 7;
    }
}