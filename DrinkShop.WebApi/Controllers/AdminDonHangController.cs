using DrinkShop.Application.Helpers;
using DrinkShop.Application.Interfaces;
using DrinkShop.WebApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DrinkShop.Application.constance;
using System.Threading.Tasks;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Policy = Permissions.Order.ViewAll)]
    public class AdminDonHangController : ControllerBase
    {
        private readonly IDonHangService _donHangService;

        public AdminDonHangController(IDonHangService donHangService)
        {
            _donHangService = donHangService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? trangThai)
        {
            var pagedOrders = await _donHangService.GetAllOrdersAdminAsync(paginationParams, trangThai);

            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
            {
                pagedOrders.CurrentPage,
                pagedOrders.TotalPages,
                pagedOrders.PageSize,
                pagedOrders.TotalCount
            }));

            return ResponseHelper.Success(pagedOrders, "Lấy danh sách đơn hàng thành công");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _donHangService.GetOrderByIdForAdminAsync(id);

            if (order == null)
                return ResponseHelper.Error("Không tìm thấy đơn hàng", 404);

            return ResponseHelper.Success(order, "Lấy chi tiết đơn hàng thành công");
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromBody] UpdateOrderStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewStatus))
                return ResponseHelper.Error("Trạng thái mới (NewStatus) là bắt buộc", 400);

            var success = await _donHangService.UpdateOrderStatusAsync(id, request.NewStatus);

            if (!success)
                return ResponseHelper.Error("Không tìm thấy đơn hàng", 404);

            return ResponseHelper.Success<object>(null, "Cập nhật trạng thái đơn hàng thành công");
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
    }
}