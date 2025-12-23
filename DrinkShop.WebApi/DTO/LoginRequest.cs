namespace DrinkShop.WebApi.DTO.Auth
{
    public class LoginRequest
    {
        public string TaiKhoan { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string? RefreshToken { get; set; } 
    }
}
