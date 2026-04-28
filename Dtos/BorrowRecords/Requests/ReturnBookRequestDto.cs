namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class ReturnBookRequestDto
    {
        // Danh sách ID sách nếu muốn trả lẻ từng cuốn
        public List<int>? BookIds { get; set; }
        
        // ID phiếu mượn nếu muốn trả tất cả sách trong phiếu đó cùng lúc
        public int? BorrowRecordId { get; set; }
    }
}