using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.ViewModels;
using System.Text.RegularExpressions;

namespace project.Controllers
{
    public class LoaiController : Controller
    {

        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public LoaiController(Hshop2023Context context, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            db = context;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index(string? query)
        {
            List<Loai> loai;
            if (string.IsNullOrWhiteSpace(query))
            {
                loai = db.Loais
               .Where(p => p.Deleted != true)
               .OrderByDescending(p => p.MaLoai)
               .ToList();
            }
            else
            {
                var LoaiTimKiem = GenerateAlias(null, query);
                loai = db.Loais
                    .Where(p => p.Deleted != true && p.TenLoaiAlias != null && p.TenLoaiAlias.Contains(LoaiTimKiem))
                    .OrderByDescending(p => p.MaLoai)
                .ToList();

                if (loai.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy loại nào với từ khóa tìm kiếm.";
                }
            }
            var ListLoais= db.Loais.Where(p => p.Deleted == true).Count();
            ViewBag.CountLoaisDeleted = ListLoais;
            ViewBag.Loai = loai;

            var viewModel = new LoaiVM();
      
            return View(viewModel);
        }

        private string GenerateAlias(int? id, string Loai)
        {
            if (string.IsNullOrEmpty(Loai))
            {
                return "";
            }

            string chuoiDaChuanHoa = Loai.Normalize(System.Text.NormalizationForm.FormD);

            string tenKhongDau = Regex.Replace(chuoiDaChuanHoa, @"\p{M}", string.Empty);

            string tenKhongDauDaSua = tenKhongDau.Replace("đ", "d").Replace("Đ", "d");

            string alias = tenKhongDauDaSua.ToLower().Replace(" ", "-");

            alias = Regex.Replace(alias, @"[^a-z0-9-]", "");

            if (id.HasValue)
            {
                return $"{id.Value}-{alias}";
            }

            return alias;

        }
        private string GenerateSlug(string Loai)
        {
            if (string.IsNullOrEmpty(Loai))
            {
                return "";
            }

            string chuoiDaChuanHoa = Loai.Normalize(System.Text.NormalizationForm.FormD);

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

        public IActionResult GetLoai(int id)
        {
            var loai = db.Loais.AsNoTracking().FirstOrDefault(p => p.MaLoai == id);
            if (loai == null)
            {
                return NotFound();
            }

            var loaiData = new
            {
                maLoai = loai.MaLoai,
                tenLoai = loai.TenLoai,
                moTa = loai.MoTa,
                hinh = loai.Hinh,
            };
            return Ok(loaiData);
        }
        [HttpPost]
        public async Task <IActionResult> AddOrEditLoai(LoaiVM model)
        {
            int? id = (model.MaLoai == 0) ? null : model.MaLoai;
            // 1. Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng kiểm tra lại các thông tin đã nhập.";
                return View("Index", model); 
            }

            // 2. Kiểm tra tên sản phẩm trùng lặp
            var existingLoaiByName = await db.Loais
                .FirstOrDefaultAsync(p => p.TenLoai.ToLower() == model.TenLoai.ToLower() && p.Deleted != true);

            // Nếu là thêm mới (id == null) và đã có sản phẩm cùng tên
            // Hoặc là chỉnh sửa (id != null) và sản phẩm cùng tên đó không phải là sản phẩm đang sửa
            if (existingLoaiByName != null && (id == null || existingLoaiByName.MaLoai != id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại này đã tồn tại.");
                ViewBag.ErrorMessage = "Thêm/Sửa loại thất bại.";
                return View("Index", model);
            }
          
            try
            {
                // LOGIC THÊM MỚI
                if (id == null)
                {
                    var newLoai = _mapper.Map<Loai>(model);

                    // Xử lý upload file (nếu có)
                    if (model.Hinh != null && model.Hinh.Length > 0)
                    {
                        newLoai.Hinh = await UploadImage(model.Hinh, "Loai");
                    }

                    newLoai.Deleted = false;

                    // Thêm vào DB và lưu để lấy MaHh
                    db.Loais.Add(newLoai);
                    await db.SaveChangesAsync();

                    // Tạo TenAlias sau khi đã có MaHh
                    newLoai.TenLoaiAlias = GenerateAlias(newLoai.MaLoai, newLoai.TenLoai);
                    newLoai.Slug = GenerateSlug(newLoai.TenLoai);
                    db.Update(newLoai);
                    await db.SaveChangesAsync(); // Lưu lần 2 để cập nhật TenAlias

                    TempData["SuccessMessage"] = $"Thêm loại '{newLoai.TenLoai}' thành công!";
                }
                // LOGIC SỬA
                else
                {
                    // Sửa lỗi 1: Tìm sản phẩm hiện có trong DB
                    var loaiToUpdate = await db.Loais.FindAsync(id);
                    if (loaiToUpdate == null || loaiToUpdate.Deleted == true)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy loại để cập nhật.";
                        return RedirectToAction("Index", "Loai");
                    }

                    // Giữ lại ảnh cũ nếu không có ảnh mới được tải lên
                    string oldImage = loaiToUpdate.Hinh;

                    // Map các giá trị từ ViewModel vào đối tượng đã lấy từ DB
                    _mapper.Map(model, loaiToUpdate);

                    // Xử lý upload file (nếu có)
                    if (model.Hinh != null && model.Hinh.Length > 0)
                    {
                        loaiToUpdate.Hinh = await UploadImage(model.Hinh, "Loai");
                        // (Tùy chọn) Xóa file ảnh cũ nếu cần
                    }
                    else
                    {
                        loaiToUpdate.Hinh = oldImage; // Gán lại ảnh cũ
                    }

                    // Cập nhật TenAlias
                    loaiToUpdate.TenLoaiAlias = GenerateAlias(loaiToUpdate.MaLoai, loaiToUpdate.TenLoai);
                    loaiToUpdate.Slug = GenerateSlug(loaiToUpdate.TenLoai);
                    // Chỉ cần gọi Update và SaveChanges một lần duy nhất
                    db.Update(loaiToUpdate);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cập nhật sản phẩm '{loaiToUpdate.TenLoai}' thành công!";
                }

                return RedirectToAction("Index", "Loai");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã có lỗi nghiêm trọng xảy ra. Vui lòng thử lại sau.";
                return View("Index", model);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> SorftDeleteLoai(int id)
        {
            var loai = await db.Loais.FindAsync(id);
            if (loai == null)
            {
                return NotFound();
            }
            try
            {
                loai.Deleted = true;
                loai.DeletedAt = DateTime.Now;
                db.Loais.Update(loai);
                await db.SaveChangesAsync();
                return Ok(new { success = true, message = "Loại đã được xóa thành công." });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa loại." });
            }

        }
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


        public IActionResult GabageLoai(string? query)
        {
            var deletedLoais = new List<Loai>();
            if (query == null)
            {
                deletedLoais = db.Loais
               .Where(p => p.Deleted == true)
               .OrderByDescending(p => p.DeletedAt)
               .ToList();
            }
            else
            {
                var LoaiTimKiem = GenerateAlias(null, query);
                deletedLoais = db.Loais
               .Where(p => p.Deleted == true)
               .Where(p => p.TenLoaiAlias != null && p.TenLoaiAlias.Contains(LoaiTimKiem))
               .OrderByDescending(p => p.DeletedAt)
               .ToList();
                if (deletedLoais.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy sản phẩm nào trong thùng rác với từ khóa tìm kiếm.";
                }
            }

            ViewBag.LoaisInGabge = deletedLoais;
            ViewBag.CurrentQuery = query;

            return View();
        }



        [HttpPost("~/loai/LoaiRestore")]
        public async Task<IActionResult> LoaiRestore([FromBody] List<int> ids)
        {

            if (ids == null || !ids.Any())
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp danh sách ID sản phẩm để hoàn tác." });
            }
 try
            {
            var loaisToRestore = await db.Loais
                                            .Where(p => ids.Contains(p.MaLoai))
                                            .ExecuteUpdateAsync(p => p
                                                .SetProperty(x => x.Deleted, false)
                                                .SetProperty(x => x.DeletedAt,(DateTime?) null));

                return Ok(new
                {
                    success = true,
                    message = $"Đã hoàn tác thành công {loaisToRestore} sản phẩm."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi hoàn tác sản phẩm." });
            }

        }
    }
}