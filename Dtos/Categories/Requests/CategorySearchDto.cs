namespace Library.API.Dtos.Categories.Requests
{
    public class CategorySearchDto
    {
        public string? Keyword { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}