// DrinkShop.Application/Settings/MinioSetting.cs

namespace DrinkShop.Application.Settings;

// Đây là lớp để ánh xạ phần cấu hình từ appsettings.json
public class MinioSetting
{
    // Giữ nguyên khớp với JSON
    public string? Endpoint { get; set; } 
    public string? AccessKey { get; set; } 
    public string? SecretKey { get; set; }

    // SỬA: Đổi từ BucketName thành Bucket (để khớp với key "Bucket" trong JSON)
    public string? Bucket { get; set; } 

    public bool UseSSL { get; set; } = false;
}