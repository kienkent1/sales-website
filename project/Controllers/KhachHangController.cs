using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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
                var khachHang = db.KhachHangs.SingleOrDefault(x => x.MaKh == model.Username);
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
                        //if(khachHang.MatKhau != model.Password.ToMd5Hash(khachHang.RandomKey))
                        if (khachHang.MatKhau != model.Password)
                        {
                            ModelState.AddModelError("Error", "Sai thông tin đăng nhập");
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

    } 
}

