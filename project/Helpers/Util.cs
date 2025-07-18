using System.Text;

namespace project.Helpers
{
    public class Util
    {
        public static string UploadHinh(IFormFile Hinh, string folder)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), 
                    "wwwroot","Hinh", folder, Hinh.FileName);
                using (var myfile= new FileStream(fullPath, FileMode.CreateNew) )
            {
                Hinh.CopyTo(myfile);
            }
                return Hinh.FileName;
            }
            catch (Exception ex)
            {

               return string.Empty;
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
    }
}
