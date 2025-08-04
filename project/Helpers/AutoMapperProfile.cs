using AutoMapper;
using Microsoft.CodeAnalysis.Options;
using NuGet.Protocol;
using project.Controllers;
using project.Data;
using project.ViewModels;

namespace project.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RegisterVM, KhachHang>();
            //.ForMember(kh => kh.HoTen, option => option.MapFrom(vm => vm.HoTen))
            //.ReverseMap();
            CreateMap<ContactVM, GopY>();

            CreateMap<ProductVM, HangHoa>()
                //doan này sẽ ánh xạ từ ProductVM sang HangHoa va no se khong tim cac thuộc tính nào trong HangHoa
                .ForMember(dest => dest.MaHh, opt => opt.Ignore()) // Ignore MaHh when creating a new product
                .ForMember(dest => dest.MaNccNavigation, opt => opt.Ignore()) // Ignore navigation property
                .ForMember(dest => dest.MaLoaiNavigation, opt => opt.Ignore()) // Ignore navigation property
             .ForMember(dest => dest.Hinh, opt => opt.Ignore());

            CreateMap<HangHoa, ProductVM>()


           .ForMember(dest => dest.MaHangHoa, opt => opt.MapFrom(src => src.MaHh))
            .ForMember(dest => dest.Hinh, opt => opt.Ignore()) // Handle file upload separately
            .ForMember(dest => dest.HinhUrl, opt => opt.MapFrom(src => src.Hinh));

            CreateMap<LoaiVM, Loai>();
            CreateMap<NhaCungCapVM, NhaCungCap>();
            CreateMap<UserVM, KhachHang>()
                .ForMember(dest => dest.MatKhau, opt => opt.Ignore())
            // Báo cho AutoMapper: "Đừng bao giờ động đến thuộc tính RandomKey của đối tượng đích"
            .ForMember(dest => dest.RandomKey, opt => opt.Ignore())
            // (Tùy chọn) Bỏ qua cả việc map ảnh, vì chúng ta sẽ xử lý nó thủ công
            .ForMember(dest => dest.Hinh, opt => opt.Ignore());

            CreateMap<GioHang, CartItem>()
                .ForMember(dest => dest.TenHangHoa, opt => opt.MapFrom(src => src.MaHhNavigation.TenHh))
                .ForMember(dest => dest.DonGia, opt => opt.MapFrom(src => src.MaHhNavigation.DonGia ?? 0)) 
                .ForMember(dest => dest.HinhAnh, opt => opt.MapFrom(src => src.MaHhNavigation.Hinh ?? string.Empty))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.MaHhNavigation.Slug));
        }
    }
    
}
