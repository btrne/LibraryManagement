using Microsoft.EntityFrameworkCore;
using Library.API.Entities;

namespace Library.API.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<BorrowDetail> BorrowDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Chặn trùng Tiêu đề sách
            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Title)
                .IsUnique();

            // 2. Chặn trùng Mã vạch của từng cuốn sách
            modelBuilder.Entity<BookCopy>()
                .HasIndex(c => c.Barcode)
                .IsUnique();
        }
    }
}