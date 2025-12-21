using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.DTO;
using DrinkShop.Application.constance;
using System;
using System.Threading.Tasks;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongKeController : ControllerBase
    {
        private readonly IThongKeService _service;

        public ThongKeController(IThongKeService service)
        {
            _service = service;
        }

        [HttpGet("revenue")]
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

        [HttpGet("top-products")]
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

        [HttpGet("ratings")]
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