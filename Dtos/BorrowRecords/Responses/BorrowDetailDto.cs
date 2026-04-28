namespace Library.API.Dtos.BorrowRecords.Responses
{
    public class BorrowDetailDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime? ReturnDate { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsReturned => ReturnDate != null;
    }
}