using DrinkShop.Application.Helpers;
using DrinkShop.Application.Interfaces;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DrinkShop.Application.constance.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class SanPhamService : ISanPhamService
    {
        private readonly ApplicationDbContext _context;

        public SanPhamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SanPhamResponse?> GetSanPhamById(int id)
        {
            var sp = await _context.SanPhams
                .Include(p => p.PhanLoai)
                .FirstOrDefaultAsync(p => p.IDSanPham == id);

            if (sp == null) return null;

            var reviews = await _context.DanhGias
                .Where(d => d.IDSanPham == id)
                .Select(d => d.SoSao)
                .ToListAsync();

            double diemTrungBinh = reviews.Any() ? reviews.Average() : 0;

            return new SanPhamResponse
            {
                IDSanPham = sp.IDSanPham,
                TenSanPham = sp.TenSanPham,
                Gia = sp.Gia,
                MoTa = sp.MoTa ?? string.Empty, 
                ImageUrl = sp.ImageUrl,
                DiemDanhGia = Math.Round(diemTrungBinh, 1),
                SoLuongDanhGia = reviews.Count
            };
        }

        public async Task<SanPham?> GetOriginalSanPhamById(int id)
        {
            return await _context.SanPhams.FindAsync(id);
        }

        public async Task<PagedList<SanPham>> GetSanPhams(PaginationParams paginationParams, string? tenSanPham, int? idPhanLoai)
        {
            var query = _context.SanPhams.AsQueryable();

            if (!string.IsNullOrEmpty(tenSanPham))
            {
                query = query.Where(sp => sp.TenSanPham != null && sp.TenSanPham.Contains(tenSanPham));
            }
            
            if (idPhanLoai.HasValue && idPhanLoai > 0)
            {
                query = query.Where(sp => sp.IDPhanLoai == idPhanLoai);
            }

            return await PagedList<SanPham>.CreateAsync(query, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task AddSanPham(SanPham sanPham)
        {
            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSanPham(SanPham sanPham)
        {
            _context.Entry(sanPham).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSanPham(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham != null)
            {
                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();
            }
        }
    }
}