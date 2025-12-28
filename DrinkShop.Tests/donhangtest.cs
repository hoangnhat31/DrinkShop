using DrinkShop.Application.Services;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
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
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateOrder_Success()
        {
            var context = GetContext();
            var service = new DonHangService(context);

            var nl = new NguyenLieu { IDNguyenLieu = 1, TenNguyenLieu = "Sugar", SoLuongTon = 100 };
            var sp = new SanPham { 
                IDSanPham = 1, 
                TenSanPham = "Tea", 
                Gia = 20, 
                CongThucs = new List<CongThuc> { new CongThuc { SoLuongCan = 10, NguyenLieu = nl } } 
            };
            var gh = new GioHang { 
                IDTaiKhoan = 1, 
                GioHangSanPhams = new List<GioHangSanPham> { new GioHangSanPham { IDSanPham = 1, SoLuong = 2 } } 
            };

            context.NguyenLieus.Add(nl);
            context.SanPhams.Add(sp);
            context.GioHangs.Add(gh);
            await context.SaveChangesAsync();

            var result = await service.CreateOrderFromCartAsync(1, "COD", null);

            Assert.NotNull(result);
            Assert.Equal(80, nl.SoLuongTon);
            Assert.Empty(context.GioHangSanPhams.Where(x => x.GioHang.IDTaiKhoan == 1));
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

        [Fact]
        public async Task CancelOrder_WrongStatus_Throws()
        {
            var context = GetContext();
            var service = new DonHangService(context);
            var order = new DonHang { IDDonHang = 1, IDTaiKhoan = 1, TinhTrang = "Completed" };

            context.DonHangs.Add(order);
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() => service.CancelOrderAsync(1, 1));
        }

        [Fact]
        public async Task UpdateStatus_Success()
        {
            var context = GetContext();
            var service = new DonHangService(context);
            var order = new DonHang { IDDonHang = 1, TinhTrang = "Pending" };

            context.DonHangs.Add(order);
            await context.SaveChangesAsync();

            await service.UpdateOrderStatusAsync(1, "Done");

            Assert.Equal("Done", order.TinhTrang);
        }
    }
}