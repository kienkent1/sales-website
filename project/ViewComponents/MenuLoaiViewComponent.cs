using Microsoft.AspNetCore.Mvc;
using project.Data;
using project.ViewModels;

namespace project.ViewComponents
{
    public class MenuLoaiViewComponent:ViewComponent
    {
        private readonly Hshop2023Context db;

        public MenuLoaiViewComponent(Hshop2023Context context) => db = context;

        public IViewComponentResult Invoke()
        {
            var data= db.Loais.Select(lo=>new MenuLoai
            {
               MaLoai= lo.MaLoai,
               TenLoai= lo.TenLoai,
                SoLuong=lo.HangHoas.Count,    
                Hinh=lo.Hinh,
            }).OrderBy(p=>p.TenLoai);

            List<NhaCungCap> list;
            list = db.NhaCungCaps
               .OrderByDescending(p => p.TenCongTy)
               .ToList();

            ViewBag.DSCty = list;
            return View(data);      //Default.cshtml
            //return View("Default",data);
        }
    }
}
