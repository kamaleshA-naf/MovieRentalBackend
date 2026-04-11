using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class WishlistCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int MovieId { get; set; }
    }
}