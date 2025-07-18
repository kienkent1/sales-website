using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Helpers;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//dk connectstring
builder.Services.AddDbContext<Hshop2023Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("HShop"));
});

//automapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
//session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    // Cookie sẽ hết hạn sau 7 ngày, bất kể người dùng có đóng trình duyệt hay không
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//cau hinh de dang nhap
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option=>
{
    option.LoginPath = "/KhachHang/DangNhap";
    option.AccessDeniedPath = "/AccessDenied";
    option.Cookie.IsEssential = true;
});

//dk paypal client dang singleton()- chi co 1 instance duy nhat trong toan bo ung dung
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]
    
    ));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession(); 

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
//Db first đoạn dưới để cập nhật lại vào db(mo console tren tools-nuget)
//Scaffold-DbContext "Data Source=XUONGKIEN\MSSQLSERVER01;Initial Catalog=Hshop2023;Integrated Security=True;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Data -f
