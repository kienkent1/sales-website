using System;
using System.Collections.Generic;

namespace project.Data;

public partial class Loai
{
    public int MaLoai { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? TenLoaiAlias { get; set; }

    public string? MoTa { get; set; }

    public string? Hinh { get; set; }

    public  string? Slug { get; set; }
    public bool? Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
