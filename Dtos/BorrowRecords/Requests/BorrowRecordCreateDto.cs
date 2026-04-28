namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class BorrowRecordCreateDto
    {
        public List<int> BookIds { get; set; } = new List<int>(); // Nhận danh sách ID sách
        public string BorrowerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}