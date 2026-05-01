namespace Library.API.Dtos.Authors.Responses
{
    public class AuthorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }
}