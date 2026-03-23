using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models.DTOs
{
    public class GenreCreateDto
    {
        [Required(ErrorMessage = "Genre name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}