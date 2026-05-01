using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library.API.Entities
{
    public enum CopyStatus
    {
        Available = 0,
        Borrowed = 1,
        Damaged = 2,
        Lost = 3
    }

    public class BookCopy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        public CopyStatus Status { get; set; } = CopyStatus.Available;

        // Foreign Keys
        public int BookId { get; set; }

        // Navigation Properties
        [ForeignKey("BookId")]
        public Book Book { get; set; } = null!;

        // Một bản sao vật lý có thể có nhiều lịch sử mượn trả
        public ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();
    }
}