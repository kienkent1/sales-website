using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Data;
using project.ViewModels;

namespace project.Controllers
{
    public class ContactController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;

        public ContactController(Hshop2023Context context, IMapper mapper)
        {
            db = context;
            _mapper = mapper;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(ContactVM model )
        {
            var contact = _mapper.Map<GopY>(model);
            if(ModelState.IsValid)
            {
                contact.NgayGy = DateOnly.FromDateTime(DateTime.Now);
                db.Gopies.Add(contact);
                db.SaveChanges();
                // Thêm một thông báo thành công để người dùng biết
                ViewBag.SuccessMessage = "Cảm ơn bạn đã gửi góp ý!";
                // Xóa model state để form được reset sau khi gửi thành công
                ModelState.Clear();
                return View("Index", new ContactVM { HoTen = "", NoiDung = "", DienThoai="", Email="" });
            }
            else
            {
                ViewBag.Message = "Vui lòng kiểm tra lại thông tin!";
                return View("Index",model);
            }

            
        }
    }
}
