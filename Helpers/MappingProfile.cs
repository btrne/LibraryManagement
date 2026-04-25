using AutoMapper;
using Library.API.Dtos;
using Library.API.Entities;

namespace Library.API.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Ánh xạ từ Entity sang DTO để trả về cho người dùng
            CreateMap<Book, BookDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.Name : "N/A"))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "N/A"));

            // Ánh xạ từ DTO đầu vào sang Entity để lưu vào DB
            CreateMap<BookCreateDto, Book>();
            // Author mapping
            CreateMap<Author, AuthorDto>();
            CreateMap<AuthorCreateDto, Author>();

            // Category mapping
            CreateMap<Category, CategoryDto>();
            CreateMap<CategoryCreateDto, Category>();

            // BorrowRecord mapping
            CreateMap<BorrowRecord, BorrowRecordDto>()
                .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book.Title));
            CreateMap<BorrowRecordCreateDto, BorrowRecord>();
        }
    }
}