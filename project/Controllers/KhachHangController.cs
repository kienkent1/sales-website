using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using project.Data;
using project.Helpers;
using project.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace project.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;


        public KhachHangController(Hshop2023Context context, IMapper mapper) {
            db = context;
            _mapper = mapper;
        }
        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
        public IActionResult DangKy(RegisterVM model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                var khachHangTonTai = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == model.MaKh);

                if (khachHangTonTai != null)
                {
                    ModelState.AddModelError("MaKh", "Tên đăng nhập này đã được sử dụng.");

                    return View(model);
                }
                try
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = Helpers.Util.GenerateRandomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;//su ly sau khi gui mail
                    khachHang.VaiTro = 0;

                    if (Hinh != null)
                    {
                        khachHang.Hinh = Util.UploadHinh(Hinh, "KhachHang");
                    }
                    db.Add(khachHang);
                    db.SaveChanges();
                    return RedirectToAction("Index", "hangHoa");
                }
                catch (Exception )
                {

                    ModelState.AddModelError(string.Empty, "Đã có lỗi xảy ra, vui lòng thử lại sau.");
                }

            }
            return View();
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            if (ModelState.IsValid)
            {
                var khachHang = db.KhachHangs.SingleOrDefault(x => x.MaKh == model.UserNameOrEmail || x.Email == model.UserNameOrEmail);
                if (khachHang == null)
                {
                    ModelState.AddModelError("Error", "Tài khoản không tồn tại");
                }
                else
                {
                    if (!khachHang.HieuLuc)
                    {
                        ModelState.AddModelError("Error", "Tài khoản bị khóa vui lòng liện hệ admin");
                    }
                    else {
                        if (khachHang.MatKhau != model.Password.ToMd5Hash(khachHang.RandomKey))
                           // if (khachHang.MatKhau != model.Password)
                        {
                           
                            ModelState.AddModelError("Error", "Sai thông tin đăng nhập"+ model.Password.ToMd5Hash(khachHang.RandomKey));
                        }
                        else {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, khachHang.Email),
                                new Claim(ClaimTypes.Name, khachHang.HoTen),
                                new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
                                //claim-role động 
                                new Claim(ClaimTypes.Role, khachHang.VaiTro.ToString()),
                                new Claim("Avatar", khachHang.Hinh ?? "")
                            };
                            var claimIdentity = new ClaimsIdentity(claims,
                                CookieAuthenticationDefaults.AuthenticationScheme);
                            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                          
                            if (Url.IsLocalUrl(ReturnUrl))
                            {
                                return Redirect(ReturnUrl);
                            }
                            else
                            {
                                return Redirect("/");
                            }
                        }
                    }

                }
            }
            return View();
        }
        #endregion
       
        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

      
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {

            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }


            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {

                return RedirectToAction("DangNhap", "KhachHang");
            }

            var khachHang = db.KhachHangs.SingleOrDefault(x => x.MaKh == username);


            if (khachHang.MatKhau != model.CurrentPassword.ToMd5Hash(khachHang.RandomKey))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
                return View("Profile", model);
            }

            khachHang.RandomKey = Helpers.Util.GenerateRandomKey();
            khachHang.MatKhau = model.NewPassword.ToMd5Hash(khachHang.RandomKey);

            db.Update(khachHang);
            await db.SaveChangesAsync(); 

            await HttpContext.SignOutAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("DangNhap", "KhachHang");
        }
    } 
}

