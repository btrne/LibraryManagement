using System.ComponentModel.DataAnnotations;

namespace Library.API.Dtos.Categories.Requests
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Tên thể loại không được để trống")]
        public string Name { get; set; } = string.Empty;
    }
}