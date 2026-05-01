using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Helpers;
using Library.API.Dtos.BorrowRecords.Requests;
using Library.API.Dtos.BorrowRecords.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Library.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;
        
        private const int MaxBooks = 5;
        private const int BorrowDays = 30;
        private const decimal FinePerDay = 5000;

        public BorrowRecordsController(LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1. GET: Lấy tất cả phiếu mượn (Có hỗ trợ lọc)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetBorrowRecords([FromQuery] BorrowRecordSearchDto searchDto)
        {
            var query = _context.BorrowRecords
                .Include(br => br.BorrowDetails) 
                    .ThenInclude(d => d.BookCopy)
                        .ThenInclude(c => c.Book)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchDto.PhoneNumber))
                query = query.Where(br => br.PhoneNumber.Contains(searchDto.PhoneNumber));

            if (!string.IsNullOrWhiteSpace(searchDto.BorrowerName))
                query = query.Where(br => br.BorrowerName.Contains(searchDto.BorrowerName));

            if (searchDto.IsActive.HasValue && searchDto.IsActive.Value)
                query = query.Where(br => br.BorrowDetails.Any(d => d.ReturnDate == null));

            if (searchDto.IsOverdue.HasValue && searchDto.IsOverdue.Value)
            {
                query = query.Where(br => 
                    br.DueDate < DateTime.Today && 
                    br.BorrowDetails.Any(d => d.ReturnDate == null));
            }

            var records = await query.OrderByDescending(r => r.BorrowDate).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<BorrowRecordDto>>(records));
        }

        // 2. POST: Tạo phiếu mượn mới
        [HttpPost]
        public async Task<IActionResult> BorrowBooks([FromBody] BorrowRecordCreateDto createDto)
        {
            if (createDto.Barcodes == null || !createDto.Barcodes.Any())
                return BadRequest("Phải quét ít nhất 1 mã vạch cuốn sách.");

            var userHistory = await _context.BorrowRecords
                .Include(r => r.BorrowDetails)
                .Where(r => r.PhoneNumber == createDto.PhoneNumber)
                .ToListAsync();

            //Tìm BookCopy dựa trên mã Barcode
            var requestedCopies = await _context.BookCopies
                .Include(c => c.Book)
                .Where(c => createDto.Barcodes.Contains(c.Barcode))
                .ToListAsync();

            if (requestedCopies.Count != createDto.Barcodes.Count)
                return NotFound("Một hoặc nhiều mã vạch sách không tồn tại trong hệ thống.");

            // Kiểm tra các luật chặn (Validation)
            var validationError = ValidateBorrowRules(userHistory, createDto.Barcodes.Count);
            if (validationError != null) return BadRequest(validationError);

            foreach (var copy in requestedCopies)
            {
                if (copy.Status != CopyStatus.Available)
                {
                    return BadRequest($"Cuốn sách '{copy.Book?.Title}' (Mã vạch: {copy.Barcode}) hiện không sẵn sàng để mượn (Trạng thái: {copy.Status.ToFriendlyString()}).");
                }
            }

            // Tạo phiếu mượn
            var record = new BorrowRecord
            {
                BorrowerName = createDto.BorrowerName,
                PhoneNumber = createDto.PhoneNumber,
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(BorrowDays),
                BorrowDetails = new List<BorrowDetail>()
            };

            // Thêm chi tiết và cập nhật Status của sách vật lý
            foreach (var copy in requestedCopies)
            {
                copy.Status = CopyStatus.Borrowed; // Đổi trạng thái sang Đang mượn

                record.BorrowDetails.Add(new BorrowDetail
                {
                    BookCopyId = copy.Id,
                    FineAmount = 0
                });
            }

            _context.BorrowRecords.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Mượn sách thành công!", 
                recordId = record.Id,
                dueDate = record.DueDate.ToString("yyyy-MM-dd") 
            });
        }

        // Hàm hỗ trợ tách biệt logic kiểm tra
        private string? ValidateBorrowRules(List<BorrowRecord> history, int requestCount)
        {
            bool hasOverdue = history.Any(r => r.DueDate < DateTime.Today && r.BorrowDetails.Any(d => d.ReturnDate == null));
            if (hasOverdue) return "Từ chối: Bạn đang có sách quá hạn chưa trả.";

            int lateCount = history.Count(r => r.BorrowDetails.Any(d => d.ReturnDate > r.DueDate));
            if (lateCount >= 2) return "Từ chối: Số điện thoại này đã bị khóa do vi phạm trả trễ nhiều lần.";

            int currentHolding = history.SelectMany(r => r.BorrowDetails).Count(d => d.ReturnDate == null);
            if (currentHolding + requestCount > MaxBooks)
                return $"Từ chối: Bạn đang giữ {currentHolding} cuốn. Bạn chỉ có thể mượn thêm tối đa {MaxBooks - currentHolding} cuốn.";

            return null;
        }

        // 3. PUT: Trả sách
        [HttpPut("return")]
        public async Task<IActionResult> ReturnBooks([FromBody] ReturnBookRequestDto request)
        {
            var query = _context.BorrowDetails
                .Include(d => d.BorrowRecord)
                .Include(d => d.BookCopy)
                .Where(d => d.ReturnDate == null);

            if (request.BorrowRecordId.HasValue && request.BorrowRecordId > 0)
            {
                query = query.Where(d => d.BorrowRecordId == request.BorrowRecordId);
            }
            else if (request.Barcodes != null && request.Barcodes.Any())
            {
                query = query.Where(d => request.Barcodes.Contains(d.BookCopy.Barcode));
            }
            else
            {
                return BadRequest("Vui lòng cung cấp danh sách mã vạch hoặc ID phiếu mượn cần trả.");
            }

            var details = await query.ToListAsync();

            if (details.Count == 0) 
                return NotFound("Không tìm thấy sách đang được mượn hợp lệ hoặc sách đã được trả trước đó.");

            decimal totalFine = 0;
            var returnDate = DateTime.Today;

            foreach (var detail in details)
            {
                detail.ReturnDate = returnDate;
                
                //Trả lại trạng thái Sẵn có cho cuốn sách đó
                if (detail.BookCopy != null)
                {
                    detail.BookCopy.Status = CopyStatus.Available;
                }

                // Tính tiền phạt
                if (returnDate > detail.BorrowRecord.DueDate)
                {
                    int daysLate = (returnDate - detail.BorrowRecord.DueDate).Days;
                    if (daysLate > 0)
                    {
                        detail.FineAmount = daysLate * FinePerDay;
                        totalFine += detail.FineAmount;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Đã trả thành công {details.Count} cuốn sách.",
                returnDate = returnDate.ToString("yyyy-MM-dd"),
                totalFineAmount = totalFine,
                note = totalFine > 0 ? $"Bạn đã trễ hạn, tổng tiền phạt là {totalFine:N0} VNĐ" : "Trả đúng hạn, cảm ơn bạn!"
            });
        }

        // 4. DELETE: Xóa phiếu mượn
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var record = await _context.BorrowRecords
                .Include(r => r.BorrowDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null) 
                return NotFound("Không tìm thấy phiếu mượn.");

            bool hasUnreturnedBooks = record.BorrowDetails.Any(d => d.ReturnDate == null);
            
            if (hasUnreturnedBooks)
            {
                return BadRequest("Từ chối: Không thể xóa phiếu mượn này vì vẫn còn sách chưa được trả. Vui lòng trả toàn bộ sách trước khi xóa.");
            }

            _context.BorrowRecords.Remove(record);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Đã xóa phiếu mượn thành công!" });
        }
    }
}