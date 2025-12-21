using Microsoft.EntityFrameworkCore;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using DrinkShop.Application.Interfaces;
using DrinkShop.Application.Helpers;
using DrinkShop.Application.DTO;

namespace DrinkShop.Application.Services
{
    public class DonHangService : IDonHangService
    {
        private readonly ApplicationDbContext _context;

        public DonHangService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. TẠO ĐƠN HÀNG (Logic Trừ Nguyên Liệu)
        // ==========================================================
        public async Task<DonHang> CreateOrderFromCartAsync(int userId, string pttt, int? voucherId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // B1: Lấy Giỏ hàng + Công Thức + Nguyên Liệu
                var cartItems = await _context.GioHangSanPhams
                    .Include(g => g.SanPham)
                        .ThenInclude(sp => sp.CongThucs)       // <--- Lấy công thức
                            .ThenInclude(ct => ct.NguyenLieu)  // <--- Lấy nguyên liệu
                    .Where(g => g.GioHang.IDTaiKhoan == userId)
                    .ToListAsync();

                if (cartItems == null || !cartItems.Any())
                    throw new Exception("Giỏ hàng trống!");

                // B2: DUYỆT TỪNG MÓN ĐỂ TRỪ KHO NGUYÊN LIỆU
                foreach (var item in cartItems)
                {
                    // Nếu món không có công thức (VD: bán chai nước suối nhập sẵn)
                    // Bạn có thể bỏ qua hoặc báo lỗi tùy nghiệp vụ. Ở đây mình báo lỗi cho chặt chẽ.
                    if (item.SanPham?.CongThucs == null || !item.SanPham.CongThucs.Any())
                    {
                        throw new Exception($"Sản phẩm '{item.SanPham.TenSanPham}' chưa có công thức pha chế, không thể tính tồn kho!");
                    }

                    // Duyệt từng thành phần trong công thức (Đường, Sữa, Trà...)
                    foreach (var ct in item.SanPham.CongThucs)
                    {
                        // Tổng lượng cần = (Định lượng 1 ly) * (Số ly khách đặt)
                        double tongCanDung = ct.SoLuongCan * item.SoLuong;
                        var nguyenLieu = ct.NguyenLieu;

                        // Kiểm tra tồn kho
                        if (nguyenLieu == null) throw new Exception($"Lỗi dữ liệu nguyên liệu cho món {item.SanPham.TenSanPham}");

                        // Chú ý: SoLuongTon trong DB của bạn là decimal? hay double?
                        // Ở đây mình ép kiểu về decimal để so sánh cho an toàn
                        decimal tonKhoHienTai = nguyenLieu.SoLuongTon ?? 0; 
                        decimal luongCan = (decimal)tongCanDung;

                        if (tonKhoHienTai < luongCan)
                        {
                            throw new Exception($"Nguyên liệu '{nguyenLieu.TenNguyenLieu}' không đủ. (Kho: {tonKhoHienTai}, Cần: {luongCan})");
                        }

                        // TRỪ KHO
                        nguyenLieu.SoLuongTon = tonKhoHienTai - luongCan;
                    }
                }

                // B3: Tính tiền (Giữ nguyên logic cũ)
                decimal tongTienHang = cartItems.Sum(item => item.SoLuong * item.SanPham.Gia);
                decimal giamGia = 0;
                decimal tongThanhToan = tongTienHang - giamGia;
                if (tongThanhToan < 0) tongThanhToan = 0;

                // B4: Tạo Đơn Hàng Master
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

                // B5: Tạo Chi Tiết Đơn
                var orderDetails = cartItems.Select(item => new DonHangSanPham
                {
                    IDDonHang = newOrder.IDDonHang,
                    IDSanPham = item.IDSanPham,
                    SoLuong = item.SoLuong,
                    GiaDonVi = item.SanPham.Gia
                }).ToList();

                _context.DonHangSanPhams.AddRange(orderDetails);

                // B6: Xóa Giỏ Hàng
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

        // ==========================================================
        // 2. HỦY ĐƠN HÀNG (Logic Hoàn Trả Nguyên Liệu)
        // ==========================================================
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

                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng"); // Đổi từ BusinessException sang Exception

                // So sánh trực tiếp với chuỗi trạng thái NVARCHAR
                if (order.TinhTrang != "Pending") 
                    throw new Exception("Chỉ được hủy đơn khi đang chờ xử lý");

                // 1️⃣ Update trạng thái sang NVARCHAR
                order.TinhTrang = "Cancelled";
                order.NgayCapNhat = DateTime.Now;

                // 2️⃣ Hoàn nguyên liệu
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
        public async Task<PagedList<OrderSummaryDto>> GetMyOrdersAsync(
            int userId, PaginationParams paginationParams)
        {
            var query = _context.DonHangs
                .AsNoTracking()
                .Where(o => o.IDTaiKhoan == userId)
                .OrderByDescending(o => o.NgayTao)
                .Select(o => new OrderSummaryDto
                {
                    OrderId = o.IDDonHang,
                    // Nếu TinhTrang bị null trong DB, gán là "Chưa xác định"
                    TinhTrang = o.TinhTrang ?? "Chờ xử lý", 
                    
                    // Nếu TrangThaiThanhToan bị null, gán là "Chưa rõ"
                    PaymentStatus = o.TrangThaiThanhToan ?? "Unpaid", 
                    
                    // Nếu TongTien bị null, gán là 0
                    TotalAmount = (decimal?)o.TongTien ?? 0m,
                        
                        // Nếu NgayTao là DateTime? thì dùng cái này:
                    CreatedAt = (DateTime?)o.NgayTao ?? DateTime.Now 
                });

            return await PagedList<OrderSummaryDto>.CreateAsync(
                query,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
        }

        public async Task<PagedList<AdminOrderDto>> GetAllOrdersAdminAsync(
            PaginationParams paginationParams, string? trangThai)
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
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng");

            // Tạm thời bỏ qua Validator nếu bạn chưa viết class OrderStatusValidator
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
                        
                        // Nếu NgayTao là DateTime? thì dùng cái này:
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