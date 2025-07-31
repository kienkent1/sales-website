using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Helpers;
using project.ViewModels;
using System.Text.RegularExpressions;

namespace project.Controllers
{
    public class NhaCungCapController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;
        private readonly Util _util;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public NhaCungCapController(Hshop2023Context context, IMapper mapper, Util util)
        {
            db = context;
            _mapper = mapper;
            _util = util;
        }
        [Authorize(Roles = "1,2")]
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
            ViewBag.CurrentQuery = query;
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
        [Authorize(Roles = "1,2")]
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


        [HttpPost]
        public async Task<IActionResult> AddOrEditNCC(NhaCungCapVM model)
        {
            string? id = (model.MaNcc == "") ? null : model.MaNcc;
            // 1. Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng kiểm tra lại các thông tin đã nhập.";
                return View("NhaCungCap", model); // Trả về View với lỗi
            }

            // 2. Kiểm tra tên sản phẩm trùng lặp
            var existingNCCByName = await db.NhaCungCaps
                .FirstOrDefaultAsync(p => p.TenCongTy.ToLower() == model.TenCongTy.ToLower() && p.Deleted != true);

            // Nếu là thêm mới (id == null) và đã có sản phẩm cùng tên
            // Hoặc là chỉnh sửa (id != null) và sản phẩm cùng tên đó không phải là sản phẩm đang sửa
            if (existingNCCByName != null && (id == null || existingNCCByName.MaNcc != id))
            {
                ModelState.AddModelError("TenCongTy", "Tên cty này đã tồn tại.");
                return View("NhaCungCap", model);
            }


            try
            {
                // LOGIC THÊM MỚI
                if (id == null)
                {
                    var newNcc = _mapper.Map<NhaCungCap>(model);

                    // Xử lý upload file (nếu có)
                    if (model.Logo != null && model.Logo.Length > 0)
                    {
                        newNcc.Logo = await _util.UploadImage(model.Logo, "NhaCungCap");
                    }

                    newNcc.Deleted = false;

                    // Thêm vào DB và lưu để lấy MaHh
                    db.NhaCungCaps.Add(newNcc);
                    await db.SaveChangesAsync();

                    // Tạo TenAlias sau khi đã có MaHh
                    newNcc.TenCongTy = Helpers.Util.GenerateSlug(newNcc.TenCongTy);
                    db.Update(newNcc);
                    await db.SaveChangesAsync(); // Lưu lần 2 để cập nhật TenAlias

                    TempData["SuccessMessage"] = $"Thêm nhà cung cấp'{newNcc.TenCongTy}' thành công!";
                }
                // LOGIC SỬA
                else
                {
                    // Sửa lỗi 1: Tìm sản phẩm hiện có trong DB
                    var NccToUpdate = await db.NhaCungCaps.FindAsync(id);
                    if (NccToUpdate == null || NccToUpdate.Deleted == true)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy cty để cập nhật.";
                        return RedirectToAction("Index", "NhaCungCap");
                    }

                    // Giữ lại ảnh cũ nếu không có ảnh mới được tải lên
                    string oldImage = NccToUpdate.Logo;

                    // Map các giá trị từ ViewModel vào đối tượng đã lấy từ DB
                    _mapper.Map(model, NccToUpdate);

                    // Xử lý upload file (nếu có)
                    if (model.Logo != null && model.Logo.Length > 0)
                    {
                        NccToUpdate.Logo = await _util.UploadImage(model.Logo, "NhaCungCap");
                        // (Tùy chọn) Xóa file ảnh cũ nếu cần
                    }
                    else
                    {
                        NccToUpdate.Logo = oldImage; // Gán lại ảnh cũ
                    }

                    NccToUpdate.TenCongTy = Helpers.Util.GenerateSlug(NccToUpdate.TenCongTy);
                    // Chỉ cần gọi Update và SaveChanges một lần duy nhất
                    db.Update(NccToUpdate);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cập nhật nhà cung cấp '{NccToUpdate.TenCongTy}' thành công!";
                }

                return RedirectToAction("Index", "NhaCungCap");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã có lỗi nghiêm trọng xảy ra. Vui lòng thử lại sau.";
                return View("NhaCungCap", model);
            }
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
    }
}
