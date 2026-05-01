using System;
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
        public int BookCopyId { get; set; }

        public DateTime? ReturnDate { get; set; } // Ngày trả riêng cho cuốn sách vật lý này

        [Column(TypeName = "decimal(18, 0)")]
        public decimal FineAmount { get; set; } = 0; // Tiền phạt riêng cho cuốn này

        // Navigation Properties
        [ForeignKey("BorrowRecordId")]
        public BorrowRecord BorrowRecord { get; set; } = null!;

        [ForeignKey("BookCopyId")]
        public BookCopy BookCopy { get; set; } = null!;
    }
}