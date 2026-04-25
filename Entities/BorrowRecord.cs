using System.ComponentModel.DataAnnotations;

namespace Library.API.Entities
{
    public class BorrowRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BorrowerName { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime? ReturnDate { get; set; } // Nullable nếu chưa trả sách

        // Navigation Property
        public Book Book { get; set; } = null!;
    }
}