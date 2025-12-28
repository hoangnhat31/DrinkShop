using DrinkShop.Application.Services;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; // Thêm thư viện này
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DrinkShop.Tests
{
    public class DonHangServiceTests
    {
        private ApplicationDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateOrder_Success()
        {
            var context = GetContext();
            var service = new DonHangService(context);

            var nl = new NguyenLieu { IDNguyenLieu = 1, TenNguyenLieu = "Sữa", SoLuongTon = 100 };
            var sp = new SanPham { 
                IDSanPham = 1, 
                TenSanPham = "Trà sữa", 
                Gia = 30000, 
                CongThucs = new List<CongThuc> { new CongThuc { SoLuongCan = 10, NguyenLieu = nl } } 
            };
            var gh = new GioHang { 
                IDTaiKhoan = 1, 
                GioHangSanPhams = new List<GioHangSanPham> { new GioHangSanPham { IDSanPham = 1, SoLuong = 2 } } 
            };

            context.Set<NguyenLieu>().Add(nl);
            context.Set<SanPham>().Add(sp);
            context.Set<GioHang>().Add(gh);
            await context.SaveChangesAsync();

            var result = await service.CreateOrderFromCartAsync(1, "COD", null);

            Assert.NotNull(result);
            Assert.Equal(80, nl.SoLuongTon); 
        }

        [Fact]
        public async Task CreateOrder_NoStock_Throws()
        {
            var context = GetContext();
            var service = new DonHangService(context);

            var nl = new NguyenLieu { IDNguyenLieu = 1, SoLuongTon = 5 };
            var sp = new SanPham { 
                IDSanPham = 1, 
                CongThucs = new List<CongThuc> { new CongThuc { SoLuongCan = 10, NguyenLieu = nl } } 
            };
            var gh = new GioHang { 
                IDTaiKhoan = 1, 
                GioHangSanPhams = new List<GioHangSanPham> { new GioHangSanPham { IDSanPham = 1, SoLuong = 1 } } 
            };

            context.AddRange(nl, sp, gh);
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() => service.CreateOrderFromCartAsync(1, "COD", null));
        }

        [Fact]
        public async Task CancelOrder_Success()
        {
            var context = GetContext();
            var service = new DonHangService(context);

            var nl = new NguyenLieu { IDNguyenLieu = 1, SoLuongTon = 50 };
            var sp = new SanPham { 
                IDSanPham = 1, 
                CongThucs = new List<CongThuc> { new CongThuc { SoLuongCan = 5, NguyenLieu = nl } } 
            };
            var order = new DonHang { 
                IDDonHang = 1, 
                IDTaiKhoan = 1, 
                TinhTrang = "Pending", 
                ChiTietDonHangs = new List<DonHangSanPham> { new DonHangSanPham { SanPham = sp, SoLuong = 2 } } 
            };

            context.AddRange(nl, sp, order);
            await context.SaveChangesAsync();

            var result = await service.CancelOrderAsync(1, 1);

            Assert.True(result);
            Assert.Equal("Cancelled", order.TinhTrang);
            Assert.Equal(60, nl.SoLuongTon); 
        }
    }
}