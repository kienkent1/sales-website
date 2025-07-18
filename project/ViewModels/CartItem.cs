namespace project.ViewModels
{
    public class CartItem
    {
        public int MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; }
        public double ThanhTien => (double)(SoLuong * DonGia);
    }
}
