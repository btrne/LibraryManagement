namespace Library.API.Dtos.Categories.Responses
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }
}