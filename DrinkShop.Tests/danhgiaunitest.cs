using DrinkShop.Application.Services;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DrinkShop.Tests
{
    public class DanhGiaServiceTests
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
        public async Task AddReview_Success()
        {
            var context = GetContext();
            var service = new DanhGiaService(context);
            var order = new DonHang { 
                IDTaiKhoan = 1, 
                TinhTrang = "Completed",
                ChiTietDonHangs = new List<DonHangSanPham> { new DonHangSanPham { IDSanPham = 10 } }
            };
            context.Set<DonHang>().Add(order);
            await context.SaveChangesAsync();

            var result = await service.AddReviewAsync(1, 10, 5, "Ngon");

            Assert.NotNull(result);
            Assert.Equal(5, result.SoSao);
            Assert.True(context.Set<DanhGia>().Any(d => d.IDSanPham == 10));
        }

        [Fact]
        public async Task AddReview_NotPurchased_Throws()
        {
            var context = GetContext();
            var service = new DanhGiaService(context);

            var order = new DonHang { 
                IDTaiKhoan = 1, 
                TinhTrang = "Pending",
                ChiTietDonHangs = new List<DonHangSanPham> { new DonHangSanPham { IDSanPham = 10 } }
            };
            context.Set<DonHang>().Add(order);
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() => service.AddReviewAsync(1, 10, 5, "Ngon"));
        }

        [Fact]
        public async Task AddReview_Duplicate_Throws()
        {
            var context = GetContext();
            var service = new DanhGiaService(context);

            context.Set<DanhGia>().Add(new DanhGia { IDTaiKhoan = 1, IDSanPham = 10, SoSao = 4 });
            context.Set<DonHang>().Add(new DonHang { 
                IDTaiKhoan = 1, TinhTrang = "Completed", 
                ChiTietDonHangs = new List<DonHangSanPham> { new DonHangSanPham { IDSanPham = 10 } }
            });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() => service.AddReviewAsync(1, 10, 5, "Ngon"));
        }

        [Fact]
        public async Task GetSummary_CalculateCorrectly()
        {
            var context = GetContext();
            var service = new DanhGiaService(context);

            var user = new TaiKhoan { IDTaiKhoan = 1, HoTen = "Anh Nhật" };
            var product = new SanPham { IDSanPham = 10, TenSanPham = "Trà sữa" };
            
            context.Set<TaiKhoan>().Add(user);
            context.Set<SanPham>().Add(product);

            context.Set<DanhGia>().AddRange(
                new DanhGia { IDTaiKhoan = 1, IDSanPham = 10, SoSao = 5, ThoiGianTao = DateTime.Now },
                new DanhGia { IDTaiKhoan = 1, IDSanPham = 10, SoSao = 4, ThoiGianTao = DateTime.Now }
            );
            await context.SaveChangesAsync();

            var result = await service.GetProductReviewSummaryAsync(10);

         
            Assert.Equal(2, result.TotalReviews); 
            Assert.Equal(4.5, result.AverageRating);
        }
    }
}