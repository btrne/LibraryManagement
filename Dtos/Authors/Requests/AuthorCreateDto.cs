using System.ComponentModel.DataAnnotations;

namespace Library.API.Dtos.Authors.Requests
{
    public class AuthorCreateDto
    {
        [Required(ErrorMessage = "Tên tác giả không được để trống")]
        public string Name { get; set; } = string.Empty;
    }
}