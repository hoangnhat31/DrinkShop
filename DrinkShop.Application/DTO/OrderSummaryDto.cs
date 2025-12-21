namespace DrinkShop.Application.DTO
{
public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public string TinhTrang { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Unpaid";
    public decimal TotalAmount { get; set; } 
    public DateTime CreatedAt { get; set; }  
}
}
