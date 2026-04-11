namespace MovieRentalApp.Models.DTOs
{
    public class PaginationDto
    {
        private int _pageNumber = 1;
        private int _pageSize   = 20;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;       // never below 1
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 20 : value > 200 ? 200 : value; // clamp 1–200
        }
    }
}
