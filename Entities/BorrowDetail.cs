using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library.API.Entities
{
    public class BorrowDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BorrowRecordId { get; set; }

        [Required]
        public int BookId { get; set; }

        public DateTime? ReturnDate { get; set; } // Ngày trả riêng cho cuốn này

        [Column(TypeName = "decimal(18, 0)")]
        public decimal FineAmount { get; set; } = 0; // Tiền phạt riêng cho cuốn này

        // Navigation Properties
        public BorrowRecord BorrowRecord { get; set; } = null!;
        public Book Book { get; set; } = null!;
    }
}