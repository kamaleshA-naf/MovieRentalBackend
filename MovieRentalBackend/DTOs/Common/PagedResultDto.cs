namespace MovieRentalApp.Models.DTOs
{
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public IEnumerable<T> Items => Data; // alias for frontend compatibility
        public int TotalCount { get; set; }
        public int TotalItems => TotalCount; // alias
        public int PageNumber { get; set; }
        public int Page => PageNumber;       // alias
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}