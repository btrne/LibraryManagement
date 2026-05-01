namespace Library.API.Dtos.Books.Responses
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public List<BookCopyDto> Copies { get; set; } = new List<BookCopyDto>();
    }
}