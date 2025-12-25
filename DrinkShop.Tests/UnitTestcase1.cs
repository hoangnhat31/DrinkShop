using Xunit;
using Microsoft.EntityFrameworkCore;
using DrinkShop.Domain.Entities; // Dòng này cực kỳ quan trọng
using DrinkShop.Application.Services;
using DrinkShop.Infrastructure;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace DrinkShop.Tests
{
    public class VoucherServiceTests
    {
        // Hàm tiện ích để tạo DbContext giả lập trong bộ nhớ
        private async Task<ApplicationDbContext> GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            var databaseContext = new ApplicationDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task CreateAsync_ShouldSetSoLuongConLai_AndSaveToDb()
        {
            // Arrange
            var context = await GetDatabaseContext();
            var service = new VoucherService(context);
            var newVoucher = new Voucher { 
                MoTa = "Giam 50k", 
                SoLuong = 100, 
                GiamGia = 50000 
            };

            // Act
            var result = await service.CreateAsync(newVoucher);

            // Assert
            Assert.Equal(100, result.SoLuongConLai); // Kiểm tra logic gán số lượng còn lại
            Assert.Equal(1, await context.Vouchers.CountAsync());
        }

        [Fact]
        public async Task GetByDescriptionAsync_ShouldReturnCorrectVoucher()
        {
            // Arrange
            var context = await GetDatabaseContext();
            context.Vouchers.Add(new Voucher { IDVoucher = 1, MoTa = "Khuyen mai He" });
            context.Vouchers.Add(new Voucher { IDVoucher = 2, MoTa = "Tet Nguyen Dan" });
            await context.SaveChangesAsync();
            var service = new VoucherService(context);

            // Act
            var result = await service.GetByDescriptionAsync("He");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.IDVoucher);
            Assert.Contains("He", result.MoTa);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenIdNotFound()
        {
            // Arrange
            var context = await GetDatabaseContext();
            var service = new VoucherService(context);

            // Act
            var result = await service.DeleteAsync(999); // ID không tồn tại

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFieldsCorrectly()
        {
            // Arrange
            var context = await GetDatabaseContext();
            var voucher = new Voucher { IDVoucher = 1, MoTa = "Cu", GiamGia = 10 };
            context.Vouchers.Add(voucher);
            await context.SaveChangesAsync();
            
            var service = new VoucherService(context);
            var updatedData = new Voucher { MoTa = "Moi", GiamGia = 20 };

            // Act
            var result = await service.UpdateAsync(1, updatedData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Moi", result.MoTa);
            Assert.Equal(20, result.GiamGia);
        }
    }
}