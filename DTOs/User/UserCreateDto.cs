using System.ComponentModel.DataAnnotations;
using MovieRentalApp.Models;

namespace MovieRentalApp.Models.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Customer;
    }
}