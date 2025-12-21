using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.Interfaces;
using DrinkShop.WebApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using DrinkShop.Application.constance;
using System;
using System.Security.Claims;
using DrinkShop.WebApi.DTO;
using DrinkShop.Application.DTO;
using DrinkShop.Application.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace DrinkShop.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DonHangController : ControllerBase
    {
        private readonly IDonHangService _donHangService;

        public DonHangController(IDonHangService donHangService)
        {
            _donHangService = donHangService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("IDTaiKhoan") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromQuery] string pttt = PaymentMethod.COD,
            [FromQuery] int? voucherId = null)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized("Token không hợp lệ hoặc không tìm thấy User ID.");

                if (!PaymentMethod.List.Contains(pttt))
                {
                    return ResponseHelper.Error($"Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: {string.Join(", ", PaymentMethod.List)}", 400);
                }

                var order = await _donHangService.CreateOrderFromCartAsync(userId, pttt, voucherId);

                return ResponseHelper.Success(order, "Tạo đơn hàng thành công");
            }
            catch (Exception ex)
            {
                return ResponseHelper.Error(ex.Message, 400);
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] PaginationParams paginationParams)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var orders = await _donHangService.GetMyOrdersAsync(userId, paginationParams);

            return ResponseHelper.Success(orders, "Lấy danh sách đơn hàng thành công");
        }

        [HttpGet("my-orders/{orderId}")]
        public async Task<IActionResult> GetMyOrderById(int orderId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var order = await _donHangService.GetMyOrderByIdAsync(userId, orderId);

            if (order == null)
                return ResponseHelper.Error("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập", 404);

            return ResponseHelper.Success(order, "Lấy chi tiết đơn hàng thành công");
        }

        [HttpPut("my-orders/{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var result = await _donHangService.CancelOrderAsync(orderId, userId);
                return ResponseHelper.Success(result, "Đã hủy đơn hàng thành công");
            }
            catch (Exception ex)
            {
                return ResponseHelper.Error(ex.Message, 400);
            }
        }
    }
}