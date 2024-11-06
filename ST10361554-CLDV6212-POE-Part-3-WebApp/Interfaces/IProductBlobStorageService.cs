using Microsoft.AspNetCore.Mvc;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for product image-related operations in blob storage
    public interface IProductBlobStorageService
    {
        // Upload a product image to blob storage
        Task<bool> UploadProductImageAsync(string category, string rowKey, IFormFile file);

        // Download a product image from blob storage
        Task<FileContentResult?> DownloadProductImageAsync(string category, string rowKey);

        // Delete a product image from blob storage
        Task<bool> DeleteProductImageAsync(string category, string rowKey);

        // Check if a product image exists in blob storage
        Task<bool> ImageExistsAsync(string category, string rowKey);
    }
}
