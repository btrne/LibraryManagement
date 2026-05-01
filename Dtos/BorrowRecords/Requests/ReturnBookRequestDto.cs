namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class ReturnBookRequestDto
    {
        // Danh sách mã vạch nếu muốn trả lẻ từng cuốn
        public List<string>? Barcodes { get; set; }

        // ID phiếu mượn nếu muốn trả tất cả sách trong phiếu đó cùng lúc
        public int? BorrowRecordId { get; set; }
    }
}