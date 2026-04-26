namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class BorrowRecordCreateDto
    {
        public int BookId { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        //public DateTime BorrowDate { get; set; } = DateTime.Now;
    }
}