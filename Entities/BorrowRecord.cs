using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime DueDate { get; set; } 

        // Navigation Property
        public ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();
    }
}