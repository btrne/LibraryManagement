using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Categories.Requests;
using Library.API.Dtos.Categories.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Library.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public CategoriesController(LibraryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories([FromQuery] CategorySearchDto searchDto)
        {
            var query = _context.Categories.AsQueryable();

            // Nếu người dùng có nhập từ khóa, tiến hành lọc theo tên Thể loại
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                query = query.Where(c => c.Name.Contains(searchDto.Keyword));
            }

            var categories = await query.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<CategoryDto>>(categories));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return Ok(_mapper.Map<CategoryDto>(category));
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> PostCategory(CategoryCreateDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, _mapper.Map<CategoryDto>(category));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Kiểm tra xem có sách nào thuộc thể loại này không trước khi xóa
            var hasBooks = await _context.Books.AnyAsync(b => b.CategoryId == id);
            if (hasBooks) return BadRequest("Không thể xóa thể loại đang có sách.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}