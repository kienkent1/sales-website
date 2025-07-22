using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class ChangePassword
    {
        [Display(Name = "Mật khẩu hiện tại")]
        [MaxLength(50, ErrorMessage = "*Mật khẩu hiện tại không được quá 50 ký tự")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tải để đổi mật khẩu")]
        public string CurrentPassword { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [MaxLength(50, ErrorMessage = "*Mật khẩu mới không được quá 50 ký tự")]
        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(5, ErrorMessage = "Mật khẩu mới phải có ít nhất 5 ký tự")]
        public string NewPassword { get; set; }

        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }


        //public string Username { get; set; }

    }
}
