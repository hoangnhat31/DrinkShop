using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;
using DrinkShop.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

public class FileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly bool _useSSL;

    public FileStorageService(IOptions<MinioSetting> minioOptions)
    {
        var settings = minioOptions.Value;

        // 1. Validate ngay lập tức để tránh lỗi ngầm
        if (string.IsNullOrEmpty(settings.Endpoint) || string.IsNullOrEmpty(settings.AccessKey))
        {
            throw new ArgumentException("MinIO Config bị thiếu! Kiểm tra lại appsettings.json");
        }

        _bucketName = settings.Bucket;
        _endpoint = settings.Endpoint;
        _useSSL = settings.UseSSL;

        _minioClient = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSSL)
            .Build();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        fileStream.Position = 0;

        // Kiểm tra và tạo bucket nếu chưa có
        bool exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName)
        );
        if (!exists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
            // Lưu ý: Nếu muốn ảnh xem được public, bạn cần set Policy trên MinIO Console hoặc qua code
        }

        // Upload file
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType));

        // 2. Tạo URL trả về chuẩn xác (tự động http/https)
        var protocol = _useSSL ? "https" : "http";
        
        // Kết quả: http://localhost:9000/drinkshop/avatars/user123.jpg
        return $"{protocol}://{_endpoint}/{_bucketName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileName)
    {
        // Logic xử lý URL -> Object Name
        if (fileName.StartsWith("http"))
        {
            try 
            {
                var uri = new Uri(fileName);
                
                // 3. Quan trọng: Decode URL (ví dụ %20 thành dấu cách)
                // Nếu file là "avatar my.jpg" -> URL là "avatar%20my.jpg" -> Cần decode lại
                var cleanPath = Uri.UnescapeDataString(uri.AbsolutePath); 
                
                var pathSegments = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                // Segment[0] là bucket, các cái sau là path của file
                if (pathSegments.Length > 1)
                {
                    fileName = string.Join("/", pathSegments.Skip(1));
                }
            }
            catch 
            {
                // Nếu parse URL lỗi, giữ nguyên fileName và thử xóa trực tiếp
            }
        }

        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName));
    }
}

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
