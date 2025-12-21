using Microsoft.AspNetCore.Mvc;
using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using DrinkShop.WebApi.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        private string? ValidateVoucher(Voucher voucher)
        {
            if (voucher.GiamGia <= 0 || voucher.GiamGia > 100)
                return "Phần trăm giảm giá phải từ 1 đến 100.";

            if (voucher.SoLuong < 0)
                return "Tổng số lượng voucher không được âm.";

            if (voucher.DieuKienMin < 0)
                return "Điều kiện đơn tối thiểu không được âm.";

            if (voucher.BatDau >= voucher.KetThuc)
                return "Ngày kết thúc phải sau ngày bắt đầu.";

            return null; 
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _voucherService.GetAllAsync();
            return ResponseHelper.Success(vouchers, "Lấy danh sách voucher thành công");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _voucherService.GetByIdAsync(id);
            if (voucher == null)
                return ResponseHelper.Error("Không tìm thấy voucher", 404);

            return ResponseHelper.Success(voucher, "Lấy thông tin voucher thành công");
        }

        [HttpPost]
        [Authorize(Policy = "CanManageVoucher")]
        public async Task<IActionResult> Create([FromBody] Voucher voucher)
        {
            var error = ValidateVoucher(voucher);
            if (error != null) return ResponseHelper.Error(error, 400);

            try 
            {
                var result = await _voucherService.CreateAsync(voucher);
                return ResponseHelper.Success(result, "Tạo voucher thành công");
            }
            catch (Exception ex)
            {
                return ResponseHelper.Error(ex.Message, 500);
            }
        }

        [Authorize(Policy = "CanManageVoucher")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Voucher voucher)
        {
            var error = ValidateVoucher(voucher);
            if (error != null) return ResponseHelper.Error(error, 400);

            try
            {
                var result = await _voucherService.UpdateAsync(id, voucher);
                
                if (result == null)
                    return ResponseHelper.Error("Không tìm thấy voucher để cập nhật", 404);

                return ResponseHelper.Success(result, "Cập nhật voucher thành công");
            }
            catch (Exception ex)
            {
                return ResponseHelper.Error(ex.Message, 500);
            }
        }

        [Authorize(Policy = "CanManageVoucher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _voucherService.DeleteAsync(id);
            if (!result)
                return ResponseHelper.Error("Không tìm thấy voucher", 404);

            return ResponseHelper.Deleted("Xóa voucher thành công");
        }
    }
}