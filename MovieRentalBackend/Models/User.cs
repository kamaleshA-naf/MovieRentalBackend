using System.ComponentModel.DataAnnotations;

namespace MovieRentalApp.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string UserName { get; set; } = string.Empty;
            

        [Required, EmailAddress]
        public string UserEmail { get; set; }  = string.Empty;
            

        [Required]
        public byte[] Password { get; set; } = Array.Empty<byte>();


        [Required]
        public byte[] PasswordSaltValue { get; set; } = Array.Empty<byte>();


        public bool IsActive { get; set; } = true;

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
       

        public ICollection<Rental> Rentals = new List<Rental>();
            
        public ICollection<Wishlist> Wishlists = new List<Wishlist>();

        public ICollection<Payment> Payments = new List<Payment>();
            
    }

    public enum UserRole
    {
        Admin = 1,
        Customer = 2,
        ContentManager = 3
    }
}