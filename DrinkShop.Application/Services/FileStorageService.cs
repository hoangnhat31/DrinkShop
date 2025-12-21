using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;
using DrinkShop.Application.Interfaces;
using System;
using System.IO;
using System.Linq;
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

        if (string.IsNullOrEmpty(settings.Endpoint) || string.IsNullOrEmpty(settings.AccessKey))
        {
            throw new ArgumentException("MinIO Config bị thiếu! Kiểm tra lại appsettings.json");
        }

        _bucketName = settings.Bucket ?? "drinkshop";
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

        bool exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName)
        );

        if (!exists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType));

        var protocol = _useSSL ? "https" : "http";
        return $"{protocol}://{_endpoint}/{_bucketName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileName)
    {
        if (fileName.StartsWith("http"))
        {
            try 
            {
                var uri = new Uri(fileName);
                var cleanPath = Uri.UnescapeDataString(uri.AbsolutePath); 
                var pathSegments = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                if (pathSegments.Length > 1)
                {
                    fileName = string.Join("/", pathSegments.Skip(1));
                }
            }
            catch 
            {
                // Giữ nguyên fileName nếu không thể parse URL
            }
        }

        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName));
    }
}

public class MinioSetting
{
    public string? Endpoint { get; set; } 
    public string? AccessKey { get; set; } 
    public string? SecretKey { get; set; }
    public string? Bucket { get; set; } 
    public bool UseSSL { get; set; } = false;
}