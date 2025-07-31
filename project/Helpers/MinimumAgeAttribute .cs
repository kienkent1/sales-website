using System.ComponentModel.DataAnnotations;

namespace project.Helpers
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        // Constructor để nhận số tuổi tối thiểu, ví dụ: [MinimumAge(14)]
        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            // Thiết lập thông báo lỗi mặc định nếu người dùng không tự định nghĩa
            ErrorMessage = $"Tuổi phải lớn hơn hoặc bằng {_minimumAge}.";
        }

        // Phương thức cốt lõi để thực hiện việc xác thực
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Kiểm tra xem giá trị có phải là kiểu DateTime không
            if (value is DateTime birthDate)
            {
                // Tính toán ngày mà một người sẽ tròn _minimumAge tuổi.

                var cutoffDate = DateTime.Today.AddYears(-_minimumAge);

                if (birthDate > cutoffDate)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }


            return ValidationResult.Success;
        }
    }
}
