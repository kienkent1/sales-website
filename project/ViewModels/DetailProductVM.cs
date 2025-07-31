using Microsoft.Build.ObjectModelRemoting;

namespace project.ViewModels
{
    public class DetailProductVM
    {
        public int MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public double DonGia { get; set; }
        public string MoTaNgan { get; set; }
        public string TenLoai { get; set; }
        public string ChiTiet { get; set; }
       public int DiemDanhGia { get; set; }
        public int SoLuongTon { get; set; }
     public string Slug { get; set; }
        public string TenNhaCungCap { get; set; }
        public string TenThuongHieu { get; set; }
        public string TenDonViTinh { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public bool? Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int MaLoai { get; set; }
    }
}
