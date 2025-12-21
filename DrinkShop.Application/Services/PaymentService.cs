using DrinkShop.Application.Interfaces;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DrinkShop.Application.constance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DonHang?> CreatePaymentAsync(int userId, int orderId, string pttt)
        {
            var order = await _context.DonHangs
                .FirstOrDefaultAsync(o => o.IDDonHang == orderId && o.IDTaiKhoan == userId);

            if (order == null) return null;

            if (order.TinhTrang != "Pending" && order.TinhTrang != "Chờ thanh toán")
            {
                throw new Exception("Đơn hàng đã được xử lý, không thể thay đổi thanh toán.");
            }

            if (order.TongTien < 0) throw new ArgumentException("Tổng tiền không hợp lệ!");

            order.PTTT = pttt;
            order.TinhTrang = "Chờ xác nhận"; 

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<DonHang?> ConfirmPaymentAsync(int orderId)
        {
            var order = await _context.DonHangs.FindAsync(orderId);
            if (order == null) return null;

            order.TinhTrang = "Đã thanh toán";
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<DonHang?> CancelPaymentAsync(int orderId, int userId)
        {
            var order = await _context.DonHangs
                 .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                        .ThenInclude(sp => sp.CongThucs)
                            .ThenInclude(c => c.NguyenLieu)
                .FirstOrDefaultAsync(d => d.IDDonHang == orderId);

            if (order == null) return null;

            if (order.IDTaiKhoan != userId)
            {
                throw new Exception("Bạn không có quyền hủy đơn hàng này!");
            }

            if (order.TinhTrang == "Đã giao" || order.TinhTrang == "Completed")
            {
                throw new Exception("Đơn hàng đã hoàn tất, không thể hủy.");
            }

            var timeSinceCreated = DateTime.Now - order.NgayTao;
            if (timeSinceCreated > TimeSpan.FromMinutes(15) && order.TinhTrang == "Chờ thanh toán")
            {
                order.TinhTrang = "Đã hủy (Quá hạn thanh toán)";
            }
            else
            {
                order.TinhTrang = "Đã hủy";
            }

            foreach (var chiTiet in order.ChiTietDonHangs)
            {
                if (chiTiet.SanPham?.CongThucs != null)
                {
                    foreach (var congThuc in chiTiet.SanPham.CongThucs)
                    {
                        if (congThuc.NguyenLieu != null)
                        {
                            double luongHoanLai = congThuc.SoLuongCan * chiTiet.SoLuong;
                            congThuc.NguyenLieu.SoLuongTon = (congThuc.NguyenLieu.SoLuongTon ?? 0) + (decimal)luongHoanLai;
                        }
                    }
                }
            }

            if (order.IDVoucher.HasValue)
            {
                var voucher = await _context.Vouchers.FindAsync(order.IDVoucher.Value);
                if (voucher != null)
                {
                    voucher.SoLuongConLai += 1;
                }
            }

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<IEnumerable<DonHang>> GetPaymentHistoryAsync(int userId)
        {
            return await _context.DonHangs
                .Where(o => o.IDTaiKhoan == userId)
                .OrderByDescending(o => o.NgayTao)
                .ToListAsync();
        }
    }
}