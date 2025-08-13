using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging.Signing;
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
        private readonly Util _util;

        public KhachHangController(Hshop2023Context context, IMapper mapper, Util util) {
            db = context;
            _mapper = mapper;
            _util = util;
        }
        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterVM model, IFormFile? Hinh)
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

                    if (Hinh != null )
                    {
                        khachHang.Hinh =await _util.UploadImage(Hinh, "KhachHang");
                    }
                    db.Add(khachHang);
                    db.SaveChanges();
                    return RedirectToAction("Index", "Home");
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
                           
                            ModelState.AddModelError("Error", "Sai thông tin đăng nhập");
                        }
                        else {var claims = new List<Claim>
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

        #region login by google
        // Bước 1: Action này sẽ khởi động quá trình đăng nhập với Google
        [HttpGet]
        public IActionResult loginByGoogle(string returnUrl = "/")
        {
            // URL mà Google sẽ chuyển hướng về sau khi xác thực thành công.
            // URL này chính là action xử lý callback mà chúng ta sẽ tạo ở dưới.
            string redirectUrl = Url.Action("ExternalLoginCallback", "KhachHang", new { ReturnUrl = returnUrl });

            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Challenge sẽ chuyển hướng người dùng đến Google
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Bước 2: Action này xử lý thông tin Google trả về
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            // Lấy thông tin đăng nhập từ nhà cung cấp bên ngoài (Google)
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                // Xử lý khi xác thực thất bại
                TempData["ErrorMessage"] = "Lỗi xác thực với Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(DangNhap));
            }

            // Lấy các claims (thông tin) từ Google
            var claims = authenticateResult.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var hoTen = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var pictureUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            // Lấy ngày sinh (dưới dạng chuỗi)
            var birthdayString = claims.FirstOrDefault(c => c.Type == "birthday")?.Value;


            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Google.";
                return RedirectToAction(nameof(DangNhap));
            }

            // Kiểm tra xem người dùng đã tồn tại trong DB của bạn chưa (dựa vào email)
            var khachHang = await db.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == email);

            if (khachHang == null)
            {
                string localImagePath = await _util.DownloadAndSaveImageAsync(pictureUrl, "KhachHang");
                DateTime? ngaySinh = null;
                // Google trả về ngày sinh dạng chuỗi "YYYY-MM-DD". Cần phải parse nó.
                if (!string.IsNullOrEmpty(birthdayString))
                {
                    // Dùng TryParse để xử lý an toàn nếu định dạng không đúng
                    if (DateTime.TryParse(birthdayString, out DateTime parsedDate))
                    {
                        ngaySinh = parsedDate;
                    }
                }
                khachHang = new KhachHang
                {
                    // Tạo MaKh ngẫu nhiên hoặc dựa trên email để không bị trùng
                    MaKh = $"GG_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    HoTen = hoTen,
                    Email = email,
                    HieuLuc = true, // Kích hoạt luôn
                    VaiTro = 0, // Vai trò khách hàng
                    GioiTinh = true,
                    NgaySinh = ngaySinh ?? default(DateTime),
                    Hinh = localImagePath
                };
                db.KhachHangs.Add(khachHang);
                await db.SaveChangesAsync();


            }

            // Tạo các claims cho hệ thống của bạn (giống hệt như trong action DangNhap)
            var localClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, khachHang.Email),
        new Claim(ClaimTypes.Name, khachHang.HoTen),
        new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
        new Claim(ClaimTypes.Role, khachHang.VaiTro.ToString()),
        new Claim("Avatar", khachHang.Hinh ?? "")
    };

            var claimsIdentity = new ClaimsIdentity(localClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Đăng nhập người dùng vào hệ thống của bạn (tạo cookie)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Xóa cookie đăng nhập tạm thời của Google
            //await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);


            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/");
            }
        }
        #endregion

        [Authorize]
        public IActionResult Profile()
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            bool IsGGAccount = customerId.StartsWith("GG_");
            ViewBag.IsGGAccount = IsGGAccount;
            return View();
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

        [Authorize(Roles = "2")]
        public IActionResult QuanLyTaiKhoan(string? query)
        {
            List<KhachHang> Users;
            if (string.IsNullOrWhiteSpace(query))
            {
                Users = db.KhachHangs
               .Where(p => p.Deleted != true)
               .OrderBy(p => p.MaKh)
               .ToList();
            }
            else
            {
                Users = db.KhachHangs
     .Where(p => p.Deleted != true && p.MaKh != null)
     .Where(p => p.MaKh.Contains(query.ToLower()) || p.HoTen.Contains(query.ToLower()))
     .OrderBy(p => p.MaKh)
     .ToList();

                if (Users.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy loại nào với từ khóa tìm kiếm.";
                }
            }
            var ListUsers = db.KhachHangs.Where(p => p.Deleted == true).Count();
            ViewBag.CountUsersDeleted = ListUsers;
            ViewBag.Users = Users;

            var viewModel = new UserVM();
            ViewBag.CurrentQuery = query;
            return View(viewModel);
        }
        public IActionResult GetTK(string id)
        {
            var TaiKhoan = db.KhachHangs.AsNoTracking().FirstOrDefault(p => p.MaKh == id);
            if (TaiKhoan == null)
            {
                return NotFound();
            }

            var TKData = new
            {
                MaKh = TaiKhoan.MaKh,
                Email = TaiKhoan.Email,
                HoTen = TaiKhoan.HoTen,
                MatKhau = TaiKhoan.MatKhau,
                GioiTinh = TaiKhoan.GioiTinh,
                NgaySinh = TaiKhoan.NgaySinh != default(DateTime) ? TaiKhoan.NgaySinh.ToString("yyyy-MM-dd") : null,
                DiaChi = TaiKhoan.DiaChi,
               DienThoai= TaiKhoan.DienThoai,
                Hinh = TaiKhoan.Hinh,
                HieuLuc = TaiKhoan.HieuLuc,
                VaiTro = TaiKhoan.VaiTro
            };
            return Ok(TKData);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteSorftUser(string id)
        {
            var ngdung = await db.KhachHangs.FindAsync(id);
            if (ngdung == null)
            {
                return NotFound();
            }
            try
            {

                ngdung.Deleted = true; 
                ngdung.DeletedAt = DateTime.Now; 
                ngdung.HieuLuc = false; 

                db.KhachHangs.Update(ngdung);
                await db.SaveChangesAsync();
                return Ok(new { success = true, message = "Tài khoản đã được xóa thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa tài khoản." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEditTK(UserVM model)
        {
            // 1. Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng kiểm tra lại các thông tin đã nhập.";
                // Phải tải lại danh sách người dùng khi trả về view
                ViewBag.Users = db.KhachHangs.Where(p => p.Deleted != true).ToList();
                return View("QuanLyTaiKhoan", model);
            }

            try
            {
                var existingUser = await db.KhachHangs.FindAsync(model.MaKh);
                // LOGIC THÊM MỚI
                if (existingUser==null )
                {

                    var newUser = _mapper.Map<KhachHang>(model);


                    newUser.RandomKey = Helpers.Util.GenerateRandomKey();
                    newUser.MatKhau = model.MatKhau.ToMd5Hash(newUser.RandomKey);

                    if (model.Hinh != null)
                    {
                        newUser.Hinh = await _util.UploadImage(model.Hinh, "KhachHang");
                    }
                    newUser.Deleted = false;

                    db.KhachHangs.Add(newUser);
                    TempData["SuccessMessage"] = $"Thêm tài khoản '{newUser.MaKh}' thành công!";
                }
                // LOGIC CẬP NHẬT
                else
                {
                    var userToUpdate = await db.KhachHangs.FindAsync(model.MaKh);
                    if (userToUpdate == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy tài khoản để cập nhật.";
                        return RedirectToAction(nameof(QuanLyTaiKhoan));
                    }
                    if (userToUpdate.VaiTro == 2)
                    {
                                                TempData["ErrorMessage"] = "Không thể cập nhật tài khoản quản trị viên.";
                        return RedirectToAction(nameof(QuanLyTaiKhoan));
                    }
                    // --- Logic xử lý mật khẩu khi cập nhật ---
                    _mapper.Map(model, userToUpdate);
                    if (model.MatKhau != userToUpdate.MatKhau)
                    {

                        // Tạo RandomKey MỚI và hash mật khẩu MỚI với key MỚI đó
                        userToUpdate.RandomKey = Helpers.Util.GenerateRandomKey();
                        userToUpdate.MatKhau = model.MatKhau.ToMd5Hash(userToUpdate.RandomKey);
                    }
                    // Nếu model.MatKhau == userToUpdate.MatKhau, nghĩa là admin không đổi pass

                    if (model.Hinh != null)
                    {
                        userToUpdate.Hinh = await _util.UploadImage(model.Hinh, "KhachHang");
           
                    }

                    db.Update(userToUpdate);
                    TempData["SuccessMessage"] = $"Cập nhật tài khoản '{userToUpdate.MaKh}' thành công!";
                }

                await db.SaveChangesAsync();
                return RedirectToAction(nameof(QuanLyTaiKhoan)); // Redirect về trang quản lý
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (quan trọng cho môi trường production)
                // logger.LogError(ex, "Lỗi trong quá trình AddOrEditTK"); 
                ViewBag.ErrorMessage = "Đã có lỗi nghiêm trọng xảy ra. Vui lòng thử lại sau.";
                ViewBag.Users = db.KhachHangs.Where(p => p.Deleted != true).ToList();
                return View("QuanLyTaiKhoan", model);
            }
        }

        [Authorize(Roles = "2")]
        public IActionResult GabageUsers(string? query)
        {
            List<KhachHang> Users;
            if (string.IsNullOrWhiteSpace(query))
            {
                Users = db.KhachHangs
               .Where(p => p.Deleted == true)
               .OrderBy(p => p.MaKh)
               .ToList();
            }
            else
            {
                Users = db.KhachHangs
     .Where(p => p.Deleted != true && p.MaKh == null)
     .Where(p => p.MaKh.Contains(query.ToLower()) || p.HoTen.Contains(query.ToLower()))
     .OrderBy(p => p.MaKh)
     .ToList();

                if (Users.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy loại nào với từ khóa tìm kiếm.";
                }
            }

            ViewBag.DeletedUsers = Users;


            return View();

        }

        [Authorize(Roles = "2")]
        [HttpPost("~/KhachHang/RestoreUser")]
        public async Task<IActionResult> RestoreUser([FromBody] List<string> id)
        {
            try { 
           

                if (id == null || !id.Any())
                {
                    return BadRequest(new { success = false, message = "Vui lòng cung cấp danh sách ID sản phẩm để hoàn tác." });
                }


                var users = await db.KhachHangs
                                                .Where(p => id.Contains(p.MaKh))
                                                .ExecuteUpdateAsync(p => p
                                                    .SetProperty(h => h.Deleted, false)
                                                    .SetProperty(h => h.DeletedAt, (DateTime?)null)
                                                .SetProperty(h=>h.HieuLuc, true));
                return Ok(new
                {
                    success = true,
                    message = $"Đã hoàn tác thành công {users} sản phẩm."
                });
            }
            catch (Exception ex)
            {
                 StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi khôi phục tài khoản." });
              return  RedirectToAction(nameof(GabageUsers));
            }
        }
    } 
}

