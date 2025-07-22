using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class RegisterVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage ="Tên đăng nhập không được để trống")]
        [MaxLength(20, ErrorMessage = "Tài khoản không được quá 20 ký tự")]
        public string MaKh { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MaxLength(50, ErrorMessage = "Mật khẩu không được quá 20 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [MaxLength(50, ErrorMessage = "Họ tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "hãy xác nhận giới tính")]
        public bool GioiTinh { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }


        [MaxLength(60, ErrorMessage = "Địa chỉ không được quá 60 ký tự")]
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [MaxLength(24, ErrorMessage = "Số điện thoại không được quá 24 ký tự")]
        [RegularExpression(@"^(\+84|0)(\d{9,15})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Điện thoại")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Required(ErrorMessage = "Email  không được để trống")]
        public string Email { get; set; } = null!;

        [Display(Name = "Avatar")]
        public string? Hinh { get; set; }
    }
}
