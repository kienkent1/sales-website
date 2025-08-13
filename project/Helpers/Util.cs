using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;


namespace project.Helpers
{
    public class Util
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public Util(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadImage(IFormFile file, string TenFolder)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Hinh", TenFolder);
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return uniqueFileName;
        }
        public async Task<string> DownloadAndSaveImageAsync(string imageUrl, string TenFolder)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Tải dữ liệu ảnh về dưới dạng một mảng byte
                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

                    // 1. SỬ DỤNG CÙNG CẤU TRÚC THƯ MỤC
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Hinh", TenFolder);
                    Directory.CreateDirectory(uploadsFolder); // Đảm bảo thư mục tồn tại

                    // 2. TẠO TÊN FILE DUY NHẤT (vì không có tên file gốc, ta tạo một tên an toàn)
                    string uniqueFileName = $"{Guid.NewGuid()}_google_avatar.jpg";

                    // Đường dẫn đầy đủ để lưu file
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu mảng byte thành một file ảnh
                    await File.WriteAllBytesAsync(filePath, imageBytes);

                    // 3. TRẢ VỀ CHỈ TÊN FILE (giống hệt UploadImage)
                    return uniqueFileName;
                }
            }
            catch (Exception ex)
            {
                return null; 
            }
        }
        public static string GenerateRandomKey(int length=5)
        {
            var chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_,.;:'`~";
            var sb=new StringBuilder();
            var rd = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[rd.Next(0,chars.Length)]);
            }
            return  sb.ToString();
        }
        public static string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return "";
            }

            // 1. Chuyển thành chữ thường và chuẩn hóa Unicode để loại bỏ dấu tiếng Việt
            string str = phrase.ToLower().Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in str)
            {
                // Lọc bỏ các ký tự dấu (non-spacing marks)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            str = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // 2. Xử lý ký tự 'đ'
            str = str.Replace("đ", "d");

            // 3. Thay thế TẤT CẢ các ký tự không phải chữ hoặc số bằng dấu gạch ngang
            //    Điều này sẽ xử lý các ký tự bạn yêu cầu: (), ", ', /, | và nhiều hơn nữa
            str = Regex.Replace(str, @"[^a-z0-9]", "-");

            // 4. Thay thế nhiều dấu gạch ngang liên tiếp bằng một dấu gạch ngang duy nhất
            str = Regex.Replace(str, @"-+", "-");

            // 5. Xóa các dấu gạch ngang ở đầu và cuối chuỗi (nếu có)
            str = str.Trim('-');

            return str;
        }
        public static string GenerateAlias(int? id, string Loai)
        {
            if (string.IsNullOrEmpty(Loai))
            {
                return "";
            }

            string chuoiDaChuanHoa = Loai.Normalize(System.Text.NormalizationForm.FormD);

            string tenKhongDau = Regex.Replace(chuoiDaChuanHoa, @"\p{M}", string.Empty);

            string tenKhongDauDaSua = tenKhongDau.Replace("đ", "d").Replace("Đ", "d");

            string alias = tenKhongDauDaSua.ToLower().Replace(" ", "-");

            alias = Regex.Replace(alias, @"[^a-z0-9-]", "");

            if (id.HasValue)
            {
                return $"{id.Value}-{alias}";
            }

            return alias;

        }
        //pagination
       

    }
   
}
