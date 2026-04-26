using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.BorrowRecords.Requests;
using Library.API.Dtos.BorrowRecords.Responses;

namespace Library.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public BorrowRecordsController(LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1. GET: Lấy danh sách phiếu mượn
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetBorrowRecords()
        {
            var records = await _context.BorrowRecords
                .Include(br => br.Book)
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<BorrowRecordDto>>(records));
        }

        // 2. POST: Tạo phiếu mượn mới
        [HttpPost]
        public async Task<ActionResult<BorrowRecordDto>> PostBorrowRecord([FromBody] BorrowRecordCreateDto createDto)
        {
            var book = await _context.Books.FindAsync(createDto.BookId);
            if (book == null) return BadRequest("Sách không tồn tại.");

            var record = _mapper.Map<BorrowRecord>(createDto);
            
            if (record.BorrowDate == default) record.BorrowDate = DateTime.Now;

            _context.BorrowRecords.Add(record);
            await _context.SaveChangesAsync();

            await _context.Entry(record).Reference(r => r.Book).LoadAsync();

            var resultDto = _mapper.Map<BorrowRecordDto>(record);
            return Ok(resultDto);
        }

        // 3. PUT: Trả sách (Cập nhật ngày trả)
        // Đường dẫn: api/borrowrecords/{id}/return
        [HttpPut("{id}/return")]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            
            if (record == null) return NotFound("Không tìm thấy bản ghi mượn sách.");

            // Nếu đã có ngày trả rồi thì không cần cập nhật lại
            if (record.ReturnDate != null) return BadRequest("Sách này đã được trả trước đó.");

            record.ReturnDate = DateTime.Now; // Ghi nhận ngày trả là hiện tại
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Trả sách thành công!", returnDate = record.ReturnDate });
        }

        // 4. DELETE: Xóa phiếu mượn (Có ràng buộc)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            if (record == null) return NotFound();

            // RÀNG BUỘC: Nếu sách chưa có ngày trả (ReturnDate là null), không cho xóa 
            if (record.ReturnDate == null)
            {
                return BadRequest("Không thể xóa bản ghi vì sách chưa được trả. Vui lòng thực hiện trả sách trước.");
            }

            _context.BorrowRecords.Remove(record);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}