using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using project.Data;
using project.Helpers;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<Util>();
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




//dk paypal client dang singleton()- chi co 1 instance duy nhat trong toan bo ung dung
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]

    ));

// === BẮT ĐẦU CẤU HÌNH AUTHENTICATION (ĐÃ GỘP LẠI) ===

builder.Services.AddAuthentication(options =>
{
    // Scheme mặc định để quản lý session của người dùng là Cookie.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Khi một trang [Authorize] yêu cầu đăng nhập, nó sẽ không tự động chuyển đến Google.
    // Thay vào đó, nó sẽ chuyển đến LoginPath được định nghĩa trong AddCookie.
    // Việc challenge đến Google sẽ được thực hiện thủ công trong action `loginByGoogle`.
})
.AddCookie(options =>
{
    // Đây là trang đăng nhập chung của bạn (cả form và nút Google).
    // Nếu người dùng chưa đăng nhập và cố truy cập trang cần quyền, họ sẽ bị chuyển về đây.
    options.LoginPath = "/KhachHang/DangNhap";
    options.AccessDeniedPath = "/404";
    options.Cookie.IsEssential = true;
})
.AddGoogle(options =>
{
    // Đọc ClientID và ClientSecret từ cấu hình
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];

    // Yêu cầu Google trả về thêm thông tin
    options.Scope.Add("profile");
    options.Scope.Add("email");
    // Xin quyền lấy ngày sinh từ People API
    options.Scope.Add("https://www.googleapis.com/auth/user.birthday.read");

    // Quan trọng: Lưu lại các token để có thể dùng cho các cuộc gọi API sau này
    options.SaveTokens = true;

    // === PHẦN QUAN TRỌNG NHẤT BẮT ĐẦU TỪ ĐÂY ===

    // 1. Ánh xạ trực tiếp trường "picture" từ JSON của Google sang một claim
    // Tên claim sẽ là "picture"
    options.ClaimActions.MapJsonKey("picture", "picture", "url");

    // 2. Can thiệp vào quá trình tạo ticket để gọi People API và lấy ngày sinh
    options.Events.OnCreatingTicket = async context =>
    {
        // Lấy access token mà Google vừa cấp
        var accessToken = context.AccessToken;
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        // Tạo một HttpClient để gọi People API
        var request = new HttpRequestMessage(HttpMethod.Get, "https://people.googleapis.com/v1/people/me?personFields=birthdays");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            // Ghi log lỗi nếu cần
            // logger.LogError("Failed to call Google People API: {Reason}", response.ReasonPhrase);
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        using (var jsonDoc = JsonDocument.Parse(content))
        {
            var birthdays = jsonDoc.RootElement.TryGetProperty("birthdays", out var birthdaysElement) ? birthdaysElement.EnumerateArray().FirstOrDefault() : default;

            if (birthdays.ValueKind != JsonValueKind.Undefined)
            {
                var date = birthdays.TryGetProperty("date", out var dateElement) ? dateElement : default;
                if (date.ValueKind != JsonValueKind.Undefined)
                {
                    var year = date.TryGetProperty("year", out var yearElement) ? yearElement.GetInt32() : (int?)null;
                    var month = date.TryGetProperty("month", out var monthElement) ? monthElement.GetInt32() : (int?)null;
                    var day = date.TryGetProperty("day", out var dayElement) ? dayElement.GetInt32() : (int?)null;

                    if (year.HasValue && month.HasValue && day.HasValue)
                    {
                        // Tạo chuỗi ngày sinh theo định dạng "yyyy-MM-dd"
                        var birthdayString = $"{year}-{month:D2}-{day:D2}";
                        // Thêm claim "birthday" vào danh sách
                        context.Identity.AddClaim(new Claim("birthday", birthdayString));
                    }
                }
            }
        }
    };
});



// === KẾT THÚC CẤU HÌNH AUTHENTICATION ===
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/404");
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();


app.UseAuthorization();

app.MapControllerRoute(
    name: "ProductDetailWithHtml",
    // Pattern yêu cầu phải có đuôi .html
    // {slug} sẽ bắt tất cả mọi thứ phía trước ".html"
    pattern: "{id}.html",
    // Chỉ định cứng Controller và Action sẽ xử lý
    defaults: new { controller = "HangHoa", action = "Detail" },
    // Thêm ràng buộc để đảm bảo slug không phải là rỗng
    constraints: new { id = ".+" }
);
app.MapControllerRoute(
    name: "CuaHang",
    pattern: "/CuaHang",
    defaults: new { controller = "HangHoa", action = "Index" }

);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
//Db first đoạn dưới để cập nhật lại vào db(mo console tren tools-nuget)
//Scaffold-DbContext "Data Source=XUONGKIEN\MSSQLSERVER01;Initial Catalog=Hshop2023;Integrated Security=True;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Data -f

//cấu hình đê login bằng gg
//dotnet user-secrets init
//dotnet user-secrets set "Google:ClientId" "your-client-id"
//dotnet user-secrets set "Google:ClientSecret" "your-client-secret"