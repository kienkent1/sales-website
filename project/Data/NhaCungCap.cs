using System;
using System.Collections.Generic;

namespace project.Data;

public partial class NhaCungCap
{
    public string MaNcc { get; set; } = null!;

    public string TenCongTy { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string? NguoiLienLac { get; set; }

    public string Email { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? DiaChi { get; set; }

    public string? MoTa { get; set; }

    public string? Slug { get; set; }

    public bool? Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }
    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
