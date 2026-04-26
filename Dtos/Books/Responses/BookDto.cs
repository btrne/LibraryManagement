namespace Library.API.Dtos.Books.Responses
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; } // Lấy tên thay vì ID
        public string? CategoryName { get; set; }
    }
}