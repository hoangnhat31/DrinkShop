using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

[Route("api/[controller]")]
[ApiController]
public class DanhGiaController : ControllerBase
{
    private readonly IDanhGiaService _danhGiaService;

    public DanhGiaController(IDanhGiaService danhGiaService)
    {
        _danhGiaService = danhGiaService;
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetReviews(int productId)
    {
        var result = await _danhGiaService.GetProductReviewSummaryAsync(productId);
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    public async Task<IActionResult> PostReview([FromBody] CreateReviewRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("Vui lòng đăng nhập.");

        try 
        {
            var review = await _danhGiaService.AddReviewAsync(
                int.Parse(userIdClaim), 
                request.IdSanPham, 
                request.SoSao, 
                request.BinhLuan
            );
            return Ok(new { success = true, data = review });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class CreateReviewRequest 
{
    public int IdSanPham { get; set; }
    public int SoSao { get; set; }
    public string BinhLuan { get; set; } = string.Empty;
}