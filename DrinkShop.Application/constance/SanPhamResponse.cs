namespace DrinkShop.Application.constance.Response 
{
    public class SanPhamResponse
    {
        public int IDSanPham { get; set; }

        public string TenSanPham { get; set; } = string.Empty; 

        public decimal Gia { get; set; }
        public string? MoTa { get; set; }
        public string? ImageUrl { get; set; }
        public int? IDPhanLoai { get; set; }

        public double DiemDanhGia { get; set; }
        public int SoLuongDanhGia { get; set; }
    }
}