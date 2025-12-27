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
            // Arrange
            var context = GetDatabaseContext();
            var service = new SanPhamService(context);

            var phanLoai = new PhanLoai { Ten = "Trà Sữa Danh Mục" };
            context.PhanLoais.Add(phanLoai);
            await context.SaveChangesAsync();

            var sanPham = new SanPham { 
                TenSanPham = "Trà Sữa", 
                Gia = 30000, 
                IDPhanLoai = phanLoai.IDPhanLoai 
            };
            context.SanPhams.Add(sanPham);
            await context.SaveChangesAsync();

            var sanPhamId = sanPham.IDSanPham;

            // Thêm đánh giá
            context.DanhGias.AddRange(
                new DanhGia { IDSanPham = sanPhamId, SoSao = 5 },
                new DanhGia { IDSanPham = sanPhamId, SoSao = 3 }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetSanPhamById(sanPhamId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4.0, result.DiemDanhGia); // (5+3)/2
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

            // Kiểm tra qua thuộc tính .Items của PagedList
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

            var pl1 = new PhanLoai { Ten = "Cà phê" };
            var pl2 = new PhanLoai { Ten = "Trà" };
            context.PhanLoais.AddRange(pl1, pl2);
            await context.SaveChangesAsync();

            context.SanPhams.AddRange(
                new SanPham { TenSanPham = "Cà phê đen", IDPhanLoai = pl1.IDPhanLoai, Gia = 20000 },
                new SanPham { TenSanPham = "Trà đào", IDPhanLoai = pl2.IDPhanLoai, Gia = 25000 }
            );
            await context.SaveChangesAsync();

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

            // Act: Lọc theo tên "Cà phê" và ID của pl1
            var result = await service.GetSanPhams(pagination, "Cà phê", pl1.IDPhanLoai);

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
            var sp = new SanPham { TenSanPham = "SP Test Xóa", Gia = 0 };
            context.SanPhams.Add(sp);
            await context.SaveChangesAsync();

            var idToDelete = sp.IDSanPham;

            await service.DeleteSanPham(idToDelete);

            var checkSp = await context.SanPhams.FindAsync(idToDelete);
            Assert.Null(checkSp);
        }
        #endregion
    }
}