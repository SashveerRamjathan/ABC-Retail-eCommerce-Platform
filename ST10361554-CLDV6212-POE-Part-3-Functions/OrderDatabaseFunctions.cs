using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_Functions.Data;
using ST10361554_CLDV6212_POE_Part_3_Functions.Models;
using System.Reflection;

namespace ST10361554_CLDV6212_POE_Part_3_Functions
{
    public class OrderDatabaseFunctions
    {
        private readonly ILogger<OrderDatabaseFunctions> _logger;
        private readonly ApplicationDbContext _context;

        public OrderDatabaseFunctions(ILogger<OrderDatabaseFunctions> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _context = dbContext;
        }

        // Method to check for null fields in any object
        private bool CheckForNullFields(object obj)
        {
            #region Check for null fields in any object

            try
            {
                // Get all public instance properties of the object
                var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // Check if any property value is null
                return properties.Any(prop => prop.GetValue(obj) == null);
            }
            catch (Exception)
            {
                // Rethrow the exception to be handled by the calling code
                throw;
            }

            #endregion
        }

        [Function(nameof(CreateOrderAsync))]
        public async Task<IActionResult> CreateOrderAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Adding order items to the database");

            try
            {
                #region get request body and deserialize it

                // Get the request body
                string requestBody;

                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // list of order items
                List<Order>? orderItems;

                // deserialize the request body
                try
                {
                    orderItems = JsonConvert.DeserializeObject<List<Order>>(requestBody);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Error deserializing request body: {jsonEx.Message}");
                    return new BadRequestObjectResult("Invalid request body. Please provide a valid JSON object.");
                }

                #endregion

                #region check for null fields in order items

                if (orderItems == null || orderItems.Count == 0)
                {
                    _logger.LogError("Invalid request body. Please provide a valid JSON object.");
                    return new BadRequestObjectResult("Invalid request body. Please provide a valid JSON object.");
                }

                // Check for null fields in the order items
                foreach (var orderItem in orderItems)
                {
                    if (CheckForNullFields(orderItem))
                    {
                        _logger.LogError($"Invalid order item detected: {JsonConvert.SerializeObject(orderItem)}");
                        return new BadRequestObjectResult("Invalid order item. Ensure all fields are provided.");
                    }
                }

                // check that all order items have the same order id
                if (orderItems.Any(item => item.OrderId != orderItems.First().OrderId))
                {
                    _logger.LogError("Invalid request body. All order items must have the same id.");
                    return new BadRequestObjectResult("Invalid request body. All order items must have the same id.");
                }

                #endregion

                #region check if database context is not null and create order

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                // add the order items to the database
                await _context.Orders.AddRangeAsync(orderItems);

                // save the changes to the database
                var response = await _context.SaveChangesAsync();

                #endregion

                #region check if the batch write was successful

                // Check if the batch was successful
                if (response == orderItems.Count)
                {
                    _logger.LogInformation($"Order {orderItems.First().OrderId} created successfully with {orderItems.Count} items.");
                    return new OkObjectResult($"Order {orderItems.First().OrderId} created successfully with {orderItems.Count} items.");
                }
                else
                {
                    _logger.LogError($"Error creating order {orderItems.First().OrderId}");
                    return new BadRequestObjectResult("Error creating order not all items were inserted. Please try again.");
                }

                #endregion
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex.Message, "Operation was cancelled.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Order.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error adding order items to the database: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetAllOrdersAsync))]
        public async Task<IActionResult> GetAllOrdersAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Getting all orders from the database");

            try
            {
                #region check if database context is null

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region get all orders from the database and group them by order id

                // Dictionary to store orders, with Id as the key, and list of items as the value
                var ordersDictionary = new Dictionary<string, List<Order>>();

                // Get all orders from the database
                var orders = await _context.Orders.ToListAsync();

                // check if no orders were found
                if (orders.Count == 0)
                {
                    _logger.LogInformation("No orders found in the database.");
                    return new NotFoundObjectResult("No orders found.");
                }

                // Group the orders by order id
                foreach (var orderItem in orders)
                {
                    if (!ordersDictionary.ContainsKey(orderItem.OrderId))
                    {
                        ordersDictionary[orderItem.OrderId] = new List<Order>();
                    }

                    ordersDictionary[orderItem.OrderId].Add(orderItem);

                }

                _logger.LogInformation($"Orders for admin retrieved successfully, grouped by order id.");

                #endregion

                // Return the dictionary of orders
                return new OkObjectResult(ordersDictionary);
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error getting orders from the database: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetOrderDetails))]
        public IActionResult GetOrderDetails([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            #region get user id and order id from query string and check if they are provided

            // get the user id from the query string
            string? userId = req.Query["userId"];

            // get the partition key from the query string
            string? orderId = req.Query["orderId"];

            // check if the user id is provided
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User Id is required.");
                return new BadRequestObjectResult("User Id is required.");
            }

            // check if the order id is provided
            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("Order Id is required.");
                return new BadRequestObjectResult("Order Id is required.");
            }

