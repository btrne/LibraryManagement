namespace Library.API.Dtos.Books.Requests
{
    public class BookSearchDto
    {
        public string? Keyword { get; set; }
        public int? AuthorId { get; set; }
        public int? CategoryId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
    }
}