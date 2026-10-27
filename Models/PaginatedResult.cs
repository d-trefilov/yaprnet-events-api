namespace EventsApi.Models
{
    public class PaginatedResult<T>
    {
        public int TotalCount {  get; set; }
        public IEnumerable<T> Events { get; set; } = [];
        public int PageNumber { get; set; }
        public int ItemsOnPage { get; set; }
    }
}
