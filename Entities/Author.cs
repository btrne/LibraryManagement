using System.ComponentModel.DataAnnotations;

namespace Library.API.Entities
{
    public class Author
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation Property: Một tác giả viết nhiều sách
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}