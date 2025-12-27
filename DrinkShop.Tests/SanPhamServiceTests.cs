using DrinkShop.Application.Helpers;
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
    public class SanPhamServiceTests
    {
        // Hàm trợ giúp khởi tạo Database trong RAM (InMemory)
        private ApplicationDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new ApplicationDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        #region 1. TEST LOGIC NGHIỆP VỤ & ĐÁNH GIÁ (RATING)
        [Fact]
        public async Task GetSanPhamById_ShouldCalculateAverageRatingCorrectly()
        {
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);
            var sanPhamId = 1;

            context.SanPhams.Add(new SanPham { IDSanPham = sanPhamId, TenSanPham = "Trà Sữa", Gia = 30000 });
            
            context.DanhGias.AddRange(
                new DanhGia { IDSanPham = sanPhamId, SoSao = 5 },
                new DanhGia { IDSanPham = sanPhamId, SoSao = 3 }
            );
            await context.SaveChangesAsync();

            var result = await service.GetSanPhamById(sanPhamId);

            Assert.NotNull(result);
            Assert.Equal(4.0, result.DiemDanhGia);
            Assert.Equal(2, result.SoLuongDanhGia);
        }
        #endregion

        #region 2. TEST QUẢN LÝ ẢNH (IMAGE URL)
        [Fact]
        public async Task AddSanPham_ShouldStoreImageUrlCorrectly()
        {
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);
            var testUrl = "http://minio:9000/drinkshop/trasua.jpg";
            var sanPham = new SanPham { TenSanPham = "Trà sữa", Gia = 35000, ImageUrl = testUrl };

            await service.AddSanPham(sanPham);

            var storedSp = await context.SanPhams.FirstOrDefaultAsync(p => p.TenSanPham == "Trà sữa");
            Assert.NotNull(storedSp);
            Assert.Equal(testUrl, storedSp.ImageUrl);
        }
        #endregion

        #region 3. TEST PHÂN TRANG (PAGINATION)
        [Fact]
        public async Task GetSanPhams_ShouldReturnCorrectPageSize()
        {
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);
            for (int i = 1; i <= 10; i++)
            {
                context.SanPhams.Add(new SanPham { IDSanPham = i, TenSanPham = $"Sản phẩm {i}", Gia = 10000 });
            }
            await context.SaveChangesAsync();

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 4 };

            var result = await service.GetSanPhams(pagination, null, null);

            // ĐÃ SỬA: Truy cập qua .Items.Count (Vì PagedList chứa danh sách trong property Items)
            Assert.Equal(4, result.Items.Count); 
            Assert.Equal(10, result.TotalCount);
        }
        #endregion

        #region 4. TEST LỌC DỮ LIỆU (FILTERING)
        [Fact]
        public async Task GetSanPhams_ShouldFilterByNameAndCategory()
        {
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);
            context.SanPhams.AddRange(
                new SanPham { TenSanPham = "Cà phê đen", IDPhanLoai = 1, Gia = 20000 },
                new SanPham { TenSanPham = "Trà đào", IDPhanLoai = 2, Gia = 25000 }
            );
            await context.SaveChangesAsync();
            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

            var result = await service.GetSanPhams(pagination, "Cà phê", 1);

            // ĐÃ SỬA: Dùng Assert.Single trên result.Items và truy cập result.Items[0]
            Assert.Single(result.Items);
            Assert.Equal("Cà phê đen", result.Items[0].TenSanPham);
        }
        #endregion

        #region 5. TEST CRUD CƠ BẢN
        [Fact]
        public async Task DeleteSanPham_ShouldRemoveProductFromDatabase()
        {
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);
            var sp = new SanPham { IDSanPham = 99, TenSanPham = "SP Test Xóa", Gia = 0 };
            context.SanPhams.Add(sp);
            await context.SaveChangesAsync();

            await service.DeleteSanPham(99);

            var checkSp = await context.SanPhams.FindAsync(99);
            Assert.Null(checkSp);
        }
        #endregion
    }
}