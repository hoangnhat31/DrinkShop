using DrinkShop.Application.DTO;
using DrinkShop.Application.Services;
using DrinkShop.Domain.Interfaces;
using DrinkShop.Domain.Models; 
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DrinkShop.Tests
{
    public class ThongKeServiceTests
    {
        private readonly Mock<IStatisticRepository> _mockRepo;
        private readonly ThongKeService _service;

        public ThongKeServiceTests()
        {
            _mockRepo = new Mock<IStatisticRepository>();
            _service = new ThongKeService(_mockRepo.Object);
        }

        [Fact]
        public async Task GetRevenue_ByDay_Success()
        {
            var data = new List<RevenueResult> { 
                new RevenueResult { TimeLabel = "2025-12-28", Revenue = 100000, OrderCount = 5 } 
            };
            _mockRepo.Setup(r => r.GetRevenueByDay(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                     .ReturnsAsync(data);

            var result = await _service.GetRevenueStatisticsAsync("day", null, null);

            Assert.Single(result);
            Assert.Equal("2025-12-28", result[0].Period);
        }

        [Fact]
        public async Task GetTopProducts_Success()
        {
            var data = new List<ProductStatResult> { 
                new ProductStatResult { ProductName = "Trà Đào", SoLuong = 50, TotalAmount = 1500000 } 
            };
            _mockRepo.Setup(r => r.GetTopProducts(It.IsAny<int>()))
                     .ReturnsAsync(data);

            var result = await _service.GetTopSellingProductsAsync(5);

            Assert.Single(result);
            Assert.Equal("Trà Đào", result[0].ProductName);
            Assert.Equal(50, result[0].SoLuong);
        }

        [Fact]
        public async Task GetRatings_Success()
        {
            var data = new List<RatingStatResult> { 
                new RatingStatResult { Star = 5, Count = 20 } 
            };
            _mockRepo.Setup(r => r.GetRatingStats())
                     .ReturnsAsync(data);

            var result = await _service.GetRatingDistributionAsync();

            Assert.Single(result);
            Assert.Equal(5, result[0].StarRating);
            Assert.Equal(20, result[0].Count);
        }
    }
}