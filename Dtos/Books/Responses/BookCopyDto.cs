namespace Library.API.Dtos.Books.Responses
{
    public class BookCopyDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty; 
    }
}