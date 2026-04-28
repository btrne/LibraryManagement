namespace Library.API.Dtos.BorrowRecords.Responses
{
    public class BorrowRecordDto
    {
        public int Id { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        
        // Danh sách các cuốn sách trong phiếu này
        public List<BorrowDetailDto> BorrowDetails { get; set; } = new List<BorrowDetailDto>();
        
        // Check xem phiếu này đã trả sạch sẽ chưa
        public bool IsFullyReturned => BorrowDetails.All(d => d.ReturnDate != null);
    }
}