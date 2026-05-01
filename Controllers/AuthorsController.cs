using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Authors.Requests;
using Library.API.Dtos.Authors.Responses;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Library.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public AuthorsController (LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        // 1. GET: api/authors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors([FromQuery] AuthorSearchDto searchDto)
        {
            var query = _context.Authors.Include(a => a.Books).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                // LIKE '%keyword%' trong SQL
                query = query.Where(a => a.Name.Contains(searchDto.Keyword));
            }

            var authors = await query.ToListAsync(); // Chạy câu SQL xuống DB
            return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authors));
        }
        // 2. GET: api/author
        [HttpGet("{id}")]
        public async Task<ActionResult<AuthorDto>> GetAuthor(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (author == null) return NotFound();
            return Ok(_mapper.Map<AuthorDto>(author));
        }
        // 3. POST: api/authors
        [HttpPost]
        public async Task<ActionResult<AuthorDto>> PostAuthor(AuthorCreateDto authorCreateDto)
        {
            // Kiểm tra trùng tên tác giả
            var isExist = await _context.Authors.AnyAsync(a => a.Name == authorCreateDto.Name);
            if (isExist) return BadRequest("Tên tác giả này đã tồn tại.");

            var author = _mapper.Map<Author>(authorCreateDto);

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<AuthorDto>(author);

            return CreatedAtAction(nameof(GetAuthor), new { id = author.Id }, resultDto);
        }
        // 4. PUT: api/author
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuthor(int id, AuthorCreateDto authorUpdateDto)
        {
            var authorInDb = await _context.Authors.FindAsync(id);
            if (authorInDb == null) return NotFound();

            _mapper.Map(authorUpdateDto, authorInDb);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Authors.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }
        // 5. DELETE: api/author
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            var authorInDb = await _context.Authors.FindAsync(id);
            if (authorInDb == null) return NotFound();

            var hasBooks = await _context.Books.AnyAsync(b => b.AuthorId == id);
            if (hasBooks) return BadRequest("Không thể xóa tác giả này vì vẫn còn sách liên quan.");

            _context.Authors.Remove(authorInDb);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // 6. POST: api/authors/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng tải lên một file.");

            if (!file.FileName.EndsWith(".xlsx"))
                return BadRequest("Hệ thống chỉ hỗ trợ định dạng file Excel (.xlsx).");

            int addedCount = 0;
            var errors = new List<string>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); 

            foreach (var row in rows)
            {
                var rowNum = row.RowNumber();
                try
                {
                    string name = row.Cell(1).GetValue<string>()?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(name))
                    {
                        errors.Add($"Dòng {rowNum}: Tên tác giả bị trống.");
                        continue;
                    }

                    // Kiểm tra xem tác giả đã tồn tại chưa
                    bool exists = _context.Authors.Local.Any(a => a.Name.ToLower() == name.ToLower()) ||
                                  await _context.Authors.AnyAsync(a => a.Name.ToLower() == name.ToLower());

                    if (exists)
                    {
                        errors.Add($"Dòng {rowNum}: Tác giả '{name}' đã tồn tại.");
                        continue;
                    }

                    _context.Authors.Add(new Author { Name = name });
                    addedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {rowNum}: Lỗi xử lý ({ex.Message})");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Xử lý file Excel hoàn tất.",
                NewAuthorsCreated = addedCount,
                Errors = errors
            });
        }
    }
}