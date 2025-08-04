using AutoMapper;
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
        private readonly IMapper _mapper;

        public CardController(Hshop2023Context context, PaypalClient paypalClient, IMapper mapper) {
            db = context;
            _paypalClient = paypalClient;
            _mapper = mapper;
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
                                     .Where(gh => gh.MaKh == customerId && gh.DatHang != true)
                                     .ToList();

                cartItems = _mapper.Map<List<CartItem>>(itemsFromDb);
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

            var cartItem = await db.GioHangs.SingleOrDefaultAsync(x => x.MaHh == id && x.MaKh == customerId && x.DatHang !=true);

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
        [Route("GioHang/ThanhToan")]
        public async Task<IActionResult> ThanhToan()
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var customer = await db.KhachHangs.FindAsync(customerId);

            if (customer == null)
            {
                return RedirectToAction("Login", "KhachHang");
            }

            var cartItemsDb = await db.GioHangs
                .Include(gh => gh.MaHhNavigation)
                .Where(gh => gh.MaKh == customerId && gh.DatHang != true)
                .ToListAsync();

            if (!cartItemsDb.Any())
            {
                TempData["message"] = "Giỏ hàng rỗng, không thể thanh toán";
                return RedirectToAction("Index");
            }

            var checkoutVm = new CheckOutVM
            {
                HoTen = customer.HoTen,
                DiaChi = customer.DiaChi,
                SoDienThoai = customer.DienThoai,
                CartItems = _mapper.Map<List<CartItem>>(cartItemsDb)
                // Các trường khác như CachThanhToan, CachVanChuyen sẽ được chọn trên View
            };

            ViewBag.paypalClientId = _paypalClient.ClientId;
            return View(checkoutVm);
        }
        [HttpPost]
        [Route("GioHang/ThanhToan")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToan(CheckOutVM model)
        {

            var customerId = HttpContext.User.Claims.SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            if (ModelState.IsValid)
            {
                var itemsInCart = await db.GioHangs
                                          .Where(gh => gh.MaKh == customerId && gh.DatHang != true)
                                          .Include(gh => gh.MaHhNavigation)
                                          .ToListAsync();

                if (!itemsInCart.Any())
                {
                    TempData["Message"] = "Giỏ hàng của bạn đã được xử lý hoặc bị rỗng.";
                    return RedirectToAction("Index");
                }

                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {

                    var tongTienHang = itemsInCart.Sum(item => item.SoLuong * (item.MaHhNavigation.DonGia ?? 0));

                
                    var hoadon = new HoaDon
                    {
                        MaKh = customerId,
                        HoTen = model.HoTen,
                        DiaChi = model.DiaChi,
                        SoDienThoai = model.SoDienThoai,
                        NgayDat = DateTime.Now,
                        NgayGiao = EstimateDeliveryDate(model.CachVanChuyen), 
                        CachThanhToan = model.CachThanhToan,
                        CachVanChuyen = model.CachVanChuyen,
                        PhiVanChuyen = (double)model.PhiVanChuyen, 
                        MaTrangThai = 0,
                        GhiChu = model.GhiChu,
                        TongTien = (double?)((decimal)tongTienHang + model.PhiVanChuyen),
                        MaGiamGia=model.maGiamGia,
                    };
                    db.Add(hoadon);
                    await db.SaveChangesAsync();

                    var cthdList = new List<ChiTietHd>();
                    foreach (var item in itemsInCart)
                    {
                        cthdList.Add(new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            MaHh = item.MaHh,
                            SoLuong = item.SoLuong,
                            DonGia = (double)(item.MaHhNavigation.DonGia ?? 0),
                            GiamGia = item.MaHhNavigation.GiamGia ?? 0,
                        });

                        // Đánh dấu sản phẩm trong giỏ đã được đặt hàng
                        item.DatHang = true;
                    }
                    db.AddRange(cthdList);
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    HttpContext.Session.Remove(MySetting.CART_KEY);
                    return View("Success");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    TempData["Message"] = "Đã xảy ra lỗi trong quá trình xử lý đơn hàng.";
                }
            }

            // Nếu model không hợp lệ, tải lại dữ liệu giỏ hàng và hiển thị lại form
            var cartItemsDb = await db.GioHangs
                .Include(gh => gh.MaHhNavigation)
                .Where(gh => gh.MaKh == customerId && gh.DatHang != true)
                .ToListAsync();
            model.CartItems = _mapper.Map<List<CartItem>>(cartItemsDb);

            return View(model);
        }

        private DateTime EstimateDeliveryDate(string shippingMethod)
        {
            if (shippingMethod == "SieuToc")
            {
                return DateTime.Now.AddDays(4); 
            }
       
            return DateTime.Now.AddDays(10); 
        }

        [Authorize]
        public IActionResult PaymentSuccess()
        {
            return View("Success");
        }
        #region Paypal payment
        public class CreatePaypalOrderRequest
        {
            public string CachVanChuyen { get; set; }
        }

        [Authorize]
        [HttpPost("/GioHang/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder([FromBody] CreatePaypalOrderRequest request, CancellationToken cancellationToken)
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            if (customerId == null) return Unauthorized();

            var itemsInCart = await db.GioHangs
                                        .Where(p => p.MaKh == customerId && p.DatHang != true)
                                        .Include(p => p.MaHhNavigation)
                                        .ToListAsync(cancellationToken);

            if (!itemsInCart.Any()) return BadRequest(new { message = "Giỏ hàng rỗng." });

            var tongTienHang = itemsInCart.Sum(item => item.SoLuong * (item.MaHhNavigation.DonGia ?? 0));

            // Sử dụng phương thức helper để tính phí vận chuyển dựa trên lựa chọn của người dùng
            decimal phiVanChuyen = CalculateShippingFee(request.CachVanChuyen);

            var tongTienCuoiCung = ((decimal)tongTienHang + phiVanChuyen);

            var donViTienTe = "USD";
            var maDonHang = "DH" + DateTime.Now.Ticks.ToString();

            try
            {
                var response = await _paypalClient.CreateOrder(tongTienCuoiCung.ToString("F2"), donViTienTe, maDonHang);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }
        [Authorize]
        [HttpPost("/GioHang/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderId, [FromBody] CheckOutVM model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var response = await _paypalClient.CaptureOrder(orderId);

                var customerId = HttpContext.User.Claims.SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
                if (customerId == null) return Unauthorized("Không xác thực được người dùng.");

                var itemsInCart = await db.GioHangs
                                        .Where(p => p.MaKh == customerId && p.DatHang != true)
                                        .Include(p => p.MaHhNavigation)
                                        .ToListAsync(cancellationToken);

                if (!itemsInCart.Any())
                {
                    return BadRequest(new { message = "Lỗi: Giỏ hàng đã được xử lý hoặc bị rỗng." });
                }

                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var tongTienHang = itemsInCart.Sum(p => p.SoLuong * (p.MaHhNavigation.DonGia ?? 0));
                    // Tính lại phí vận chuyển từ model để đảm bảo nhất quán
                    decimal phiVanChuyen = CalculateShippingFee(model.CachVanChuyen);

                    var hoadon = new HoaDon
                    {
                        MaKh = customerId,
                        // Lấy thông tin trực tiếp từ model mà client gửi lên
                        HoTen = model.HoTen,
                        DiaChi = model.DiaChi,
                        SoDienThoai = model.SoDienThoai,
                        NgayDat = DateTime.Now,
                        NgayGiao = EstimateDeliveryDate(model.CachVanChuyen),
                        CachThanhToan = "Thanh toán qua Paypal", // Giá trị này đã được xác định
                        CachVanChuyen = model.CachVanChuyen,
                        PhiVanChuyen = (double)phiVanChuyen,
                        MaTrangThai = 1, // Đã thanh toán
                        GhiChu = $"Đơn hàng PayPal. Order ID: {orderId}. " + model.GhiChu,
                        TongTien = (double?)((decimal)tongTienHang + phiVanChuyen)
                    };
                    db.Add(hoadon);
                    await db.SaveChangesAsync(cancellationToken);

                    var cthdList = new List<ChiTietHd>();
                    foreach (var item in itemsInCart)
                    {
                        cthdList.Add(new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            MaHh = item.MaHh,
                            SoLuong = item.SoLuong,
                            DonGia = (double)(item.MaHhNavigation.DonGia ?? 0),
                            GiamGia = item.MaHhNavigation.GiamGia ?? 0,
                        });
                        item.DatHang = true;
                    }
                    db.AddRange(cthdList);
                    await db.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return Ok(response);
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    // Ghi log lỗi
                    return BadRequest(new { message = "Thanh toán thành công nhưng đã có lỗi khi lưu đơn hàng của bạn. Vui lòng liên hệ hỗ trợ." });
                }
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }

        private decimal CalculateShippingFee(string shippingMethod)
        {
            if (shippingMethod == "SieuToc")
            {
                return 2.00m; 
            }
            return 1.00m; 
        }
        #endregion

        public class UpdateCartInput
        {
            public int Id { get; set; }
            public int Quantity { get; set; }
        }
        private async void CreateHoaDon(string customerId, int IdHh)
        {
            //var createHoaDon = await db.HoaDons.
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
