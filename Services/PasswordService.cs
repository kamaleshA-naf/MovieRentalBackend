using MovieRentalApp.Interfaces;
using System.Security.Cryptography;
using System.Text;
using MovieRentalApp.Models.DTOs;


namespace MovieRentalApp.Services
{
    public class PasswordService : IPasswordService
    {
        public byte[] HashPassword(
            string password,
            byte[]? dbHashKey,
            out byte[]? hashkey)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException(
                    "Password cannot be null or empty.", nameof(password));

            using var hmac = dbHashKey == null
                ? new HMACSHA256()
                : new HMACSHA256(dbHashKey);

            hashkey = dbHashKey == null ? hmac.Key : null;

            return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}