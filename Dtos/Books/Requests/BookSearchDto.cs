namespace Library.API.Dtos.Books.Requests
{
    public class BookSearchDto
    {
        public string? Keyword { get; set; }
        public int? AuthorId { get; set; }
        public int? CategoryId { get; set; }
    }
}