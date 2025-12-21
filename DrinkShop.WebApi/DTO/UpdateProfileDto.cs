using System.ComponentModel.DataAnnotations;

namespace DrinkShop.WebApi.DTO.Auth
{
    public class UpdateProfileDto
    {
        [MaxLength(100, ErrorMessage = "Họ tên quá dài")]
        public string? HoTen { get; set; } // Cho phép null để người dùng không gửi field này nếu không muốn sửa

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SDT { get; set; }

        public string? DiaChi { get; set; }
    }
}