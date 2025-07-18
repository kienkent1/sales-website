using project.Data;
using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class ContactVM : IValidatableObject
    {
        [MaxLength(50, ErrorMessage = "*Tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "*Tên không được để trống")]
        public  string HoTen { get; set; }

        [Required(ErrorMessage = "*Nội dung không được để trống")]
        public  string NoiDung { get; set; } 

        public DateOnly NgayGy { get; set; }

        [MaxLength(50, ErrorMessage = "*Email không được quá 50 ký tự")]
        [EmailAddress(ErrorMessage = "*Email không hợp lệ")]
        public string? Email { get; set; }

        [MaxLength(15, ErrorMessage = "*Số điện thoại không được quá 15 ký tự")]
        [RegularExpression(@"^(\+84|0)(\d{9,15})$", ErrorMessage = "*Số điện thoại không hợp lệ")]
        public string? DienThoai { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra nếu cả Email và Điện thoại đều rỗng hoặc chỉ có khoảng trắng
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(DienThoai))
            {
                // Nếu cả hai đều rỗng, tạo ra một lỗi validation
                yield return new ValidationResult(
                    "*Bạn phải nhập Email hoặc Số điện thoại.",
                    // Báo lỗi này liên quan đến cả hai trường
                    new[] { nameof(Email), nameof(DienThoai) }
                );
            }
        }
    }

}
