using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
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
            // 1. Map Tác giả
            CreateMap<Author, AuthorDto>()
                .ForMember(dest => dest.BookCount, opt => opt.MapFrom(src => src.Books.Count));
            CreateMap<AuthorCreateDto, Author>();

            // 2. Map Thể loại
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.BookCount, opt => opt.MapFrom(src => src.Books.Count));
            CreateMap<CategoryCreateDto, Category>();

            // 3. Map cho Đầu sách (Book) -> BookDto
            CreateMap<Book, BookDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
            CreateMap<BookCreateDto, Book>();

            // 4. Map cho Bản sao (BookCopy) -> BookCopyDto
            CreateMap<BookCopy, BookCopyDto>()
                .ForMember(dest => dest.Status, 
                           opt => opt.MapFrom(src => src.Status.ToFriendlyString()));

            // 5. Map cho Chi tiết mượn (BorrowDetail)
            CreateMap<BorrowDetail, BorrowDetailDto>()
                .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => src.BookCopy.BookId)) 
                .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.BookCopy.Book.Title));

            // 6. Map cho Phiếu mượn
            CreateMap<BorrowRecord, BorrowRecordDto>();
        }
    }
}