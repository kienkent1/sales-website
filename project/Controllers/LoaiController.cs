using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Helpers;
using project.ViewModels;
using System.Text.RegularExpressions;

namespace project.Controllers
{
    public class LoaiController : Controller
    {

        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly Util _util;
        public LoaiController(Hshop2023Context context, IMapper mapper, Util util)
        {
            db = context;
            _mapper = mapper;
            _util= util;
        }
        [Authorize(Roles = "1,2")]
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
                var LoaiTimKiem = Helpers.Util.GenerateAlias(null, query);
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
            ViewBag.CurrentQuery = query;
            var viewModel = new LoaiVM();
      
            return View(viewModel);
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
                        newLoai.Hinh = await _util.UploadImage(model.Hinh, "Loai");
                    }

                    newLoai.Deleted = false;

                    // Thêm vào DB và lưu để lấy MaHh
                    db.Loais.Add(newLoai);
                    await db.SaveChangesAsync();

                    // Tạo TenAlias sau khi đã có MaHh
                    newLoai.TenLoaiAlias = Helpers.Util.GenerateAlias(newLoai.MaLoai, newLoai.TenLoai);
                    newLoai.Slug = Helpers.Util.GenerateSlug(newLoai.TenLoai);
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
                        loaiToUpdate.Hinh = await _util.UploadImage(model.Hinh, "Loai");
                        // (Tùy chọn) Xóa file ảnh cũ nếu cần
                    }
                    else
                    {
                        loaiToUpdate.Hinh = oldImage; // Gán lại ảnh cũ
                    }

                    // Cập nhật TenAlias
                    loaiToUpdate.TenLoaiAlias = Helpers.Util.GenerateAlias(loaiToUpdate.MaLoai, loaiToUpdate.TenLoai);
                    loaiToUpdate.Slug = Helpers.Util.GenerateSlug(loaiToUpdate.TenLoai);
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


        [Authorize(Roles = "1,2")]
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
                var LoaiTimKiem = Helpers.Util.GenerateAlias(null, query);
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