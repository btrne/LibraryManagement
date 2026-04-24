using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library.API.Entities
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(13)]
        public string Isbn { get; set; } = string.Empty;

        // Khóa ngoại (Foreign Keys)
        public int AuthorId { get; set; }
        public int CategoryId { get; set; }

        // Navigation Properties
        [ForeignKey("AuthorId")]
        public Author Author { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;
    }
}