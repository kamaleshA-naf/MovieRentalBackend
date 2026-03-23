namespace MovieRentalApp.Interfaces

{
    public interface IPasswordService
    {
        byte[] HashPassword(
            string password,
            byte[]? dbHashKey,
            out byte[]? hashkey);
    }
}