using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Collections.Concurrent;
using System.Text;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class OrderDatabaseStorageService : IOrderDatabaseService
    {

        private readonly ILogger<OrderDatabaseStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // Constructor to initialize the TableClient and Logger
        public OrderDatabaseStorageService(
            ILogger<OrderDatabaseStorageService> logger,
            IConfiguration configuration,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = clientFactory.CreateClient();
        }

        /*
           * Code Attribution:
           * All the methods below use the http client to make HTTP requests to Azure Functions.
           * IEvangelist
           * 11 February 2023
           * Make HTTP requests with the HttpClient class
           * https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
        */

        public async Task<bool> CreateOrderAsync(List<Order> orderItems)
        {
            #region check if the orderItems is null and get the request url from the configuration

            if (orderItems == null || !orderItems.Any())
            {
                _logger.LogError("List of order items is null");
                return false;
            }

            // get the request uri from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:CreateOrderAsync"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return false;
            }

            #endregion

            try
            {
                #region serialize order items to json and send the request to the function

                // Serialize order items to JSON
                var jsonContent = JsonConvert.SerializeObject(orderItems);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the request to the function
                var response = await _httpClient.PostAsync(requestUrl, content);

                #endregion

                #region check if the request was successful

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Order {orderItems.ElementAt(0).OrderId} created successfully");
                    return true;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error creating order: {errorMessage}");
                    return false;
                }

                #endregion

            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP request error: {httpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return false;
            }
        }

        // Method to get all orders by all users (Admin)
        public async Task<Dictionary<string, List<Order>>> GetAllOrdersAsync()
        {
            #region get the request url from the configuration

            // get the request url from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:GetAllOrdersAsync"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return null!;
            }

            #endregion

            try
            {
                #region send the request to the function and check if the request was successful

                // Send the request to the function
                var response = await _httpClient.GetAsync(requestUrl);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        // Deserialize the JSON content to a dictionary
                        var orders = JsonConvert.DeserializeObject<Dictionary<string, List<Order>>>(jsonContent);

                        if (orders == null || !orders.Any())
                        {
                            _logger.LogInformation("No orders found.");
                            return null!;
                        }

                        _logger.LogInformation("Orders retrieved successfully for admin");
                        return orders;
                    }
                    catch (JsonException jsEx)
                    {
                        _logger.LogError($"Error deserializing JSON content: {jsEx.Message}");
                        return null!;
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting orders for admin: {errorMessage}");
                    return null!;
                }

                #endregion

            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error while calling the function: {httpEx.Message}");
                return null!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllOrdersAsync: {ex.Message}");
                return null!;
            }
        }

        public async Task<List<Order>> GetOrderDetailsAsync(string userId, string orderId)
        {
            #region check if the userId or order id is null or empty and get the request url from the configuration

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("UserId or PartitionKey is null or empty");
                return null!;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:GetOrderDetails"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return null!;
            }

            #endregion

            try
            {
                #region construct the request url and send the request to the function

                // construct the request url
                requestUrl = $"{requestUrl}?userId={userId}&orderId={orderId}";

                // Send the request to the function
                var response = await _httpClient.GetAsync(requestUrl);

                #endregion

                #region check if the request was successful

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    var orderItems = JsonConvert.DeserializeObject<List<Order>>(jsonContent);

                    if (orderItems == null || !orderItems.Any())
                    {
                        _logger.LogInformation($"No order items found for order {orderId} and user {userId}");
                        return null!;
                    }

                    _logger.LogInformation($"Order items retrieved successfully for order {orderId} and user {userId}");
                    return orderItems;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting order details for user {userId} and order {orderId}: {errorMessage}");
                    return null!;
                }

                #endregion
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error getting order details for user {userId} and order {orderId}: {ex.Message}");
                return null!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order details for user {userId} and order {orderId}: {ex.Message}");
                return null!;
            }
        }

        // Method to get all orders for the current user
        public async Task<Dictionary<string, List<Order>>> GetOrdersHistoryByUserIdAsync(string userId)
        {
            #region check if the userId is null or empty and get the request url from the configuration

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("UserId  is null or empty");
                return null!;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:GetOrdersHistoryByUserId"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return null!;
            }

            #endregion

            try
            {
                #region construct the request url and send the request to the function

                // construct the request url
                requestUrl = $"{requestUrl}?userId={userId}";

                // Send the request to the function
                var response = await _httpClient.GetAsync(requestUrl);

                #endregion

                #region check if the request was successful

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        // Deserialize the JSON content to a dictionary
                        var orders = JsonConvert.DeserializeObject<Dictionary<string, List<Order>>>(jsonContent);

                        if (orders == null || !orders.Any())
                        {
                            _logger.LogInformation("No orders found for user.");
                            return null!;
                        }

                        _logger.LogInformation($"Orders retrieved successfully for user {userId}");
                        return orders;
                    }
                    catch (JsonException jsEx)
                    {
                        _logger.LogError($"Error deserializing JSON content: {jsEx.Message}");
                        return null!;
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting orders for user: {errorMessage}");
                    return null!;
                }

                #endregion
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error while calling the function: {httpEx.Message}");
                return null!;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error getting order history for user {userId}: {ex.Message}");

                return null!;
            }
        }

        // Method to update the status of a single order item (Admin)
        public async Task<bool> UpdateOrderItemStatusAsync(string orderId, string productId, string newStatus)
        {
            #region check if the orderId, productId, or newStatus is null or empty and get the request url from the configuration

            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(newStatus))
            {
                _logger.LogError("OrderId, ProductId, or new status is null or empty");
                return false;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:UpdateOrderItemStatusAsync"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return false;
            }

            #endregion

            try
            {
                #region construct the request url and send the request to the function

                // construct the request url
                requestUrl = $"{requestUrl}?orderId={orderId}&productId={productId}&newStatus={newStatus}";

                // Send the request to the function
                var response = await _httpClient.PutAsync(requestUrl, null);

                #endregion

                #region check if the request was successful

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Order item {productId} status updated to {newStatus} for order {orderId}");
                    return true;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error updating order item {productId} status for order {orderId}: {errorMessage} (Status Code: {response.StatusCode})");
                    return false;
                }

                #endregion
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error updating order item {productId} status for order {orderId}: {httpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error updating order item {productId} status for order {orderId}: {ex.Message}");

                return false;
            }
        }

        // Method to update the status of a complete order (Admin)
        public async Task<bool> UpdateWholeOrderStatusAsync(string orderId, string newStatus)
        {
            #region check if the orderId or newStatus is null or empty and get the request url from the configuration

            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(newStatus))
            {
                _logger.LogError("OrderId or new status is null or empty");
                return false;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["OrderDatabaseStorageUrls:UpdateWholeOrderStatusAsync"];

            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Request URL is null or empty");
                return false;
            }

            #endregion

            try
            {
                #region construct the request url and send the request to the function

                // construct the request url
                requestUrl = $"{requestUrl}?orderId={orderId}&newStatus={newStatus}";

                // Send the request to the function
                var response = await _httpClient.PutAsync(requestUrl, null);

                #endregion

                #region check if the request was successful

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Order {orderId} status updated to {newStatus}");
                    return true;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error updating order {orderId} status: {errorMessage}");
                    return false;
                }

                #endregion
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error updating order {orderId} status: {httpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error updating order {orderId} status: {ex.Message}");

                return false;
            }
        }
    }
}
