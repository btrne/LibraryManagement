using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Categories.Requests;
using Library.API.Dtos.Categories.Responses;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Library.API.Dtos.Common;

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
            var query = _context.Categories.Include(c => c.Books).AsQueryable();
            // 1. Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                query = query.Where(c => c.Name.Contains(searchDto.Keyword));
            }

            var totalItems = await query.CountAsync();

            // 2. Phân trang
            var categories = await query
                .OrderBy(c => c.Name)
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var result = new PagedResult<CategoryDto>
            {
                Items = _mapper.Map<IEnumerable<CategoryDto>>(categories),
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / searchDto.PageSize),
                CurrentPage = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books) 
                .FirstOrDefaultAsync(c => c.Id == id);
                
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
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua dòng Header

            foreach (var row in rows)
            {
                var rowNum = row.RowNumber();
                try
                {
                    string name = row.Cell(1).GetValue<string>()?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(name))
                    {
                        errors.Add($"Dòng {rowNum}: Tên thể loại bị trống.");
                        continue;
                    }

                    // Kiểm tra xem thể loại đã tồn tại chưa
                    bool exists = _context.Categories.Local.Any(c => c.Name.ToLower() == name.ToLower()) ||
                                  await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());

                    if (exists)
                    {
                        errors.Add($"Dòng {rowNum}: Thể loại '{name}' đã tồn tại.");
                        continue;
                    }

                    _context.Categories.Add(new Category { Name = name });
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
                NewCategoriesCreated = addedCount,
                Errors = errors
            });
        }
    }
}