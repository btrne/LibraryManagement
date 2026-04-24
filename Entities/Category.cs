using System.ComponentModel.DataAnnotations;

namespace Library.API.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Navigation Property: Một thể loại có nhiều sách
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}