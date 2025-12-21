using DrinkShop.Application.DTO;
using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Nhớ using cái này cho List
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class PosService : IPosService
    {
        private readonly ApplicationDbContext _context;

        public PosService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PosOrderReceiptDto> CreateAndPayPosOrderAsync(PosCreateOrderDto request, int staffId)
        {
            // 1. Kiểm tra đầu vào
            if (request.Items == null || !request.Items.Any())
                throw new Exception("Đơn hàng phải có ít nhất 1 sản phẩm.");

            // 2. Tính toán tổng tiền
            decimal totalAmount = 0;
            var orderDetails = new List<DonHangSanPham>();

            foreach (var item in request.Items)
            {
                // Lưu ý: DTO dùng 'IDSanPham', Entity SanPham dùng 'Id' (hoặc IDSanPham tùy bạn khai báo)
                var product = await _context.SanPhams.FindAsync(item.IDSanPham);
                if (product == null) throw new Exception($"Sản phẩm ID {item.IDSanPham} không tồn tại.");

                totalAmount += product.Gia * item.SoLuong; // DTO vẫn dùng .Quantity

                // MAP SANG ENTITY (QUAN TRỌNG: Dùng tên Tiếng Việt của Entity)
                orderDetails.Add(new DonHangSanPham
                {
                    IDSanPham = item.IDSanPham,
                    SoLuong = item.SoLuong,    // Map Quantity (DTO) -> SoLuong (Entity)
                    GiaDonVi = product.Gia      // Map Price -> DonGia (Entity)
                });
            }

            // 3. Xử lý thanh toán
            string paymentStatus = "Pending"; 
            decimal changeAmount = 0;       

            if (request.PaymentMethod.ToUpper() == "CASH")
            {
                if (request.AmountReceived < totalAmount)
                    throw new Exception($"Khách đưa thiếu tiền. Tổng: {totalAmount}, Đưa: {request.AmountReceived}");
                
                changeAmount = request.AmountReceived - totalAmount;
                paymentStatus = "Paid"; 
            }
            else 
            {
                request.AmountReceived = totalAmount; 
                paymentStatus = "Paid"; 
            }
            int guestId = 1;
            // 4. Tạo đơn hàng
            var order = new DonHang
            {
                
                IDTaiKhoan = guestId,       
                
                NgayTao = DateTime.Now,
                TongTien = totalAmount,
                TinhTrang = paymentStatus == "Paid" ? "Completed" : "Pending",
                PTTT = request.PaymentMethod,
                GhiChu = request.Note ?? "Đơn tại quầy (POS)", // Map Note -> GhiChu
                
                ChiTietDonHangs = orderDetails
            };

            _context.DonHangs.Add(order);
            await _context.SaveChangesAsync();

            // 5. Trả về kết quả
            return new PosOrderReceiptDto
            {
                OrderId = order.IDDonHang,
                TotalAmount = totalAmount,
                AmountReceived = request.AmountReceived,
                ChangeAmount = changeAmount,
                PaymentStatus = paymentStatus,
                CreatedAt = order.NgayTao ?? DateTime.Now,
            };
        }
    }
}