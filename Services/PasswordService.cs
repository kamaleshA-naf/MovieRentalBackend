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
                    "Password cannot be null or empty.",
                    nameof(password));

            HMACSHA256 hmac;

           
            if (dbHashKey == null)
            {
                
                hmac = new HMACSHA256();
                hashkey = hmac.Key;
            }
            else
            {
                
                hmac = new HMACSHA256(dbHashKey);
                hashkey = null;
            }

            
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashedPassword = hmac.ComputeHash(passwordBytes);

            
            hmac.Dispose();
            return hashedPassword;
        }
    }
}