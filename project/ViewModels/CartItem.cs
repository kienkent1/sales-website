namespace project.ViewModels
{
    public class CartItem
    {

        public int? MaGh { get; set; }
        public string MaKh { get; set; } = null!;

        public int SoLuong { get; set; }

        public int MaHh { get; set; }

        public string TenHangHoa { get; set; }

        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; }
        public string? Slug { get; set; }
        public double ThanhTien => (double)(SoLuong * DonGia);
    }
}
