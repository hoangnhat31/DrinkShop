using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.DTO;
using DrinkShop.Application.constance; // Import file Permissions
using System;
using System.Threading.Tasks;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongKeController : ControllerBase
    {
        private readonly IThongKeService _service;

        // Inject Service vào Controller
        public ThongKeController(IThongKeService service)
        {
            _service = service;
        }

        // ==========================================
        // 1. THỐNG KÊ DOANH THU
        // ==========================================
        // GET: api/thongke/revenue?type=day
        [HttpGet("revenue")]
        // Yêu cầu quyền: STATISTIC_VIEW_REVENUE
        [Authorize(Policy = Permissions.Statistic.ViewRevenue)] 
        public async Task<IActionResult> GetRevenue(
            [FromQuery] string type = "day", 
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var result = await _service.GetRevenueStatisticsAsync(type, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 2. THỐNG KÊ SẢN PHẨM BÁN CHẠY
        // ==========================================
        // GET: api/thongke/top-products?n=5
        [HttpGet("top-products")]
        // Yêu cầu quyền: STATISTIC_VIEW_TOP_PRODUCTS
        [Authorize(Policy = Permissions.Statistic.ViewTopProducts)]
        public async Task<IActionResult> GetTopProducts([FromQuery] int n = 5)
        {
            try
            {
                var result = await _service.GetTopSellingProductsAsync(n);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 3. THỐNG KÊ ĐÁNH GIÁ (SAO)
        // ==========================================
        // GET: api/thongke/ratings
        [HttpGet("ratings")]
        // Yêu cầu quyền: STATISTIC_VIEW_RATING
        [Authorize(Policy = Permissions.Statistic.ViewRating)]
        public async Task<IActionResult> GetRatingStats()
        {
            try
            {
                var result = await _service.GetRatingDistributionAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}