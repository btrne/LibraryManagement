using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
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
                    .ThenInclude(d => d.Book)    
                .AsQueryable();

            // Lọc theo số điện thoại (Tuyệt vời để tra cứu lịch sử 1 người)
            if (!string.IsNullOrWhiteSpace(searchDto.PhoneNumber))
                query = query.Where(br => br.PhoneNumber.Contains(searchDto.PhoneNumber));

            // Lọc theo tên người mượn
            if (!string.IsNullOrWhiteSpace(searchDto.BorrowerName))
                query = query.Where(br => br.BorrowerName.Contains(searchDto.BorrowerName));

            // Chỉ lấy các phiếu mượn "Đang hoạt động" (Còn ít nhất 1 cuốn sách chưa trả)
            if (searchDto.IsActive.HasValue && searchDto.IsActive.Value)
                query = query.Where(br => br.BorrowDetails.Any(d => d.ReturnDate == null));

            // Chỉ lấy các phiếu "Quá hạn" (Phục vụ cho chức năng gọi điện đòi sách)
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
            if (createDto.BookIds == null || !createDto.BookIds.Any())
                return BadRequest("Phải có ít nhất 1 cuốn sách.");

            // Kéo toàn bộ lịch sử mượn của SĐT này ra để kiểm tra
            var userHistory = await _context.BorrowRecords
                .Include(r => r.BorrowDetails)
                .Where(r => r.PhoneNumber == createDto.PhoneNumber)
                .ToListAsync();

            // =================================================================
            // LUẬT 1: KIỂM TRA TỔNG SỐ SÁCH ĐANG GIỮ (TỐI ĐA 5 CUỐN)
            // =================================================================
            // Đếm những cuốn sách mà SĐT này mượn nhưng chưa trả (ReturnDate == null)
            int currentHoldingCount = userHistory
                .SelectMany(r => r.BorrowDetails)
                .Count(d => d.ReturnDate == null);

            int availableSlots = MaxBooks - currentHoldingCount;

            // Trường hợp 1: Đã giữ đủ 5 cuốn -> Khóa luôn
            if (availableSlots <= 0)
            {
                return BadRequest($"Từ chối: Bạn đang giữ {currentHoldingCount} cuốn sách (đã đạt giới hạn tối đa). Vui lòng trả bớt sách để có thể mượn thêm.");
            }

            // Trường hợp 2: Số sách muốn mượn đợt này lớn hơn số Slot còn lại
            if (createDto.BookIds.Count > availableSlots)
            {
                return BadRequest($"Từ chối: Bạn hiện đang giữ {currentHoldingCount} cuốn sách. Khung quy định tối đa là {MaxBooks} cuốn, nên bạn chỉ được mượn thêm TỐI ĐA {availableSlots} cuốn nữa.");
            }

            // =================================================================
            // LUẬT 2: CHẶN NẾU ĐANG CÓ SÁCH QUÁ HẠN CHƯA TRẢ
            // =================================================================
            var currentlyOverdue = userHistory.Any(r => 
                r.DueDate < DateTime.Today && 
                r.BorrowDetails.Any(d => d.ReturnDate == null)
            );
            
            if (currentlyOverdue)
                return BadRequest("Từ chối: Bạn đang có sách quá hạn chưa trả. Vui lòng nộp phạt trước khi mượn mới.");

            // =================================================================
            // LUẬT 3: CHẶN VĨNH VIỄN NẾU TÁI PHẠM (TRỄ HẠN TỪ 2 LẦN)
            // =================================================================
            var lateIncidentsCount = userHistory
                .Where(r => r.BorrowDetails.Any(d => d.ReturnDate != null && d.ReturnDate > r.DueDate)) // Đã sửa r.BorrowRecord.DueDate thành r.DueDate
                .Count();

            if (lateIncidentsCount >= 2)
                return BadRequest("Từ chối: Số điện thoại này đã bị khóa vĩnh viễn do tái phạm trả trễ hạn.");

            // =================================================================
            // TẠO PHIẾU MƯỢN MỚI
            // =================================================================
            var borrowDate = DateTime.Today;
            var record = new BorrowRecord
            {
                BorrowerName = createDto.BorrowerName,
                PhoneNumber = createDto.PhoneNumber,
                BorrowDate = borrowDate,
                DueDate = borrowDate.AddDays(BorrowDays)
            };

            foreach (var bookId in createDto.BookIds)
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return BadRequest($"Lỗi: Sách có ID {bookId} không tồn tại.");

                var isAvailable = !await _context.BorrowDetails
                    .AnyAsync(d => d.BookId == bookId && d.ReturnDate == null);
                
                if (!isAvailable) return BadRequest($"Lỗi: Sách '{book.Title}' hiện đang được người khác mượn.");

                record.BorrowDetails.Add(new BorrowDetail
                {
                    BookId = bookId,
                    FineAmount = 0
                });
            }

            _context.BorrowRecords.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Mượn sách thành công!", 
                recordId = record.Id,
                totalBooks = record.BorrowDetails.Count,
                dueDate = record.DueDate.ToString("yyyy-MM-dd") 
            });
        }

        // 3. PUT: Trả sách
        [HttpPut("return")]
        public async Task<IActionResult> ReturnBooks([FromBody] ReturnBookRequestDto request)
        {
            // Bắt đầu truy vấn tìm các sách CHƯA TRẢ
            var query = _context.BorrowDetails
                .Include(d => d.BorrowRecord)
                .Where(d => d.ReturnDate == null);

            // Kiểm tra xem người dùng truyền vào ID Phiếu mượn hay ID Sách
            if (request.BorrowRecordId.HasValue && request.BorrowRecordId > 0)
            {
                // Trả theo Phiếu mượn: Lấy tất cả sách chưa trả thuộc Phiếu này
                query = query.Where(d => d.BorrowRecordId == request.BorrowRecordId);
            }
            else if (request.BookIds != null && request.BookIds.Any())
            {
                // Trả lẻ theo Sách: Kéo những cuốn trùng BookId
                query = query.Where(d => request.BookIds.Contains(d.BookId));
            }
            else
            {
                return BadRequest("Vui lòng cung cấp danh sách ID sách hoặc ID phiếu mượn cần trả.");
            }

            var details = await query.ToListAsync();

            if (details.Count == 0) 
                return NotFound("Không tìm thấy sách đang được mượn hợp lệ hoặc sách đã được trả trước đó.");

            decimal totalFine = 0;
            var returnDate = DateTime.Today;

            foreach (var detail in details)
            {
                detail.ReturnDate = returnDate;

                // Tính tiền phạt cho từng cuốn sách nếu trễ
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
        // 4. DELETE: Xóa phiếu mượn (Chỉ cho phép xóa khi ĐÃ TRẢ HẾT SÁCH)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            // Lấy phiếu mượn kèm theo danh sách chi tiết mượn bên trong
            var record = await _context.BorrowRecords
                .Include(r => r.BorrowDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null) 
                return NotFound("Không tìm thấy phiếu mượn.");

            // RÀNG BUỘC: Kiểm tra xem có cuốn sách nào trong phiếu này chưa trả không
            bool hasUnreturnedBooks = record.BorrowDetails.Any(d => d.ReturnDate == null);
            
            if (hasUnreturnedBooks)
            {
                return BadRequest("Từ chối: Không thể xóa phiếu mượn này vì vẫn còn sách chưa được trả. Vui lòng trả toàn bộ sách trước khi xóa.");
            }

            // Xóa phiếu mượn (EF Core sẽ tự động xóa các BorrowDetail con nhờ Cascade Delete)
            _context.BorrowRecords.Remove(record);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Đã xóa phiếu mượn thành công!" });
        }
    }
}