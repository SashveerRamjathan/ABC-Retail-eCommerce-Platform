using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for order-related operations in table storage
    public interface IOrderDatabaseService
    {
        // Create a new order with multiple items
        Task<bool> CreateOrderAsync(List<Order> orderItems);

        // Get order details for a specific user and order
        Task<List<Order>> GetOrderDetailsAsync(string userId, string orderId);

        // Get the order history for a specific user
        Task<Dictionary<string, List<Order>>> GetOrdersHistoryByUserIdAsync(string userId);

        // Update the status of an entire order
        Task<bool> UpdateWholeOrderStatusAsync(string orderId, string newStatus);

        // Get all orders (for admin purposes)
        Task<Dictionary<string, List<Order>>> GetAllOrdersAsync();

        // Update the status of a specific order item
        Task<bool> UpdateOrderItemStatusAsync(string orderId, string productId, string newStatus);
    }
}
