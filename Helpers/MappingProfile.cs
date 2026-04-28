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
            CreateMap<Author, AuthorDto>();
            CreateMap<AuthorCreateDto, Author>();

            CreateMap<Category, CategoryDto>();
            CreateMap<CategoryCreateDto, Category>();

            CreateMap<Book, BookDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
            CreateMap<BookCreateDto, Book>();

            // Map cho BorrowDetail (Lấy Title từ bảng Book)
            CreateMap<BorrowDetail, BorrowDetailDto>()
                .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book.Title));
                
            // Map cho BorrowRecord
            CreateMap<BorrowRecord, BorrowRecordDto>();
        }
    }
}