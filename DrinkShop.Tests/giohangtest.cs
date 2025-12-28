using DrinkShop.Application.Services;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DrinkShop.Tests
{
    public class GioHangServiceTests
    {
        private ApplicationDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task AddToCart_New_Success()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.Add(new SanPham { IDSanPham = 1, TenSanPham = "Tea", Gia = 10 });
            await context.SaveChangesAsync();

            var result = await service.AddToCartAsync(1, 1, 2);

            Assert.NotNull(result);
            Assert.Single(result.GioHangSanPhams);
            Assert.Equal(2, result.GioHangSanPhams.First().SoLuong);
        }

        [Fact]
        public async Task AddToCart_ExistItem_UpdateQty()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.Add(new SanPham { IDSanPham = 1, TenSanPham = "Tea" });
            await service.AddToCartAsync(1, 1, 2);

            var result = await service.AddToCartAsync(1, 1, 3);

            Assert.Equal(5, result.GioHangSanPhams.First().SoLuong);
        }

        [Fact]
        public async Task AddToCart_NoProduct_Throws()
        {
            var context = GetContext();
            var service = new GioHangService(context);

            await Assert.ThrowsAsync<Exception>(() => service.AddToCartAsync(1, 999, 1));
        }

        [Fact]
        public async Task UpdateQty_Valid_Success()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.Add(new SanPham { IDSanPham = 1, TenSanPham = "Tea" });
            await service.AddToCartAsync(1, 1, 2);

            var result = await service.UpdateQuantityAsync(1, 1, 10);

            Assert.Equal(10, result.GioHangSanPhams.First().SoLuong);
        }

        [Fact]
        public async Task GetByUserId_Success()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.Add(new SanPham { IDSanPham = 1, TenSanPham = "Tea" });
            await service.AddToCartAsync(1, 1, 2);

            var result = await service.GetByUserIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.IDTaiKhoan);
        }

        [Fact]
        public async Task RemoveItem_Success()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.Add(new SanPham { IDSanPham = 1, TenSanPham = "Tea" });
            await service.AddToCartAsync(1, 1, 2);

            var result = await service.RemoveFromCartAsync(1, 1);

            Assert.True(result);
            var cart = await service.GetByUserIdAsync(1);
            Assert.Empty(cart.GioHangSanPhams);
        }

        [Fact]
        public async Task ClearCart_Success()
        {
            var context = GetContext();
            var service = new GioHangService(context);
            context.SanPhams.AddRange(
                new SanPham { IDSanPham = 1, TenSanPham = "Tea" },
                new SanPham { IDSanPham = 2, TenSanPham = "Milk" }
            );
            await service.AddToCartAsync(1, 1, 1);
            await service.AddToCartAsync(1, 2, 1);

            await service.ClearCartAsync(1);

            var cart = await service.GetByUserIdAsync(1);
            Assert.Empty(cart.GioHangSanPhams);
        }
    }
}