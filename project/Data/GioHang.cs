using System;
using System.Collections.Generic;

namespace project.Data;

public partial class GioHang
{
    public int MaGh { get; set; }

    public string MaKh { get; set; } = null!;

    public int SoLuong { get; set; }

    public int MaHh { get; set; }

    public virtual HangHoa MaHhNavigation { get; set; } = null!;

    public virtual KhachHang MaKhNavigation { get; set; } = null!;
}
