using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for file-related operations
    public interface IFileService
    {
        // Upload a file with the given byte array and file name
        Task<bool> FileUploadAsync(byte[] bytes, string fileName);

        // Download a file by its name and return the result
        Task<FileDownloadResult> FileDownloadAsync(string fileName);

        // Set the directory for a specific customer by their ID
        void SetCustomerDirectory(string customerId);
    }
}
