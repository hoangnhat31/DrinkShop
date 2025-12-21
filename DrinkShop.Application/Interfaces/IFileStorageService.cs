using DrinkShop.Domain.Entities;

namespace DrinkShop.Application.Interfaces
{
public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string fileName);
}

}