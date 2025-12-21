using DrinkShop.Application.Interfaces;
using DrinkShop.Infrastructure;
using DrinkShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Application.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Voucher>> GetAllAsync()
        {
            return await _context.Vouchers
                .OrderByDescending(v => v.IDVoucher)
                .ToListAsync();
        }

        public async Task<Voucher?> GetByIdAsync(int id)
        {
            return await _context.Vouchers.FindAsync(id);
        }

        public async Task<Voucher?> GetByDescriptionAsync(string mota)
        {
            return await _context.Vouchers
                .FirstOrDefaultAsync(v => v.MoTa != null && v.MoTa.Contains(mota));
        }

        public async Task<Voucher> CreateAsync(Voucher voucher)
        {
            voucher.SoLuongConLai = voucher.SoLuong; 

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<Voucher?> UpdateAsync(int id, Voucher voucher)
        {
            var existing = await _context.Vouchers.FindAsync(id);
            if (existing == null) return null;

            existing.MoTa = voucher.MoTa;
            existing.GiamGia = voucher.GiamGia;
            existing.ToiDa = voucher.ToiDa;
            existing.BatDau = voucher.BatDau;
            existing.KetThuc = voucher.KetThuc;
            existing.DieuKienMin = voucher.DieuKienMin;
            existing.SoLuong = voucher.SoLuong;
            existing.SoLuongConLai = voucher.SoLuongConLai; 

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var v = await _context.Vouchers.FindAsync(id);
            if (v == null) return false;

            _context.Vouchers.Remove(v);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}