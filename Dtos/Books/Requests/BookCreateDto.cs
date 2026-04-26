using System.ComponentModel.DataAnnotations;

namespace Library.API.Dtos.Book.Requests
{
    public class BookCreateDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(13)] 
        public string Isbn { get; set; } = string.Empty;

        // Linh hoạt cho Author
        public int? AuthorId { get; set; }
        public string? AuthorName { get; set; }

        // Linh hoạt cho Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}