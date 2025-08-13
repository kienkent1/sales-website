using project.Data;

namespace project.ViewModels
{
    public class HangHoaVM
    {
        public int MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public double DonGia { get; set; }
        public string MoTaNgan { get; set; }
        public double? GiamGia { get; set; }
        public string TenLoai { get; set; }
        public string Slug { get; set; }
        public virtual Loai MaLoaiNavigation { get; set; } = null!;

        public virtual NhaCungCap MaNccNavigation { get; set; } = null!;
    }
}
