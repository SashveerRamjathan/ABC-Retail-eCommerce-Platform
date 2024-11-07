using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Factory interface for creating instances of FileService
    public interface IFileServiceFactory
    {
        // Get an instance of FileService for a specific customer by their ID
        FileService GetFileService(string customerId);
    }
}
