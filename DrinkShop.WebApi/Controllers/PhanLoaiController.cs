using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using DrinkShop.WebApi.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DrinkShop.Application.constance;

namespace DrinkShop.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhanLoaiController : ControllerBase
    {
        private readonly IPhanLoaiService _phanLoaiService;

        public PhanLoaiController(IPhanLoaiService phanLoaiService)
        {
            _phanLoaiService = phanLoaiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _phanLoaiService.GetAllAsync();
            return ResponseHelper.Success(data, "Lấy danh sách phân loại thành công");
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var phanLoai = await _phanLoaiService.GetByIdAsync(id);
            if (phanLoai == null)
                return ResponseHelper.Error("Không tìm thấy phân loại", 404);

            return ResponseHelper.Success(phanLoai, "Lấy thông tin phân loại thành công");
        }

        [HttpPost]
        [Authorize(Policy = "CanManageProduct")]
        public async Task<IActionResult> Create([FromBody] PhanLoai request)
        {
            if (!ModelState.IsValid)
                return ResponseHelper.Error("Dữ liệu không hợp lệ", 400);

            var created = await _phanLoaiService.CreateAsync(request);
            return ResponseHelper.Success(created, "Thêm phân loại thành công");
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "CanManageProduct")]
        public async Task<IActionResult> Update(int id, [FromBody] PhanLoai phanLoaiCapNhat)
        {
            if (!ModelState.IsValid)
                return ResponseHelper.Error("Dữ liệu không hợp lệ", 400);

            var updated = await _phanLoaiService.UpdateAsync(id, phanLoaiCapNhat);
            if (updated == null)
                return ResponseHelper.Error("Không tìm thấy phân loại để cập nhật", 404);

            return ResponseHelper.Success(updated, "Cập nhật phân loại thành công");
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "CanManageProduct")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _phanLoaiService.DeleteAsync(id);
            if (!deleted)
                return ResponseHelper.Error("Không tìm thấy phân loại để xóa", 404);

            return ResponseHelper.Deleted("Xóa phân loại thành công");
        }
    }
}