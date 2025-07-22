using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class NhaCungCapVM
    {
        [Required(ErrorMessage = "Mã nhà cung cấp không được để trống")]
        [MaxLength(50, ErrorMessage = "Mã nhà cung cấp không được quá 50 ký tự")]
        public string MaNcc { get; set; }

        [Required(ErrorMessage = "Tên công ty không được để trống")]
        [MaxLength(50, ErrorMessage = "Tên công ty quá dài")]
        public string TenCongTy { get; set; }

        [Required(ErrorMessage = "Logo không được để trống")]
        public IFormFile Logo { get; set; }

        [MaxLength(50, ErrorMessage = "Tên liên lạc không được quá 50 ký tự")]
        public string? NguoiLienLac { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "*Email không hợp lệ")]
        public string Email { get; set; }


        [RegularExpression(@"^(\+84|0)(\d{9,15})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? DienThoai { get; set; }

        [MaxLength(100, ErrorMessage = "Địa chỉ không được quá 100 ký tự")]
        public string? DiaChi { get; set; }

        public string? MoTa { get; set; }

        public bool? Deleted { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}
