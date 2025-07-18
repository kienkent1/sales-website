using Microsoft.AspNetCore.Mvc;
using project.Helpers;
using project.ViewModels;

namespace project.ViewComponents
{
    public class CardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var count = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
            return View("CardPanel",new CardModel
            {
                Quality = count.Sum(x => x.SoLuong),
                Total = count.Sum(x => x.ThanhTien)
            });
        }
    }
}
