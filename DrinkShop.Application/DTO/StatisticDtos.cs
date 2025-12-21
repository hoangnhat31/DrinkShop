using System;

namespace DrinkShop.Application.DTO
{
    public class RevenueStatDto
    {
        public string? Period { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }

    public class ProductStatDto
    {
        public string? ProductName { get; set; }
        public int SoLuong { get; set; }
        public decimal TotalRevenueFromProduct { get; set; }
    }

    public class RatingStatDto
    {
        public int StarRating { get; set; }
        public int Count { get; set; }
    }
}