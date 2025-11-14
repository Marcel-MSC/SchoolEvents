namespace SchoolEvents.API.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNext => CurrentPage < TotalPages;
        public bool HasPrevious => CurrentPage > 1;
    }

    public class PaginationParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}