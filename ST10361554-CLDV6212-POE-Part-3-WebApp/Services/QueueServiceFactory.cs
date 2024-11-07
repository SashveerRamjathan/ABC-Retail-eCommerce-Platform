using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class QueueServiceFactory : IQueueServiceFactory
    {
        private readonly ILogger<QueueService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Constructor to initialize the connection string and logger
        public QueueServiceFactory(
            ILogger<QueueService> logger,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = clientFactory;
            _configuration = configuration;
        }

        // Method to create a new instance of QueueService
        public QueueService Create(string queueName)
        {
            // Create and return a new QueueService instance
            return new QueueService(_logger, _httpClientFactory, _configuration);
        }
    }
}
