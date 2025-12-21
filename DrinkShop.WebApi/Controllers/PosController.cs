using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.DTO;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.constance;
using System.Threading.Tasks;
using System;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.Pos.CreateOrder)]
    public class PosController : ControllerBase
    {
        private readonly IPosService _posService;

        public PosController(IPosService posService)
        {
            _posService = posService;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] PosCreateOrderDto request)
        {
            try
            {
                int fixedStaffId = 1; 
                var receipt = await _posService.CreateAndPayPosOrderAsync(request, fixedStaffId);

                return Ok(new 
                { 
                    message = "Tạo đơn thành công!", 
                    data = receipt 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}