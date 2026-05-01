using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Dtos.Statistics;
using Library.API.Dtos.BorrowRecords.Responses;
using AutoMapper;

namespace Library.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public StatisticsController(LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1. GET: api/statistics/summary
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var summary = new DashboardSummaryDto
            {
                TotalBooks = await _context.Books.CountAsync(),
                TotalCopies = await _context.BookCopies.CountAsync(),
                // Đếm số chi tiết phiếu mượn mà chưa có ngày trả
                CurrentlyBorrowed = await _context.BorrowDetails
                    .CountAsync(d => d.ReturnDate == null),
                // Đếm số phiếu mượn mà hạn trả đã qua và còn sách chưa trả
                OverdueRecords = await _context.BorrowRecords
                    .CountAsync(r => r.DueDate < DateTime.Now && r.BorrowDetails.Any(d => d.ReturnDate == null))
            };

            return Ok(summary);
        }

        // 2. GET: api/statistics/top-books
        [HttpGet("top-books")]
        public async Task<ActionResult<IEnumerable<TopBookDto>>> GetTopBooks()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var topBooks = await _context.BorrowDetails
                .Where(d => d.BorrowRecord.BorrowDate.Month == currentMonth && d.BorrowRecord.BorrowDate.Year == currentYear)
                .GroupBy(d => d.BookCopy.Book.Title) // Nhóm theo tên sách
                .Select(g => new TopBookDto
                {
                    Title = g.Key,
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5) // Lấy 5 cuốn nhiều nhất
                .ToListAsync();

            return Ok(topBooks);
        }

        // 3. GET: api/statistics/overdue-readers
        [HttpGet("overdue-readers")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetOverdueReaders()
        {
            var overdueRecords = await _context.BorrowRecords
                .Include(r => r.BorrowDetails)
                    .ThenInclude(d => d.BookCopy)
                        .ThenInclude(c => c.Book)
                .Where(r => r.DueDate < DateTime.Now && r.BorrowDetails.Any(d => d.ReturnDate == null))
                .OrderBy(r => r.DueDate)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<BorrowRecordDto>>(overdueRecords));
        }
    }
}