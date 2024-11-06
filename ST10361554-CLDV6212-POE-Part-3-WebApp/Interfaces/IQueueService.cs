using Azure.Storage.Queues.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for queue-related operations
    public interface IQueueService
    {
        // Send a message to a specified queue and return the send receipt
        Task<SendReceipt> SendMessageAsync(string message, string queueName);
    }
}
