using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.API.Data;
using Library.API.Entities;
using AutoMapper;
using Library.API.Dtos.Books.Requests;
using Library.API.Dtos.Books.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Để dùng IFormFile
using ClosedXML.Excel;
using Library.API.Dtos.Common;

namespace Library.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment = default!;

        public BooksController(LibraryDbContext context, IMapper mapper, IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
        }

        // 1. GET: api/books
        [HttpGet]
        public async Task<ActionResult<PagedResult<BookDto>>> GetBooks([FromQuery] BookSearchDto searchDto)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Copies)
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

            //Sắp xếp và phân trang
            if (!string.IsNullOrWhiteSpace(searchDto.SortBy))
            {
                if (searchDto.SortBy.Equals("Title", StringComparison.OrdinalIgnoreCase))
                {
                    query = searchDto.IsDescending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title);
                }
                else if (searchDto.SortBy.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    query = searchDto.IsDescending ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id);
                }
                else if (searchDto.SortBy.Equals("AuthorName", StringComparison.OrdinalIgnoreCase))
                {
                    query = searchDto.IsDescending ? query.OrderByDescending(b => b.Author.Name) : query.OrderBy(b => b.Author.Name);
                }
                else if (searchDto.SortBy.Equals("CategoryName", StringComparison.OrdinalIgnoreCase))
                {
                    query = searchDto.IsDescending ? query.OrderByDescending(b => b.Category.Name) : query.OrderBy(b => b.Category.Name);
                }
                
            }
            else
            {
                query = query.OrderBy(b => b.Id);
            }

            var totalItems = await query.CountAsync();
            var skipNumber = (searchDto.PageNumber - 1) * searchDto.PageSize;
            var books = await query
                .Skip(skipNumber)
                .Take(searchDto.PageSize)
                .ToListAsync();
            
            var totalPages = (int)Math.Ceiling((double)totalItems / searchDto.PageSize);
    
            var result = new PagedResult<BookDto>
            {
                Items = _mapper.Map<IEnumerable<BookDto>>(books),
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(result);
        }
        // 2. GET: api/book
        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Copies)
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

            // Trả về kết quả kèm thông tin đầy đủ (DTO) để tránh lỗi vòng lặp
            var result = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == book.Id);

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, _mapper.Map<BookDto>(result));
        }

        // 4. PUT: api/book
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, BookCreateDto bookUpdateDto) 
        {
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

        // 5. DELETE: api/book
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // 6. POST: api/books/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            // 1. Kiểm tra file hợp lệ
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng tải lên một file.");

            if (!file.FileName.EndsWith(".xlsx"))
                return BadRequest("Hệ thống chỉ hỗ trợ định dạng file Excel (.xlsx).");

            var errors = new List<string>();
            int addedBooksCount = 0;
            int addedCopiesCount = 0;

            // 2. Đọc file Excel từ bộ nhớ
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // Lấy Sheet đầu tiên
            
            // Lấy tất cả các dòng có dữ liệu, bỏ qua dòng 1 (Header)
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var rowNum = row.RowNumber();
                try
                {
                    // Đọc dữ liệu từng cột
                    string title = row.Cell(1).GetValue<string>()?.Trim() ?? string.Empty;
                    string isbn = row.Cell(2).GetValue<string>()?.Trim() ?? string.Empty;
                    string barcode = row.Cell(5).GetValue<string>()?.Trim() ?? string.Empty;
                    int authorId = row.Cell(3).GetValue<int>();
                    int categoryId = row.Cell(4).GetValue<int>();

                    // Kiểm tra rỗng
                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(barcode))
                    {
                        errors.Add($"Dòng {rowNum}: Bị thiếu Tiêu đề hoặc Mã vạch.");
                        continue;
                    }

                    // 3. Kiểm tra xem Đầu sách đã tồn tại chưa (Dựa vào Isbn hoặc Title)
                    // Dùng .Local.FirstOrDefault để EF Core tự tìm trong bộ nhớ tạm những cuốn vừa được add trong vòng lặp này
                    var book = _context.Books.Local.FirstOrDefault(b => b.Isbn == isbn) 
                               ?? await _context.Books.FirstOrDefaultAsync(b => b.Isbn == isbn || b.Title == title);

                    if (book == null)
                    {
                        // Kiểm tra Author và Category có thực sự tồn tại trong DB không
                        bool isValidFk = await _context.Authors.AnyAsync(a => a.Id == authorId) && 
                                         await _context.Categories.AnyAsync(c => c.Id == categoryId);
                        
                        if (!isValidFk)
                        {
                            errors.Add($"Dòng {rowNum}: AuthorId hoặc CategoryId không tồn tại trong hệ thống.");
                            continue;
                        }

                        // Tạo đầu sách mới
                        book = new Book
                        {
                            Title = title,
                            Isbn = isbn,
                            AuthorId = authorId,
                            CategoryId = categoryId
                        };
                        _context.Books.Add(book);
                        addedBooksCount++;
                    }

                    // 4. Kiểm tra Mã vạch bản sao có bị trùng không
                    bool barcodeExists = _context.BookCopies.Local.Any(c => c.Barcode == barcode) ||
                                         await _context.BookCopies.AnyAsync(c => c.Barcode == barcode);

                    if (barcodeExists)
                    {
                        errors.Add($"Dòng {rowNum}: Mã vạch '{barcode}' đã tồn tại.");
                        continue;
                    }

                    // Tạo bản sao vật lý mới
                    var bookCopy = new BookCopy
                    {
                        Barcode = barcode,
                        Status = CopyStatus.Available,
                        Book = book // Gắn trực tiếp Object Book vào đây thay vì BookId vì Book có thể chưa được lưu (chưa có ID)
                    };
                    
                    _context.BookCopies.Add(bookCopy);
                    addedCopiesCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {rowNum}: Lỗi định dạng dữ liệu ({ex.Message})");
                }
            }

            // 5. Lưu toàn bộ thay đổi vào Database
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Xử lý file Excel hoàn tất.",
                NewBooksCreated = addedBooksCount,
                NewCopiesCreated = addedCopiesCount,
                Errors = errors
            });
        }
        
        // 7. POST: api/books/{id}/upload-image
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            // 1. Kiểm tra file hợp lệ
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn một file ảnh.");
            
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound("Không tìm thấy sách.");

            // 2. Tạo thư mục lưu trữ nếu chưa có (wwwroot/uploads)
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // 3. Tạo tên file duy nhất để không bị trùng
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            // 4. Lưu file vật lý vào thư mục
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Lưu đường dẫn vào Database
            book.ImageUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { imageUrl = book.ImageUrl });
        }
    }
}