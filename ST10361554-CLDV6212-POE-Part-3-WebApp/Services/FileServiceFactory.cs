using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Factory class for creating instances of FileService
    public class FileServiceFactory : IFileServiceFactory
    {
        private readonly ILogger<FileService> _logger;
        private readonly IQueueServiceFactory _queueServiceFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Constructor to initialize the factory with connection string, logger, and queue service factory
        public FileServiceFactory(
            ILogger<FileService> logger,
            IQueueServiceFactory queueServiceFactory,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _queueServiceFactory = queueServiceFactory;
            _httpClientFactory = clientFactory;
            _configuration = configuration;
        }

        // Method to create and configure a new instance of FileService
        public FileService GetFileService(string customerId)
        {
            // Create a new instance of the FileService class
            var fileService = new FileService(_logger, _queueServiceFactory, _httpClientFactory, _configuration);

            // Set the customer directory
            fileService.SetCustomerDirectory(customerId);

            // Return the configured file service
            return fileService;
        }
    }
}
