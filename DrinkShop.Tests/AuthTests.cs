using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using DrinkShop.WebApi.Controllers;
using DrinkShop.WebApi.DTO.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DrinkShop.Tests
{
    public class AuthTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IFileStorageService> _mockFileStorage;

        public AuthTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockFileStorage = new Mock<IFileStorageService>();
            
            // Giả lập JWT Secret cho token generation
            _mockConfig.Setup(c => c["JWT_SECRET"]).Returns("super_secret_key_for_testing_purposes_only_12345");
        }

        private ApplicationDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            
            // Seed VaiTro ID=3 cho kiểm thử đăng ký
            context.VaiTros.Add(new VaiTro { IDVaiTro = 3, TenVaiTro = "KhachHang" });
            context.SaveChanges();
            
            return context;
        }

        [Fact]
        public async Task Register_Success_ShouldCreateUser()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AuthController(context, _mockConfig.Object, _mockFileStorage.Object);
            var request = new RegisterDto { HoTen = "Test User", Email = "test@gmail.com", MatKhau = "123456" };

            // Act
            var result = await controller.Register(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(result); // Tùy vào ResponseHelper trả về gì
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(await context.TaiKhoans.AnyAsync(u => u.Email == "test@gmail.com"));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShouldReturnError()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.TaiKhoans.Add(new TaiKhoan { Email = "duplicate@gmail.com", HoTen = "Old", MatKhau = "..." });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _mockConfig.Object, _mockFileStorage.Object);
            var request = new RegisterDto { Email = "duplicate@gmail.com", MatKhau = "123456" };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequest = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var context = GetDatabaseContext();
            var password = "password123";
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new TaiKhoan { Email = "login@test.com", MatKhau = hashed, IDVaiTro = 3, HoTen = "User" };
            context.TaiKhoans.Add(user);
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _mockConfig.Object, _mockFileStorage.Object);
            var loginRequest = new LoginRequest { TaiKhoan = "login@test.com", MatKhau = password };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ForgotPassword_ValidEmail_ShouldSetResetToken()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.TaiKhoans.Add(new TaiKhoan { Email = "forgot@test.com", MatKhau = "..." });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _mockConfig.Object, _mockFileStorage.Object);

            // Act
            var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "forgot@test.com" });

            // Assert
            var user = await context.TaiKhoans.FirstAsync(u => u.Email == "forgot@test.com");
            Assert.NotNull(user.ResetToken);
            Assert.NotNull(user.ResetTokenExpire);
        }
    }
}