using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.ViewModels;
using System.Text.RegularExpressions;

namespace project.Controllers
{
    public class NhaCungCapController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public NhaCungCapController(Hshop2023Context context, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            db = context;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index(string? query)
        {
            List<NhaCungCap> NCC;
            if (string.IsNullOrWhiteSpace(query))
            {
                NCC = db.NhaCungCaps                    
               .Where(p => p.Deleted != true)
               .OrderByDescending(p => p.MaNcc)
               .ToList();
            }
            else
            {
                NCC = db.NhaCungCaps
                    .Where(p => p.Deleted != true && p.TenCongTy != null && p.TenCongTy.Contains(query))
                    .OrderByDescending(p => p.MaNcc)
                .ToList();

                if (NCC.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy tên công ty nào với từ khóa tìm kiếm.";
                }
            }
            var Nccs = db.NhaCungCaps.Where(p => p.Deleted == true).Count();
            ViewBag.CountNCCsDeleted = Nccs;
            ViewBag.NCCs = NCC;

            var viewModel = new NhaCungCapVM();

            return View(viewModel);
           
        }
        public IActionResult GetNCC(string id)
        {
            var Ncc = db.NhaCungCaps.AsNoTracking().FirstOrDefault(p => p.MaNcc == id);
            if (Ncc == null)
            {
                return NotFound();
            }

            var NccData = new
            {
                maNCC = Ncc.MaNcc,
                tenCty = Ncc.TenCongTy,
                ngLienLac = Ncc.NguoiLienLac,
                email = Ncc.Email,
                dienThoai = Ncc.DienThoai,
                diaChi = Ncc.DiaChi,
                moTa = Ncc.MoTa,
                logo = Ncc.Logo,
            };
            return Ok(NccData);
        }
        [HttpDelete]
        public async Task<IActionResult> SorftDeleteNCC(string id)
        {
            var Ncc = await db.NhaCungCaps.FindAsync(id);
            if (Ncc == null)
            {
                return NotFound();
            }
            try
            {
                Ncc.Deleted = true;
                Ncc.DeletedAt = DateTime.Now;
                db.NhaCungCaps.Update(Ncc);
                await db.SaveChangesAsync();
                return Ok(new { success = true, message = "Nhà cung cấp đã được xóa thành công." });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa nhà cung cấp." });
            }

        }
        private string GenerateSlug(string NCC)
        {
            if (string.IsNullOrEmpty(NCC))
            {
                return "";
            }

            string chuoiDaChuanHoa = NCC.Normalize(System.Text.NormalizationForm.FormD);

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
        public IActionResult GabageNCC(string? query)
        {
            var deletedNccs = new List<NhaCungCap>();
            if (query == null)
            {
                deletedNccs = db.NhaCungCaps
               .Where(p => p.Deleted == true)
               .OrderByDescending(p => p.DeletedAt)
               .ToList();
            }
            else
            {
                deletedNccs = db.NhaCungCaps
               .Where(p => p.Deleted == true)
               .Where(p => p.TenCongTy != null && p.TenCongTy.Contains(query))
               .OrderByDescending(p => p.DeletedAt)
               .ToList();
                if (deletedNccs.Count == 0)
                {
                    TempData["Message"] = "Không tìm thấy sản phẩm nào trong thùng rác với từ khóa tìm kiếm.";
                }
            }

            ViewBag.NccsInGabge = deletedNccs;
            ViewBag.CurrentQuery = query;

            return View();
        }

        [HttpPost("~/NhaCungCap/NCCRestore")]
        public async Task<IActionResult> NCCRestore([FromBody] List<string> ids)
        {

            if (ids == null || !ids.Any())
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp danh sách ID nhà cung cấp để hoàn tác." });
            }
            try
            {
                var nccsToRestore = await db.NhaCungCaps
                                                .Where(p => ids.Contains(p.MaNcc))
                                                .ExecuteUpdateAsync(p => p
                                                    .SetProperty(h => h.Deleted, false)
                                                    .SetProperty(h => h.DeletedAt, (DateTime?)null));
                return Ok(new
                {
                    success = true,
                    message = $"Đã hoàn tác thành công {nccsToRestore} nhà cung cấp."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi hoàn tác nhà cung cấp." });
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
    }
}
