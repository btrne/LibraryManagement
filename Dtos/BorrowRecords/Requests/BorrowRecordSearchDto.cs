namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class BorrowRecordSearchDto
    {
        public string? PhoneNumber { get; set; }
        public string? BorrowerName { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsOverdue { get; set; }
    }
}