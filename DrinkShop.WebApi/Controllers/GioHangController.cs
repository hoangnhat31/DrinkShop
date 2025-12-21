using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DrinkShop.Application.Interfaces;
using DrinkShop.WebApi.Utilities;
using System.Security.Claims;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GioHangController : ControllerBase
    {
        private readonly IGioHangService _gioHangService;

        public GioHangController(IGioHangService gioHangService)
        {
            _gioHangService = gioHangService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            var cart = await _gioHangService.AddToCartAsync(userId, request.IDSanPham, request.SoLuong);
            
            return ResponseHelper.Success(cart, "Thêm sản phẩm vào giỏ hàng thành công");
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try 
            {
                var idClaim = User.FindFirst("IDTaiKhoan") ?? User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                
                if (idClaim == null) 
                {
                    return Unauthorized(new { success = false, message = "Không xác định được User. Vui lòng đăng nhập lại." });
                }

                var userId = int.Parse(idClaim.Value);
                var cart = await _gioHangService.GetByUserIdAsync(userId);

                if (cart == null || cart.GioHangSanPhams == null || !cart.GioHangSanPhams.Any())
                {
                    return ResponseHelper.Success(new List<object>(), "Giỏ hàng trống");
                }

                var result = cart.GioHangSanPhams.Select(item => new 
                {
                    idSanPham = item.IDSanPham,
                    tenSanPham = item.SanPham?.TenSanPham ?? "Sản phẩm không tồn tại",
                    gia = item.SanPham?.Gia ?? 0,
                    imageUrl = item.SanPham?.ImageUrl ?? "",
                    soLuong = item.SoLuong
                }).ToList();

                return ResponseHelper.Success(result, "Lấy giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromCart(int IDSanPham)
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            var result = await _gioHangService.RemoveFromCartAsync(userId, IDSanPham);

            if (!result)
                return ResponseHelper.Error("Không tìm thấy sản phẩm trong giỏ hàng", 404);

            return ResponseHelper.Success<object?>(null, "Xóa sản phẩm khỏi giỏ hàng thành công");
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity(int IDSanPham, int soLuongMoi)
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            var cart = await _gioHangService.UpdateQuantityAsync(userId, IDSanPham, soLuongMoi);

            if (cart == null)
                return ResponseHelper.Error("Không tìm thấy sản phẩm trong giỏ hàng", 404);

            return ResponseHelper.Success(cart, "Cập nhật số lượng sản phẩm thành công");
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            await _gioHangService.ClearCartAsync(userId);
            return ResponseHelper.Success<object?>(null, "Đã xóa toàn bộ giỏ hàng");
        }
    }
}

public class AddToCartRequest
{
    public int IDSanPham { get; set; }
    public int SoLuong { get; set; }
}