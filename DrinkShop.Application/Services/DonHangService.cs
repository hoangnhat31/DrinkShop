using Microsoft.EntityFrameworkCore;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.Helpers;
using DrinkShop.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class DonHangService : IDonHangService
    {
        private readonly ApplicationDbContext _context;

        public DonHangService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DonHang> CreateOrderFromCartAsync(int userId, string pttt, int? voucherId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cartItems = await _context.GioHangSanPhams
                    .Include(g => g.SanPham)
                        .ThenInclude(sp => sp.CongThucs)
                            .ThenInclude(ct => ct.NguyenLieu)
                    .Where(g => g.GioHang.IDTaiKhoan == userId)
                    .ToListAsync();

                if (cartItems == null || !cartItems.Any())
                    throw new Exception("Giỏ hàng trống!");

                foreach (var item in cartItems)
                {
                    if (item.SanPham?.CongThucs == null || !item.SanPham.CongThucs.Any())
                    {
                        throw new Exception($"Sản phẩm '{item.SanPham?.TenSanPham}' chưa có công thức pha chế!");
                    }

                    foreach (var ct in item.SanPham.CongThucs)
                    {
                        double tongCanDung = ct.SoLuongCan * item.SoLuong;
                        var nguyenLieu = ct.NguyenLieu;

                        if (nguyenLieu == null) throw new Exception($"Lỗi dữ liệu nguyên liệu cho món {item.SanPham.TenSanPham}");

                        decimal tonKhoHienTai = nguyenLieu.SoLuongTon ?? 0; 
                        decimal luongCan = (decimal)tongCanDung;

                        if (tonKhoHienTai < luongCan)
                        {
                            throw new Exception($"Nguyên liệu '{nguyenLieu.TenNguyenLieu}' không đủ. (Kho: {tonKhoHienTai}, Cần: {luongCan})");
                        }

                        nguyenLieu.SoLuongTon = tonKhoHienTai - luongCan;
                    }
                }

                decimal tongTienHang = cartItems.Sum(item => (decimal)item.SoLuong * (item.SanPham?.Gia ?? 0));
                decimal giamGia = 0;
                decimal tongThanhToan = tongTienHang - giamGia;
                if (tongThanhToan < 0) tongThanhToan = 0;

                var newOrder = new DonHang
                {
                    IDTaiKhoan = userId,
                    NgayTao = DateTime.Now,
                    TinhTrang = "Pending",
                    PTTT = pttt,
                    TongTien = tongThanhToan,
                    IDVoucher = voucherId
                };

                _context.DonHangs.Add(newOrder);
                await _context.SaveChangesAsync();

                var orderDetails = cartItems.Select(item => new DonHangSanPham
                {
                    IDDonHang = newOrder.IDDonHang,
                    IDSanPham = item.IDSanPham,
                    SoLuong = item.SoLuong,
                    GiaDonVi = item.SanPham?.Gia ?? 0
                }).ToList();

                _context.DonHangSanPhams.AddRange(orderDetails);
                _context.GioHangSanPhams.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return newOrder;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try 
            {
                var order = await _context.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(od => od.SanPham)
                            .ThenInclude(sp => sp.CongThucs)
                                .ThenInclude(ct => ct.NguyenLieu)
                    .FirstOrDefaultAsync(o => o.IDDonHang == orderId && o.IDTaiKhoan == userId);

                if (order == null) throw new Exception("Không tìm thấy đơn hàng");

                if (order.TinhTrang != "Pending") 
                    throw new Exception("Chỉ được hủy đơn khi đang chờ xử lý");

                order.TinhTrang = "Cancelled";
                order.NgayCapNhat = DateTime.Now;

                foreach (var item in order.ChiTietDonHangs)
                {
                    if (item.SanPham?.CongThucs == null) continue;

                    foreach (var recipe in item.SanPham.CongThucs)
                    {
                        var ingredient = recipe.NguyenLieu;
                        if (ingredient == null) continue;

                        decimal quantityToRestore = (decimal)(recipe.SoLuongCan * item.SoLuong);
                        ingredient.SoLuongTon = (ingredient.SoLuongTon ?? 0) + quantityToRestore;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception(ex.Message);
            }
        }

        public async Task<PagedList<OrderSummaryDto>> GetMyOrdersAsync(int userId, PaginationParams paginationParams)
        {
            var query = _context.DonHangs
                .AsNoTracking()
                .Where(o => o.IDTaiKhoan == userId)
                .OrderByDescending(o => o.NgayTao)
                .Select(o => new OrderSummaryDto
                {
                    OrderId = o.IDDonHang,
                    TinhTrang = o.TinhTrang ?? "Chờ xử lý", 
                    PaymentStatus = o.TrangThaiThanhToan ?? "Unpaid", 
                    TotalAmount = (decimal?)o.TongTien ?? 0m,
                    CreatedAt = (DateTime?)o.NgayTao ?? DateTime.Now 
                });

            return await PagedList<OrderSummaryDto>.CreateAsync(query, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<PagedList<AdminOrderDto>> GetAllOrdersAdminAsync(PaginationParams paginationParams, string? trangThai)
        {
            var query = _context.DonHangs
                .Include(o => o.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(o => o.TinhTrang == trangThai);

            query = query.OrderByDescending(o => o.NgayTao);

            return await PagedList<AdminOrderDto>.CreateAsync(
                query.Select(o => new AdminOrderDto
                {
                    OrderId = o.IDDonHang,
                    CustomerId = o.IDTaiKhoan,
                    CustomerName = o.TaiKhoan.HoTen,
                    CustomerPhone = o.TaiKhoan.SDT,
                    TinhTrang = o.TinhTrang,
                    PaymentStatus = o.TrangThaiThanhToan,
                    PaymentMethod = o.PTTT,
                    TotalAmount = (decimal?)o.TongTien ?? 0m,
                    CreatedAt = (DateTime?)o.NgayTao ?? DateTime.Now ,
                    UpdatedAt = o.NgayCapNhat
                }),
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.DonHangs.FindAsync(orderId);
            if (order == null) throw new Exception("Không tìm thấy đơn hàng");

            order.TinhTrang = status;
            order.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OrderDetailDto?> GetMyOrderByIdAsync(int userId, int orderId)
        {
            return await _context.DonHangs
                .Where(o => o.IDDonHang == orderId && o.IDTaiKhoan == userId)
                .Select(o => new OrderDetailDto
                {
                    OrderId = o.IDDonHang,
                    TinhTrang = o.TinhTrang,
                    PaymentStatus = o.TrangThaiThanhToan,
                    PaymentMethod = o.PTTT,
                    TotalAmount = (decimal?)o.TongTien ?? 0m,
                    CreatedAt = (DateTime?)o.NgayTao ?? DateTime.Now ,
                    Items = o.ChiTietDonHangs.Select(i => new OrderItemDto
                    {
                        IDSanPham = i.IDSanPham,
                        TenSanPham = i.SanPham.TenSanPham,
                        SoLuong = i.SoLuong,
                        GiaDonVi = i.GiaDonVi
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AdminOrderDto?> GetOrderByIdForAdminAsync(int orderId)
        {
            return await _context.DonHangs
                .Where(o => o.IDDonHang == orderId)
                .Select(o => new AdminOrderDto
                {
                    OrderId = o.IDDonHang,
                    CustomerId = o.IDTaiKhoan,
                    CustomerName = o.TaiKhoan.HoTen,
                    CustomerPhone = o.TaiKhoan.SDT,
                    TinhTrang = o.TinhTrang,
                    PaymentStatus = o.TrangThaiThanhToan,
                    PaymentMethod = o.PTTT,
                    TotalAmount = (decimal?)o.TongTien ?? 0m,
                    CreatedAt = (DateTime?)o.NgayTao ?? DateTime.Now ,
                    UpdatedAt = o.NgayCapNhat,
                    Items = o.ChiTietDonHangs.Select(i => new OrderItemDto
                    {
                        IDSanPham = i.IDSanPham,
                        TenSanPham = i.SanPham.TenSanPham,
                        SoLuong = i.SoLuong,
                        GiaDonVi = i.GiaDonVi
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}