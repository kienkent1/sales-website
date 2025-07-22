using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class LoginVM
    {

        [Display(Name = "Hãy nhập tên đăng nhâpj hoặc email")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        public string UserNameOrEmail { get; set; }



        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

    }
}
