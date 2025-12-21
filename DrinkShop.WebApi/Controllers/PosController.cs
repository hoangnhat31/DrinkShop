using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.DTO;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.constance; // Import file chứa Permissions
using System.Threading.Tasks;
using System;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // 1. SỬA QUYỀN: Áp dụng Policy "POS_CREATE_ORDER"
    [Authorize(Policy = Permissions.Pos.CreateOrder)]
    public class PosController : ControllerBase
    {
        private readonly IPosService _posService;

        public PosController(IPosService posService)
        {
            _posService = posService;
        }

        // POST: api/pos/create-order
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] PosCreateOrderDto request)
        {
            try
            {
                // 2. ĐÃ XÓA LOGIC LẤY ID NHÂN VIÊN TỪ TOKEN
                // Vì chỉ có 1 tài khoản Staff duy nhất, ta truyền cứng ID của tài khoản đó (ví dụ: 1)
                // Hoặc truyền 0 nếu bên trong Service bạn đã gán cứng MaNhanVien
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