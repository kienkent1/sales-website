using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [Authorize]
        [Route("GioHang")]
        public IActionResult Index()
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            // Xử lý giỏ hàng cho cả người dùng đăng nhập và khách
            List<CartItem> cartItems = new List<CartItem>();

            if (!string.IsNullOrEmpty(customerId)) 
            {
                var itemsFromDb = db.GioHangs
                                     .Include(gh => gh.MaHhNavigation) 
                                     .Where(gh => gh.MaKh == customerId)
                                     .ToList();

                cartItems = itemsFromDb.Select(item => new CartItem
                {
                    MaGh = item.MaGh,
                    MaHh = item.MaHh,
                    TenHangHoa = item.MaHhNavigation.TenHh,
                    DonGia = (decimal)(item.MaHhNavigation.DonGia ?? 0),
                    HinhAnh = item.MaHhNavigation.Hinh ?? string.Empty,
                    Slug=item.MaHhNavigation.Slug,
                    SoLuong = item.SoLuong
                }).ToList();
            }


            return View(cartItems);
        }
        
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
       
            var customerId = HttpContext.User.Claims.SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("Login", "KhachHang", new { ReturnUrl = Url.Action("Details", "HangHoa", new { id = id }) });
            }

            var cartItem = await db.GioHangs.SingleOrDefaultAsync(x => x.MaHh == id && x.MaKh == customerId);

            if (cartItem == null) 
            {

                var product = await db.HangHoas.FindAsync(id);
                if (product == null)
                {
                    TempData["message"] = $"Không tìm thấy sản phẩm có mã: {id}";
                    return Redirect("/404");
                }
                cartItem = new GioHang
                {
                    MaKh = customerId,
                    MaHh = id,
                    SoLuong = quantity
                };
                db.GioHangs.Add(cartItem);
            }
            else 
            {
                cartItem.SoLuong += quantity;
            }


            await db.SaveChangesAsync();

            TempData["Message"] = "Thêm sản phẩm vào giỏ hàng thành công!";
            return RedirectToAction("Index", "Card"); 
        }
        public IActionResult RemoveCard(int id)
        {
            var item = db.GioHangs.SingleOrDefault(x => x.MaGh == id);
            if (item != null)
            {
                db.GioHangs.Remove(item);
                db.SaveChanges();
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
                                MaHh = item.MaHh,
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

        public class UpdateCartInput
        {
            public int Id { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPost]
        [Authorize]
        // Đặt tên Route rõ ràng
        [Route("/Card/UpdateCart")]
        // Validate token để chống tấn công CSRF
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartInput model)
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            var cartItem = await db.GioHangs
                                   .Include(gh => gh.MaHhNavigation)
                                   .SingleOrDefaultAsync(gh => gh.MaGh == model.Id && gh.MaKh == customerId);

            if (cartItem == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }

            if (model.Quantity <= 0)
            {
                // Nếu số lượng <= 0, ta sẽ xóa sản phẩm khỏi giỏ hàng
                db.GioHangs.Remove(cartItem);
            }
            else
            {
                cartItem.SoLuong = model.Quantity;
            }

            await db.SaveChangesAsync();

            // Tính toán lại các giá trị tổng để trả về
            var itemsInCart = db.GioHangs
                                .Include(gh => gh.MaHhNavigation)
                                .Where(gh => gh.MaKh == customerId);

            decimal newItemTotal = (decimal)(cartItem.MaHhNavigation.DonGia ?? 0) * model.Quantity;
            decimal cartSubtotal = itemsInCart.Sum(item => item.SoLuong * (decimal)(item.MaHhNavigation.DonGia ?? 0));
            decimal shippingFee = 3.00m; // Giả sử phí ship cố định là $3
            decimal cartTotal = cartSubtotal + shippingFee;

            return Ok(new
            {
                success = true,
                newItemTotal = newItemTotal.ToString("N2"), // Định dạng 2 chữ số thập phân
                cartSubtotal = cartSubtotal.ToString("N2"),
                cartTotal = cartTotal.ToString("N2"),
                message = "Cập nhật giỏ hàng thành công."
            });
        }
    }
}
