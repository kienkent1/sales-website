using System.ComponentModel.DataAnnotations;

namespace project.ViewModels
{
    public class CheckOutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [DataType(DataType.PhoneNumber)]
        public string SoDienThoai { get; set; }

        public string? GhiChu { get; set; }

        // Thông tin người dùng lựa chọn trên form
 
        public string CachThanhToan { get; set; }


        public string CachVanChuyen { get; set; }

        // Trường này sẽ được điền bởi JavaScript và gửi về
        public decimal PhiVanChuyen { get; set; }

        public string? maGiamGia { get; set; }

        // Danh sách các sản phẩm để hiển thị trên trang
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
