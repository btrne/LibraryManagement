namespace Library.API.Dtos.BorrowRecords.Requests
{
    public class BorrowRecordCreateDto
    {
        public List<string> Barcodes { get; set; } = new List<string>();
        public string BorrowerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}