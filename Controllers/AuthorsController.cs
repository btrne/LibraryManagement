using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Authors.Requests;
using Library.API.Dtos.Authors.Responses;

namespace Library.API.Controllers
{
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
            // AsQueryable() giúp chúng ta "lắp ráp" câu query trước khi thực sự chạy xuống Database
            var query = _context.Authors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                // LIKE '%keyword%' trong SQL
                query = query.Where(a => a.Name.Contains(searchDto.Keyword));
            }

            var authors = await query.ToListAsync(); // Chạy câu SQL xuống DB
            return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authors));
        }
        // 2. GET: api/authors/5
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
        // 4. PUT: api/authors/5
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
        // 5. DELETE: api/authors/5
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
    }
}