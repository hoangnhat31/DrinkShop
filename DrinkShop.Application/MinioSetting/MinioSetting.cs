

namespace DrinkShop.Application.Settings;

public class MinioSetting
{
    // Giữ nguyên khớp với JSON
    public string? Endpoint { get; set; } 
    public string? AccessKey { get; set; } 
    public string? SecretKey { get; set; }

    public string? Bucket { get; set; } 

    public bool UseSSL { get; set; } = false;
}