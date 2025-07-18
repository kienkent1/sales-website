using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using project.Data;
using project.Helpers;
using project.ViewModels;
using System.Threading.Tasks;

namespace project.Controllers
{
    public class CardController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly PaypalClient _paypalClient;

        public CardController(Hshop2023Context context, PaypalClient paypalClient) {
            db = context;
            _paypalClient = paypalClient;
        } 

        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
        public IActionResult Index()
        {
            return View(Cart);
        }

        public IActionResult AddToCard(int id, int quality=1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(x => x.MaHangHoa == id);
            if(item == null)
            {
                var hangHoa = db.HangHoas.Find(id);
                if(hangHoa == null)
                {
                    TempData["message"] = $"Không tìm thấy sản phẩm có mã: {id}";
                    return Redirect("/404");
                }
                  item = new CartItem
                {
                    MaHangHoa = hangHoa.MaHh,
                    TenHangHoa = hangHoa.TenHh,
                    SoLuong = quality,
                    DonGia = (decimal)(hangHoa.DonGia ?? 0),
                    HinhAnh = hangHoa.Hinh ?? string .Empty,
                };
                gioHang.Add(item);
            }
            else
            {
                item.SoLuong += quality;
            }
            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index");
        }
        public IActionResult RemoveCard(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(x => x.MaHangHoa == id);
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        [HttpGet]
        public IActionResult ThanhToan()
        {

            if (Cart.Count == 0)
            {
                TempData["message"] = "Giỏ hàng rỗng, không thể thanh toán";
                return RedirectToAction("/");
            }
            ViewBag.paypalClientId = _paypalClient.ClientId;
            return View(Cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult ThanhToan(CheckOutVM model)
        {

            if (ModelState.IsValid)
            {
               var customerId= HttpContext.User.Claims.SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID).Value;

                var khachHang = new KhachHang();

                if (model.GiongKhachHang)
                {
                    khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
                }
                
                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    HoTen = model.HoTen ?? khachHang.HoTen,
                    DiaChi = model.DiaChi ?? khachHang.DiaChi,
                    SoDienThoai = model.SoDienThoai ?? khachHang.DienThoai,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán khi nhận hàng",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai=0,
                    GhiChu = model.GhiChu,
                };
                db.Database.BeginTransaction();
                
                    try
                    {

                    db.Database.CommitTransaction();
                    db.Add(hoadon);
                        db.SaveChanges();

                        var cthd = new List<ChiTietHd>();
                        foreach (var item in Cart)
                        {
                            cthd.Add(new ChiTietHd
                            {
                                MaHd = hoadon.MaHd,
                                MaHh = item.MaHangHoa,
                                SoLuong = item.SoLuong,
                                DonGia = (double)item.DonGia,
                                GiamGia = 0,
                            });
                        }
                        db.AddRange(cthd);
                        db.SaveChanges();
                        HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                        return View("Success");
                    }
                    catch
                    {

                    db.Database.RollbackTransaction();
                }
                
            }
            return View(Cart);
        }

        [Authorize]
        public IActionResult PaymentSuccess()
        {
            return View("Success");
        }
        #region Paypal payment
        [Authorize]
        [HttpPost ("/Card/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
        {
            //thong tin cua don hang gui qua paypal
            var tongTien= Cart.Sum(x => x.ThanhTien).ToString();
            var donViTienTe= "USD"; //hoac VND
            var maDonHang = "DH" + DateTime.Now.Ticks.ToString();

            try
            {
                var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHang);

                return Ok(response);
            }
            catch (Exception ex)
            {

                var error = new {ex.GetBaseException().Message};
                return BadRequest(error);
            }
        }

        [Authorize]
        [HttpPost("/Card/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderId);
               
                    //thanh toan thanh cong
                    return Ok(response);
              
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }
        #endregion
    }
}
