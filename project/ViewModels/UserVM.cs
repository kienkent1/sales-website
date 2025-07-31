using System.ComponentModel.DataAnnotations;
using project.Helpers;
namespace project.ViewModels
{
    public class UserVM
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(20, ErrorMessage = "Tài khoản không được quá 20 ký tự")]
        public string MaKh { get; set; } 

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MaxLength(50, ErrorMessage = "Mật khẩu không được quá 20 ký tự")]
        public string MatKhau { get; set; }

        [MaxLength(50, ErrorMessage = "Họ tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "hãy xác nhận giới tính")]
        public bool GioiTinh { get; set; }


        [MaxLength(60, ErrorMessage = "Địa chỉ không được quá 60 ký tự")]
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [MaxLength(24, ErrorMessage = "Số điện thoại không được quá 24 ký tự")]
        [RegularExpression(@"^(\+84|0)(\d{9,15})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Required(ErrorMessage = "Email  không được để trống")]
        public string Email { get; set; }

        public IFormFile? Hinh { get; set; }

        public bool HieuLuc { get; set; }

        public int VaiTro { get; set; }
        public string? RandomKey { get; set; }

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [DataType(DataType.Date)]
        [MinimumAge(14, ErrorMessage = "Bạn phải lớn hơn 14 tuổi để đăng ký.")]
        public DateTime NgaySinh { get; set; }

        public bool? Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public UserVM()
        {
            this.NgaySinh = DateTime.Today;
        }
    }
}
