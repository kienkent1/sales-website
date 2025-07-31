using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Helpers;
using project.ViewModels;

namespace project.ViewComponents
{
    public class CardViewComponent : ViewComponent
    {
        private readonly Hshop2023Context _db; 

   
        public CardViewComponent(Hshop2023Context db)
        {
            _db = db;
        }

 
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            CardModel cardModel=new CardModel();

            if (!string.IsNullOrEmpty(customerId))
            {
                var cartItemsFromDb = _db.GioHangs
                                         .AsNoTracking() 
                                         .Where(gh => gh.MaKh == customerId);

                cardModel = new CardModel
                {
                    Soluong = await cartItemsFromDb.SumAsync(gh => gh.SoLuong),

                    Total = (double)(decimal)await cartItemsFromDb.SumAsync(gh => gh.SoLuong * (gh.MaHhNavigation.DonGia ?? 0))
                };
            }

            return View("CardPanel", cardModel);
        }
    }
}
