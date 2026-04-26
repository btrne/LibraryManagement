using AutoMapper;
using Library.API.Entities;
using Library.API.Dtos.Authors.Requests;
using Library.API.Dtos.Authors.Responses;
using Library.API.Dtos.Books.Requests;
using Library.API.Dtos.Books.Responses;
using Library.API.Dtos.Categories.Requests;
using Library.API.Dtos.Categories.Responses;
using Library.API.Dtos.BorrowRecords.Requests;
using Library.API.Dtos.BorrowRecords.Responses;

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