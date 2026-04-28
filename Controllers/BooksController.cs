using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Books.Requests;
using Library.API.Dtos.Books.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Library.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public BooksController(LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1. GET: api/books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks([FromQuery] BookSearchDto searchDto)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .AsQueryable();

            // 1. Lọc theo từ khóa (Tên sách)
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
                query = query.Where(b => b.Title.Contains(searchDto.Keyword));

            // 2. Lọc theo Tác giả
            if (searchDto.AuthorId.HasValue)
                query = query.Where(b => b.AuthorId == searchDto.AuthorId.Value);

            // 3. Lọc theo Thể loại
            if (searchDto.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == searchDto.CategoryId.Value);

            var books = await query.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }
        // 2. GET: api/books/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            return Ok(_mapper.Map<BookDto>(book));
        }

        // 3. POST: api/books
        [HttpPost]
        public async Task<ActionResult> PostBook(BookCreateDto bookCreateDto)
        {
            // 1. Kiểm tra trùng tiêu đề hoặc ISBN để tránh dữ liệu rác
            if (await _context.Books.AnyAsync(b => b.Title == bookCreateDto.Title || b.Isbn == bookCreateDto.Isbn))
            {
                return BadRequest("Sách hoặc mã ISBN này đã tồn tại.");
            }

            // 2. Xử lý Logic Author (Tác giả)
            int finalAuthorId;
            if (bookCreateDto.AuthorId.HasValue && bookCreateDto.AuthorId > 0)
            {
                finalAuthorId = bookCreateDto.AuthorId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(bookCreateDto.AuthorName))
            {
                var existingAuthor = await _context.Authors
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == bookCreateDto.AuthorName.ToLower());
                
                if (existingAuthor != null) {
                    finalAuthorId = existingAuthor.Id;
                } else {
                    var newAuthor = new Author { Name = bookCreateDto.AuthorName };
                    _context.Authors.Add(newAuthor);
                    await _context.SaveChangesAsync();
                    finalAuthorId = newAuthor.Id;
                }
            }
            else return BadRequest("Phải có AuthorId hoặc AuthorName.");

            // 3. Xử lý Logic Category (Thể loại)
            int finalCategoryId;
            if (bookCreateDto.CategoryId.HasValue && bookCreateDto.CategoryId > 0)
            {
                finalCategoryId = bookCreateDto.CategoryId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(bookCreateDto.CategoryName))
            {
                var existingCat = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == bookCreateDto.CategoryName.ToLower());
                
                if (existingCat != null) {
                    finalCategoryId = existingCat.Id;
                } else {
                    var newCat = new Category { Name = bookCreateDto.CategoryName };
                    _context.Categories.Add(newCat);
                    await _context.SaveChangesAsync();
                    finalCategoryId = newCat.Id;
                }
            }
            else return BadRequest("Phải có CategoryId hoặc CategoryName.");

            // 4. Lưu Sách với các ID đã xác định
            var book = _mapper.Map<Book>(bookCreateDto);
            book.AuthorId = finalAuthorId;
            book.CategoryId = finalCategoryId;

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Trả về kết quả kèm thông tin đầy đủ (DTO) để tránh lỗi vòng lặp [cite: 6]
            var result = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == book.Id);

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, _mapper.Map<BookDto>(result));
        }

        // 4. PUT: api/books/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, BookCreateDto bookUpdateDto) 
        {
            // Trong thực tế, Put thường dùng chung BookCreateDto hoặc tạo BookUpdateDto riêng
            var bookInDb = await _context.Books.FindAsync(id);
            if (bookInDb == null) return NotFound();

            // AutoMapper sẽ tự đổ dữ liệu từ DTO vào Entity đã có sẵn trong DB
            _mapper.Map(bookUpdateDto, bookInDb);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Books.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/books/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}