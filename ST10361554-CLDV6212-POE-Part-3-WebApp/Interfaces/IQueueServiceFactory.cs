using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Factory interface for creating instances of QueueService
    public interface IQueueServiceFactory
    {
        // Create an instance of QueueService for a specified queue name
        QueueService Create(string queueName);
    }
}
