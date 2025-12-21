using DrinkShop.Application.Interfaces;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class GioHangService : IGioHangService
    {
        private readonly ApplicationDbContext _context;

        public GioHangService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GioHang?> UpdateQuantityAsync(int userId, int sanPhamId, int soLuongMoi)
        {
            var cart = await _context.GioHangs
                .Include(g => g.GioHangSanPhams)
                .FirstOrDefaultAsync(g => g.IDTaiKhoan == userId);

            if (cart == null) return null;

            var item = cart.GioHangSanPhams.FirstOrDefault(x => x.IDSanPham == sanPhamId);
            if (item == null) return null;

            item.SoLuong = soLuongMoi;
            await _context.SaveChangesAsync();

            return cart;
        }

        public async Task<GioHang?> GetByUserIdAsync(int userId)
        {
            return await _context.GioHangs
                .Include(g => g.GioHangSanPhams)
                .ThenInclude(x => x.SanPham)
                .FirstOrDefaultAsync(g => g.IDTaiKhoan == userId);
        }

        public async Task<GioHang> AddToCartAsync(int userId, int sanPhamId, int soLuong)
        {
            var productExists = await _context.SanPhams.AnyAsync(p => p.IDSanPham == sanPhamId);
            if (!productExists)
            {
                throw new Exception($"Sản phẩm có ID {sanPhamId} không tồn tại hoặc đã bị xóa!");
            }

            var cart = await _context.GioHangs
                .Include(g => g.GioHangSanPhams)
                .FirstOrDefaultAsync(g => g.IDTaiKhoan == userId);

            if (cart == null)
            {
                cart = new GioHang 
                { 
                    IDTaiKhoan = userId,
                    GioHangSanPhams = new List<GioHangSanPham>() 
                };
                _context.GioHangs.Add(cart);
                await _context.SaveChangesAsync(); 
            }

            var existingItem = cart.GioHangSanPhams.FirstOrDefault(x => x.IDSanPham == sanPhamId);

            if (existingItem != null)
            {
                existingItem.SoLuong += soLuong;
                _context.Entry(existingItem).State = EntityState.Modified; 
            }
            else
            {
                var newItem = new GioHangSanPham
                {
                    IDGioHang = cart.IDGioHang,
                    IDSanPham = sanPhamId,
                    SoLuong = soLuong
                };
                _context.GioHangSanPhams.Add(newItem); 
            }

            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int sanPhamId)
        {
            var cart = await _context.GioHangs
                .Include(g => g.GioHangSanPhams)
                .FirstOrDefaultAsync(g => g.IDTaiKhoan == userId);

            if (cart == null) return false;

            var item = cart.GioHangSanPhams.FirstOrDefault(x => x.IDSanPham == sanPhamId);
            if (item == null) return false;

            _context.GioHangSanPhams.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(int userId)
        {
            var cart = await _context.GioHangs
                .Include(g => g.GioHangSanPhams)
                .FirstOrDefaultAsync(g => g.IDTaiKhoan == userId);

            if (cart != null)
            {
                _context.GioHangSanPhams.RemoveRange(cart.GioHangSanPhams);
                await _context.SaveChangesAsync();
            }
        }
    }
}