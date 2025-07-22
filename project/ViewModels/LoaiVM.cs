using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class LoaiVM
    {
        public int MaLoai { get; set; }

        [MaxLength(50, ErrorMessage = "Tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "Tên hàng hóa không được để trống")]
        public string TenLoai { get; set; } 

        public string? TenLoaiAlias { get; set; }

     
        public string? MoTa { get; set; }


        [Required(ErrorMessage = "Hình không được để trống")]
        public IFormFile? Hinh { get; set; }

        public bool? Deleted { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}
