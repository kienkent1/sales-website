using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class ProductVM
    {
        
        public int MaHangHoa { get; set; }

        [MaxLength(50, ErrorMessage = "*Tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "Tên hàng hóa không được để trống")]
        public string TenHh { get; set; } 

        public string? TenAlias { get; set; }

        [Required(ErrorMessage = "Loại không được để trống")]
        public int MaLoai { get; set; }

        [MaxLength(50, ErrorMessage = "*Mô tả không được quá 50 ký tự")]
        public string? MoTaDonVi { get; set; }

        [Required(ErrorMessage = "Đơn giá hàng hóa không được để trống")]
        [Range(1, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 1")]
        public double DonGia { get; set; }

        public string? HinhUrl { get; set; }

        [Required(ErrorMessage = "Hình không được để trống")]
        public IFormFile Hinh { get; set; }

        [Required(ErrorMessage = "Ngày sản xuất không được để trống")]
        public DateTime NgaySx { get; set; }

        [Required(ErrorMessage = "Giảm giá không được để trống")]
        [Range(0, 99, ErrorMessage = "Giảm giá phải trong khoảng từ 0 đến 99")]
        public double GiamGia { get; set; }


        public int SoLanXem { get; set; }

       
        public string? MoTa { get; set; }

        [MaxLength(50, ErrorMessage = "*Tên không được quá 50 ký tự")]
        [Required(ErrorMessage = "Tên hàng hóa không được để trống")]
        public string MaNcc { get; set; } = null!;

        public bool? IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public string? TenLoai { get; set; }
        public string? TenCongTy { get; set; }
    }
}
