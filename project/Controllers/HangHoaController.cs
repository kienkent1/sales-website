using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using project.Data;
using project.ViewModels;
using System.Text.RegularExpressions;

namespace project.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public HangHoaController(Hshop2023Context context, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            db = context;
            _mapper=mapper;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int? loai, string? NCC, int pageNumber = 1)
        {
           
            // 1. Định nghĩa số lượng item trên mỗi trang
            int pageSize = 12;
            // 2. Bắt đầu xây dựng câu truy vấn (chưa thực thi)
            IQueryable<HangHoa> hangHoas = db.HangHoas.AsQueryable();

     
            hangHoas = hangHoas.Where(hh => hh.IsDeleted != true);

            // 3. Áp dụng bộ lọc NẾU CÓ. Câu truy vấn vẫn chưa được thực thi.
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(hh => hh.MaLoai == loai.Value);
            }
            if (NCC != null)
            {
                hangHoas = hangHoas.Where(hh => hh.MaNcc == NCC);
            }
            // 4. Đếm tổng số item SAU KHI đã lọc để tính số trang chính xác.
            // Câu truy vấn COUNT(*) sẽ được gửi đến DB ở đây.
            var totalItems = await hangHoas.CountAsync();

            // 5. Tính toán tổng số trang
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var result = await hangHoas
                .Where(hh => hh.IsDeleted != true)
      .OrderBy(p => p.TenHh)
      .Skip((pageNumber - 1) * pageSize)
      .Take(pageSize)
      .Select(p => new HangHoaVM
      {
          MaHangHoa = p.MaHh,
          TenHangHoa = p.TenHh,
          HinhAnh = p.Hinh ?? "",
          DonGia = p.DonGia ?? 0,
          MoTaNgan = p.MoTaDonVi ?? "",
          TenLoai = p.MaLoaiNavigation.TenLoai
      })
      .ToListAsync();


            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["CurrentLoai"] = loai;
            return View(result);
        }
        
        public async Task<IActionResult> Search(string query, int pageNumber = 1)
        {
            int pageSize = 12;

            // 1. Bắt đầu với IQueryable cơ bản
            IQueryable<HangHoa> hangHoasQuery = db.HangHoas.AsQueryable();

            // 2. Áp dụng các bộ lọc để xây dựng câu truy vấn
            if (!string.IsNullOrWhiteSpace(query))
            {
                hangHoasQuery = hangHoasQuery.Where(hh => hh.TenHh.ToLower().Contains(query.ToLower()));
            }

            // Luôn lọc ra các sản phẩm chưa bị xóa
            hangHoasQuery = hangHoasQuery.Where(hh => hh.IsDeleted != true);

            // Lấy tổng số lượng TRƯỚC khi phân trang
            var totalItems = await hangHoasQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // 3. Áp dụng sắp xếp và phân trang cho câu truy vấn
            var paginatedQuery = hangHoasQuery
               .OrderBy(p => p.TenHh)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize);

            // 4. CHỈ BÂY GIỜ mới thực thi truy vấn và chuyển đổi sang ViewModel
            //    `await` chỉ được sử dụng ở bước cuối cùng này.
            var result = await paginatedQuery
               .Select(p => new HangHoaVM
               {
                   MaHangHoa = p.MaHh,
                   TenHangHoa = p.TenHh,
                   HinhAnh = p.Hinh ?? "",
                   DonGia = p.DonGia ?? 0,
                   MoTaNgan = p.MoTaDonVi ?? "",
                   TenLoai = p.MaLoaiNavigation.TenLoai
               })
               .ToListAsync();


            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["CurrentQuery"] = query;
            return View(result);
        }

        [Route("HangHoa/ChiTiet /{id:int}")]
        public IActionResult Detail(int id)
        {
            var hangHoa = db.HangHoas
                .Include(hh => hh.MaLoaiNavigation)
                .SingleOrDefault(hh => hh.MaHh == id);


            if (hangHoa == null)
            {
                TempData["Message"] = $"Không tìm thấy sản phẩm có mã {id}";
                return Redirect("/404");
            }
            var result = new DetailProductVM
            {
                MaHangHoa = hangHoa.MaHh,
                TenHangHoa = hangHoa.TenHh,
                MoTa = hangHoa.MoTa ?? "",
                HinhAnh = hangHoa.Hinh ?? "",
                DonGia = hangHoa.DonGia ?? 0,
                MoTaNgan = hangHoa.MoTaDonVi ?? "",
                TenLoai = hangHoa.MaLoaiNavigation.TenLoai,
                ChiTiet = hangHoa.MoTa ?? "",
                DiemDanhGia = 5,
                SoLuongTon = 10,
            };
            return View(result);
        }

        public IActionResult getProduct(int id)
        {
            // Dùng AsNoTracking() vì ta chỉ đọc dữ liệu, không cần theo dõi thay đổi
            var hangHoa = db.HangHoas.AsNoTracking().FirstOrDefault(p => p.MaHh == id);

            if (hangHoa == null)
            {
                return NotFound();
            }

            // KHÔNG DÙNG AutoMapper và ProductVM ở đây.
            // Tạo một đối tượng ẩn danh (anonymous object) để trả về JSON.
            var productData = new
            {
                // Tên thuộc tính bên trái (vd: maHangHoa) phải khớp với tên bạn dùng trong JS
                maHangHoa = hangHoa.MaHh,
                tenHh = hangHoa.TenHh,
                maLoai = hangHoa.MaLoai,
                maNcc = hangHoa.MaNcc,
                ngaySx = hangHoa.NgaySx != default(DateTime) ? hangHoa.NgaySx.ToString("yyyy-MM-dd") : null, // Format cho input type="date"
                moTaDonVi = hangHoa.MoTaDonVi,
                donGia = hangHoa.DonGia,
                giamGia = hangHoa.GiamGia,
                moTa = hangHoa.MoTa,
                hinhUrl = hangHoa.Hinh // <-- Quan trọng: Trả về tên file ảnh
            };

            return Ok(productData);
        }

        public IActionResult QuanLySanPham(string? query) 
        {
            // 1. Lấy danh sách sản phẩm cho bảng 
            List<HangHoa> danhSachHangHoa;
            if (string.IsNullOrEmpty(query))
            {
                danhSachHangHoa = db.HangHoas
                                 .Where(p => p.IsDeleted != true)
                                .Include(p => p.MaLoaiNavigation)
                                .Include(p => p.MaNccNavigation)
                                .OrderByDescending(p => p.MaHh)
                                .ToList();
            }
            else
            {
                var HangHoaTimkiem= GenerateAlias(null, query);
                danhSachHangHoa = db.HangHoas
                                 .Where(p => p.IsDeleted != true && p.TenAlias != null && p.TenAlias.Contains(HangHoaTimkiem))
                                .Include(p => p.MaLoaiNavigation)
                                .Include(p => p.MaNccNavigation)
                                .OrderByDescending(p => p.MaHh)
                                .ToList();
                if (danhSachHangHoa.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy sản phẩm nào với từ khóa tìm kiếm.";
                }
            }
            ViewBag.hangHoa = danhSachHangHoa;

            // 2. Chuẩn bị dữ liệu phụ 
            var countDeleted = db.HangHoas.IgnoreQueryFilters().Count(p => p.IsDeleted == true);
            ViewBag.CountDeleted = countDeleted;

            // 3. Chuẩn bị DropDownList 
            ViewBag.DanhSachLoai = new SelectList(db.Loais.Where(p => p.Deleted != true).ToList(), "MaLoai", "TenLoai");
            ViewBag.DanhSachNcc = new SelectList(db.NhaCungCaps.ToList(), "MaNcc", "TenCongTy");

            // 4. Luôn tạo một Model RỖNG cho form
            // Vì View yêu cầu @model ProductVM, chúng ta phải cung cấp nó.
            var modelChoForm = new ProductVM();

            return View(modelChoForm);
        }
        [HttpPost]
        public async Task<IActionResult> AddOrEditProduct(ProductVM model)
        {
            int? id = (model.MaHangHoa == 0) ? null : model.MaHangHoa;
            // 1. Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng kiểm tra lại các thông tin đã nhập.";
                ViewBag.DanhSachLoai = new SelectList(db.Loais.Where(p => p.Deleted != true).ToList(), "MaLoai", "TenLoai", model.MaLoai);
                ViewBag.DanhSachNcc = new SelectList(db.NhaCungCaps.ToList(), "MaNcc", "TenCongTy", model.MaNcc);
                return View("QuanLySanPham", model); // Trả về View với lỗi
            }

            // 2. Kiểm tra tên sản phẩm trùng lặp
            var existingProductByName = await db.HangHoas
                .FirstOrDefaultAsync(p => p.TenHh.ToLower() == model.TenHh.ToLower() && p.IsDeleted != true);

            // Nếu là thêm mới (id == null) và đã có sản phẩm cùng tên
            // Hoặc là chỉnh sửa (id != null) và sản phẩm cùng tên đó không phải là sản phẩm đang sửa
            if (existingProductByName != null && (id == null || existingProductByName.MaHh != id))
            {
                ModelState.AddModelError("TenHh", "Tên sản phẩm này đã tồn tại.");
                ViewBag.ErrorMessage = "Thêm/Sửa sản phẩm thất bại.";
                ViewBag.DanhSachLoai = new SelectList(db.Loais.Where(p => p.Deleted !=true).ToList(), "MaLoai", "TenLoai", model.MaLoai);
                ViewBag.DanhSachNcc = new SelectList(db.NhaCungCaps.ToList(), "MaNcc", "TenCongTy", model.MaNcc);
                return View("QuanLySanPham", model);
            }


            try
            {
                // LOGIC THÊM MỚI
                if (id == null)
                {
                    var newProduct = _mapper.Map<HangHoa>(model);

                    // Xử lý upload file (nếu có)
                    if (model.Hinh != null && model.Hinh.Length > 0)
                    {
                        newProduct.Hinh = await UploadImage(model.Hinh,"HangHoa");
                    }

                    newProduct.SoLanXem = 0; // Chỉ set cho sản phẩm mới
                    newProduct.IsDeleted = false;

                    // Thêm vào DB và lưu để lấy MaHh
                    db.HangHoas.Add(newProduct);
                    await db.SaveChangesAsync();

                    // Tạo TenAlias sau khi đã có MaHh
                    newProduct.TenAlias = GenerateAlias(newProduct.MaHh, newProduct.TenHh);
                    newProduct.TenHh = GenerateSlug(newProduct.TenHh);
                    db.Update(newProduct);
                    await db.SaveChangesAsync(); // Lưu lần 2 để cập nhật TenAlias

                    TempData["SuccessMessage"] = $"Thêm sản phẩm '{newProduct.TenHh}' thành công!";
                }
                // LOGIC SỬA
                else
                {
                    // Sửa lỗi 1: Tìm sản phẩm hiện có trong DB
                    var productToUpdate = await db.HangHoas.FindAsync(id);
                    if (productToUpdate == null || productToUpdate.IsDeleted == true)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để cập nhật.";
                        return RedirectToAction("QuanLySanPham", "HangHoa");
                    }

                    // Giữ lại ảnh cũ nếu không có ảnh mới được tải lên
                    string oldImage = productToUpdate.Hinh;

                    // Map các giá trị từ ViewModel vào đối tượng đã lấy từ DB
                    _mapper.Map(model, productToUpdate);

                    // Xử lý upload file (nếu có)
                    if (model.Hinh != null && model.Hinh.Length > 0)
                    {
                        productToUpdate.Hinh = await UploadImage(model.Hinh,"HangHoa");
                        // (Tùy chọn) Xóa file ảnh cũ nếu cần
                    }
                    else
                    {
                        productToUpdate.Hinh = oldImage; // Gán lại ảnh cũ
                    }

                    // Cập nhật TenAlias
                    productToUpdate.TenAlias = GenerateAlias(productToUpdate.MaHh, productToUpdate.TenHh);
                    productToUpdate.TenHh = GenerateSlug(productToUpdate.TenHh);
                    // Chỉ cần gọi Update và SaveChanges một lần duy nhất
                    db.Update(productToUpdate);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cập nhật sản phẩm '{productToUpdate.TenHh}' thành công!";
                }

                return RedirectToAction("QuanLySanPham", "HangHoa");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã có lỗi nghiêm trọng xảy ra. Vui lòng thử lại sau.";
                ViewBag.DanhSachLoai = new SelectList(db.Loais.ToList(), "MaLoai", "TenLoai", model.MaLoai);
                ViewBag.DanhSachNcc = new SelectList(db.NhaCungCaps.ToList(), "MaNcc", "TenCongTy", model.MaNcc);
                return View("QuanLySanPham", model);
            }
        }

        // Hàm trợ giúp để tránh lặp code (có thể để private trong cùng controller)
        private async Task<string> UploadImage(IFormFile file, string TenFolder)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Hinh", TenFolder);
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return uniqueFileName;
        }

        private string GenerateAlias(int? id, string productName)
        {
            if (string.IsNullOrEmpty(productName))
            {
                return "";
            }

            string chuoiDaChuanHoa = productName.Normalize(System.Text.NormalizationForm.FormD);

            string tenKhongDau = Regex.Replace(chuoiDaChuanHoa, @"\p{M}", string.Empty);

            string tenKhongDauDaSua = tenKhongDau.Replace("đ", "d").Replace("Đ", "D");

            string alias = tenKhongDauDaSua.ToLower().Replace(" ", "-");

            alias = Regex.Replace(alias, @"[^a-z0-9-]", "");

            if (id.HasValue)
            {
                return $"{id.Value}-{alias}";
            }

            return alias;
        }

        private string GenerateSlug( string productName)
        {
            if (string.IsNullOrEmpty(productName))
            {
                return "";
            }

            string chuoiDaChuanHoa = productName.Normalize(System.Text.NormalizationForm.FormD);

            //bỏ dấu
            string tenKhongDau = Regex.Replace(chuoiDaChuanHoa, @"\p{M}", string.Empty);

            string slug = tenKhongDau.Replace("đ", "d").Replace("Đ", "D");

            //bỏ các ký tự đặc biệt không hợp lệ
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            //chuyên thành chữ thường, thay thế khoảng trắng bằng gạch ngang
            slug = Regex.Replace(slug.ToLower().Trim(), @"\s+", "-");

            slug = Regex.Replace(slug, @"-+", "-"); // Thay thế nhiều gạch ngang bằng một
            slug = slug.Trim('-'); // Xóa gạch ngang ở đầu và cuối

            return slug;
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSorftProduct(int id)
        {
            var product = await db.HangHoas.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            try
            {
                
                product.IsDeleted = true; // Đánh dấu là đã xóa mềm
                product.DeletedAt = DateTime.Now; // Ghi lại thời gian xóa

                db.HangHoas.Update(product);
                await db.SaveChangesAsync();
                return Ok(new { success = true, message = "Sản phẩm đã được xóa thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa sản phẩm." });
            }
        }

        public IActionResult Gabage(string? query) {
            var deletedProducts = new List<HangHoa>();
            if (query == null)
            {
                 deletedProducts = db.HangHoas
                .Where(p => p.IsDeleted == true)
                .OrderByDescending(p => p.DeletedAt)
                .Include(p => p.MaLoaiNavigation)
                 .Include(p => p.MaNccNavigation)
                .ToList();
            }
            else
            {
                deletedProducts = db.HangHoas
               .Where(p => p.IsDeleted == true)
               .Where(p => p.TenAlias != null && p.TenAlias.ToLower().Contains(query.ToLower()))
               .OrderByDescending(p => p.DeletedAt)
               .Include(p => p.MaLoaiNavigation)
                .Include(p => p.MaNccNavigation)
               .ToList();
                if(deletedProducts.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy sản phẩm nào trong thùng rác với từ khóa tìm kiếm.";
                }
            }
           
            ViewBag.ProductsInGabge = deletedProducts;
            ViewBag.CurrentQuery = query;

            return View();
        }

        [HttpPost("~/HangHoa/Restore")]
        public async Task<IActionResult> Restore([FromBody] List<int>ids)
        {

            if (ids == null || !ids.Any())
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp danh sách ID sản phẩm để hoàn tác." });
            }
try
            {
            var productsToRestore = await db.HangHoas
                                            .Where(p => ids.Contains(p.MaHh))
                                            .ExecuteUpdateAsync(p => p
                                                .SetProperty(h => h.IsDeleted, false)
                                                .SetProperty(h => h.DeletedAt,(DateTime?) null));
                return Ok(new
                {
                    success = true,
                    message = $"Đã hoàn tác thành công {productsToRestore} sản phẩm."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi hoàn tác sản phẩm." });
            }

        }
    }
}
