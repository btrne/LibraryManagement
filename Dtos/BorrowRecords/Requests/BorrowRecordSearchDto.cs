namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class BorrowRecordSearchDto
    {
        public string? PhoneNumber { get; set; }
        public string? BorrowerName { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsOverdue { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
    }
}
