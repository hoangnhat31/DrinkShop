using DrinkShop.Application.Services;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrinkShop.Tests
{
    public class PhanLoaiServiceTests
    {
        private ApplicationDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new ApplicationDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact] 
        public async Task CreateAsync_ShouldReturnCreatedPhanLoai()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new PhanLoaiService(context);
            var newCategory = new PhanLoai { Ten = "Trà Trái Cây", MoTa = "Các loại trà tươi" };

            // Act
            var result = await service.CreateAsync(newCategory);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IDPhanLoai > 0);
            Assert.Equal("Trà Trái Cây", result.Ten);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllItems()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new PhanLoaiService(context);
            context.PhanLoais.AddRange(
                new PhanLoai { Ten = "Loại 1" },
                new PhanLoai { Ten = "Loại 2" }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingRecord()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new PhanLoaiService(context);
            var category = new PhanLoai { Ten = "Tên Cũ", MoTa = "Mô tả cũ" };
            context.PhanLoais.Add(category);
            await context.SaveChangesAsync();

            var updateData = new PhanLoai { Ten = "Tên Mới", MoTa = "Mô tả mới" };

            // Act
            var result = await service.UpdateAsync(category.IDPhanLoai, updateData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tên Mới", result.Ten);
            Assert.Equal("Mô tả mới", result.MoTa);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new PhanLoaiService(context);
            var category = new PhanLoai { Ten = "Xóa tôi" };
            context.PhanLoais.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteAsync(category.IDPhanLoai);

            // Assert
            Assert.True(result);
            Assert.Null(await context.PhanLoais.FindAsync(category.IDPhanLoai));
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_IfFound()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new PhanLoaiService(context);
            var category = new PhanLoai { Ten = "Tồn tại" };
            context.PhanLoais.Add(category);
            await context.SaveChangesAsync();

            // Act & Assert
            Assert.True(await service.ExistsAsync(category.IDPhanLoai));
            Assert.False(await service.ExistsAsync(999)); // ID không tồn tại
        }
    }
}