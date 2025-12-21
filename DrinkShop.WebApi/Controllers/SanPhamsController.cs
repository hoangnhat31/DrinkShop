using DrinkShop.Application.Helpers;
using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using DrinkShop.Application.constance.Response;
using Microsoft.AspNetCore.Http;

[Route("api/[controller]")]
[ApiController]
public class SanPhamsController : ControllerBase
{
    private readonly ISanPhamService _sanPhamService;
    private readonly IConfiguration _configuration;

    public SanPhamsController(ISanPhamService sanPhamService, IConfiguration configuration)
    {
        _sanPhamService = sanPhamService;
        _configuration = configuration;
    }

    private SanPham ApplyPlaceholder(SanPham sanPham)
    {
        if (sanPham == null) return null;
        var defaultImageUrl = _configuration["AppSettings:DefaultProductImageUrl"];
        if (string.IsNullOrEmpty(sanPham.ImageUrl)) sanPham.ImageUrl = defaultImageUrl;
        return sanPham;
    }

    private SanPhamResponse ApplyPlaceholder(SanPhamResponse sanPham)
    {
        if (sanPham == null) return null;
        var defaultImageUrl = _configuration["AppSettings:DefaultProductImageUrl"];
        if (string.IsNullOrEmpty(sanPham.ImageUrl)) sanPham.ImageUrl = defaultImageUrl;
        return sanPham;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetSanPhams([FromQuery] PaginationParams paginationParams, [FromQuery] string? tenSanPham, [FromQuery] int? IDPhanLoai)
    {
        var pagedList = await _sanPhamService.GetSanPhams(paginationParams, tenSanPham, IDPhanLoai);

        foreach (var sanPham in pagedList.Items)
        {
            ApplyPlaceholder(sanPham);
        }

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
        {
            pagedList.CurrentPage,
            pagedList.TotalPages,
            pagedList.PageSize,
            pagedList.TotalCount
        }));

        return Ok(pagedList);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSanPham(int id)
    {
        var sanPhamDto = await _sanPhamService.GetSanPhamById(id);
        
        if (sanPhamDto == null) return NotFound();
        
        return Ok(ApplyPlaceholder(sanPhamDto));
    }

    [Authorize(Policy = "CanManageProduct")]
    [HttpPost]
    public async Task<IActionResult> PostSanPham(SanPham sanPham)
    {
        await _sanPhamService.AddSanPham(sanPham);
        return CreatedAtAction(nameof(GetSanPham), new { id = sanPham.IDSanPham }, sanPham);
    }

    [Authorize(Policy = "CanManageProduct")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSanPham(int id, SanPham sanPham)
    {
        if (id != sanPham.IDSanPham) return BadRequest("ID không khớp");
        await _sanPhamService.UpdateSanPham(sanPham);
        return NoContent();
    }

    [Authorize(Policy = "CanManageProduct")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSanPham(int id)
    {
        await _sanPhamService.DeleteSanPham(id);
        return NoContent();
    }

    [Authorize(Policy = "CanManageProduct")]
    [HttpPost("{id}/image")]
    public async Task<IActionResult> UploadImage(
        int id,
        IFormFile file,
        [FromServices] IFileStorageService fileStorageService)
    {
        if (file == null || file.Length == 0) return BadRequest("File không hợp lệ");

        var sanPham = await _sanPhamService.GetOriginalSanPhamById(id);
        if (sanPham == null) return NotFound("Không tìm thấy sản phẩm");

        if (!string.IsNullOrEmpty(sanPham.ImageUrl))
        {
            try
            {
                var uri = new Uri(sanPham.ImageUrl);
                var oldFileName = Path.GetFileName(uri.LocalPath); 
                await fileStorageService.DeleteFileAsync(oldFileName);
            }
            catch { }
        }

        try
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            using var stream = file.OpenReadStream();
            var url = await fileStorageService.UploadFileAsync(stream, uniqueFileName, file.ContentType);

            sanPham.ImageUrl = url;
            await _sanPhamService.UpdateSanPham(sanPham);

            return Ok(new { message = "Upload ảnh thành công", imageUrl = url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi upload file: {ex.Message}");
        }
    }
}