            #endregion

            #region check if database context is null

            // check if dbContext is null
            if (_context == null)
            {
                _logger.LogError("Database Context is not initialized.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation($"Getting order details for user {userId} and order {orderId}");

            #endregion

            try
            {
                #region get order items for the user and order id

                List<Order> orderItems = new List<Order>();

                // Log the query parameters
                _logger.LogInformation($"Querying with OrderId: {orderId}, UserId: {userId}");

                // Execute the query
                orderItems = _context.Orders.Where(o => o.OrderId == orderId && o.UserId == userId).ToList();

                #endregion

                #region check if order items were found

                // Check if no order items were found for order id
                if (orderItems.Count == 0)
                {
                    _logger.LogInformation($"No order items found for order {orderId} and user {userId}");
                    return new NotFoundObjectResult($"No order items found for order {orderId} and user {userId}");
                }

                _logger.LogInformation($"Order items for order {orderId} and user {userId} retrieved successfully.");

                #endregion

                // Return the list of order items
                return new OkObjectResult(orderItems);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order items for order {orderId} and user {userId}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetOrdersHistoryByUserId))]
        public IActionResult GetOrdersHistoryByUserId([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            #region get user id from query string, check if it is provided

            // get the user id from the query string
            string? userId = req.Query["userId"];

            // check if the user id is provided
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User Id is required.");
                return new BadRequestObjectResult("User Id is required.");
            }

            #endregion

            #region check if database context is null

            // check if dbContext is null
            if (_context == null)
            {
                _logger.LogError("Database Context is not initialized.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation($"Getting order history for user {userId}");

            #endregion

            try
            {
                #region get order history for the user and group them by order id

                // Dictionary to store orders, with OrderId as the key, and list of items as the value
                var ordersDictionary = new Dictionary<string, List<Order>>();

                // Log the query parameters
                _logger.LogInformation($"Querying with UserId: {userId}");

                // Get all orders for the user
                var orders = _context.Orders.Where(o => o.UserId == userId).ToList();

                // check if no orders were found
                if (orders == null || orders.Count == 0)
                {
                    _logger.LogInformation($"No order history found for user {userId}");
                    return new NotFoundObjectResult($"No order history found for user {userId}");
                }

                // Group items by their OrderId
                foreach (var orderItem in orders)
                {
                    if (!ordersDictionary.ContainsKey(orderItem.OrderId))
                    {
                        ordersDictionary[orderItem.OrderId] = new List<Order>();
                    }

                    ordersDictionary[orderItem.OrderId].Add(orderItem);
                }

                _logger.LogInformation($"Order history for user {userId} retrieved successfully, grouped by order id.");

                #endregion


                // Return the list of order items
                return new OkObjectResult(ordersDictionary);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order history for user {userId}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(UpdateWholeOrderStatusAsync))]
        public async Task<IActionResult> UpdateWholeOrderStatusAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
        {
            #region get order id and new status from query string, check if they are provided

            // get the partition key from the query string
            string? orderId = req.Query["orderId"];

            // get the new status from the query string
            string? newStatus = req.Query["newStatus"];

            // check if the order id is provided
            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("Order Id is required.");
                return new BadRequestObjectResult("Order Id is required.");
            }

            // check if the new status is provided
            if (string.IsNullOrEmpty(newStatus))
            {
                _logger.LogError("New Status is required.");
                return new BadRequestObjectResult("New Status is required.");
            }

            #endregion

            #region check if database context is null

            // check if dbContext is null
            if (_context == null)
            {
                _logger.LogError("Database Context is not initialized.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation($"Updating order status for order {orderId}");

            #endregion

            try
            {
                #region get order items for the order id

                // Log the query parameters
                _logger.LogInformation($"Querying with OrderId: {orderId}");

                // Query all items in the order
                var items = _context.Orders.Where(o => o.OrderId == orderId).ToList();

                // Check if no order items were found for order id
                if (items == null || items.Count == 0)
                {
                    _logger.LogInformation($"No order items found for order {orderId}");
                    return new NotFoundObjectResult($"No order items found for order {orderId}");
                }
                
                _logger.LogInformation($"Order items for order {orderId} and retrieved successfully.");

                #endregion

                #region update order items statuses

                _logger.LogInformation($"Updating order status to {newStatus} for order {orderId}");

                // Update the status of all items in the order
                foreach (var item in items)
                {
                    item.ItemStatus = newStatus;
                }

                // Update the items in the database
                _context.Orders.UpdateRange(items);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                #endregion

                #region check if the batch write was successfull

                // Check if the batch was successful
                if (response == items.Count)
                {
                    _logger.LogInformation($"Order {orderId} status updated successfully to {newStatus}.");
                    return new OkObjectResult($"Order {orderId} status updated successfully to {newStatus}.");
                }
                else
                {
                    _logger.LogError($"Error updating order {orderId} status to {newStatus}, not all items were updated");
                    return new BadRequestObjectResult("Error updating order status. Please try again. Not all items were updated.");
                }

                #endregion
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Order.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order items for order {orderId}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(UpdateOrderItemStatusAsync))]
        public async Task<IActionResult> UpdateOrderItemStatusAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
        {
            #region get order id, product id, new status from query string, check if they are provided

            // get the partition key from the query string
            string? orderId = req.Query["orderId"];

            // get the row key from the query string
            string? productId = req.Query["productId"];

            // get the new status from the query string
            string? newStatus = req.Query["newStatus"];

            // check if the order id is provided
            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("Order Id is required.");
                return new BadRequestObjectResult("Order Id is required.");
            }

            // check if the product id is provided
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogError("Product Id is required.");
                return new BadRequestObjectResult("Product Id is required.");
            }

            // check if the new status is provided
            if (string.IsNullOrEmpty(newStatus))
            {
                _logger.LogError("New Status is required.");
                return new BadRequestObjectResult("New Status is required.");
            }

            #endregion

            #region check if database context is null

            // check if dbContext is null
            if (_context == null)
            {
                _logger.LogError("Database Context is not initialized.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            #endregion

            try
            {
                #region get order item for the order id and product id

                _logger.LogInformation($"Updating order item status for order {orderId} and product {productId}");

                // Log the query parameters
                _logger.LogInformation($"Querying with OrderId: {orderId}, ProductId: {productId}");

                Order? item;

                // get the order item
                item = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.ProductId == productId);

                // Check if no order item was found for order id and product id
                if (item == null)
                {
                    _logger.LogInformation($"No order item found for order {orderId} and product {productId}");
                    return new NotFoundObjectResult($"No order item found for order {orderId} and product {productId}");
                }

                _logger.LogInformation($"Order item for order {orderId} and product {productId} retrieved successfully.");

                #endregion

                #region update order item status

                _logger.LogInformation($"Updating order item status to {newStatus}");

                // Update the status
                item.ItemStatus = newStatus;

                // Update the item in the database
                _context.Orders.Update(item);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                #endregion

                #region confirm the order status has been updated

                // Confirm the order status has been updated
                if (response == 0)
                {
                    _logger.LogError($"Error updating order item {productId} status to {newStatus} for order {orderId}");
                    return new BadRequestObjectResult("Error updating order item status. Please try again.");
                }

                _logger.LogInformation($"Order item {productId} status updated successfully to {newStatus} for order {orderId}");
                return new OkObjectResult($"Order item {productId} status updated successfully to {newStatus} for order {orderId}");

                #endregion
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Order.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order item for order {orderId} and product {productId}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
