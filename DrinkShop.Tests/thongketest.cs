using DrinkShop.Application.DTO;
using DrinkShop.Application.Services;
using DrinkShop.Domain.Interfaces;
using DrinkShop.Domain.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using DrinkShop.Application.Interfaces;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using DrinkShop.Application.DTO;

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
            var data = new List<RevenueResult> { new() { TimeLabel = "2025-12-28", Revenue = 100, OrderCount = 5 } };
            _mockRepo.Setup(r => r.GetRevenueByDay(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

            var result = await _service.GetRevenueStatisticsAsync("day", null, null);

            Assert.Single(result);
            Assert.Equal("2025-12-28", result[0].Period);
            _mockRepo.Verify(r => r.GetRevenueByDay(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetRevenue_ByMonth_Success()
        {
            var data = new List<RevenueResult> { new() { TimeLabel = "12/2025", Revenue = 500 } };
            _mockRepo.Setup(r => r.GetRevenueByMonth(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

            var result = await _service.GetRevenueStatisticsAsync("month", null, null);

            Assert.Equal("12/2025", result[0].Period);
            _mockRepo.Verify(r => r.GetRevenueByMonth(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetRevenue_ByYear_Default()
        {
            var data = new List<RevenueResult> { new() { TimeLabel = "2025", Revenue = 1000 } };
            _mockRepo.Setup(r => r.GetRevenueByYear()).ReturnsAsync(data);

            var result = await _service.GetRevenueStatisticsAsync("other", null, null);

            Assert.Equal("2025", result[0].Period);
            _mockRepo.Verify(r => r.GetRevenueByYear(), Times.Once);
        }

        [Fact]
        public async Task GetTopProducts_Success()
        {
            var data = new List<ProductResult> { new() { ProductName = "Coffee", SoLuong = 10, TotalAmount = 200 } };
            _mockRepo.Setup(r => r.GetTopProducts(5)).ReturnsAsync(data);

            var result = await _service.GetTopSellingProductsAsync(5);

            Assert.Single(result);
            Assert.Equal("Coffee", result[0].ProductName);
        }

        [Fact]
        public async Task GetRatings_Success()
        {
            var data = new List<RatingResult> { new() { Star = 5, Count = 100 } };
            _mockRepo.Setup(r => r.GetRatingStats()).ReturnsAsync(data);

            var result = await _service.GetRatingDistributionAsync();

            Assert.Equal(5, result[0].StarRating);
            Assert.Equal(100, result[0].Count);
        }
    }
}