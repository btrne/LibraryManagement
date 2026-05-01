namespace Library.API.Dtos.Statistics
{
    public class DashboardSummaryDto
    {
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int CurrentlyBorrowed { get; set; }
        public int OverdueRecords { get; set; }
    }

    public class TopBookDto
    {
        public string Title { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }
}