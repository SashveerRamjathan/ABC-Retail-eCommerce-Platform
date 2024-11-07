using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;
using ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Controllers
{
    public class AdminOrdersController : Controller
    {
        // Private fields for services and logger
        private readonly IOrderDatabaseService _orderTableStorageService;
        private readonly ILogger<AdminOrdersController> _logger;
        private readonly IProductDatabaseService _productTableStorageService;
        private readonly QueueService _orderQueueService;
        private readonly string orderQueueName = "order-queue";
        private readonly IAccountDatabaseService _accountTableStorageService;
        private readonly IProductBlobStorageService _blobStorageService;
        private readonly IFileServiceFactory _fileServiceFactory;

        // Constructor to initialize services and logger
        public AdminOrdersController(IOrderDatabaseService orderService,
            ILogger<AdminOrdersController> logger,
            IProductDatabaseService productTableStorageService,
            IQueueServiceFactory queueServiceFactory,
            IAccountDatabaseService accountTableStorageService,
            IProductBlobStorageService blobStorageService,
            IFileServiceFactory fileServiceFactory)
        {
            _orderTableStorageService = orderService;
            _logger = logger;
            _productTableStorageService = productTableStorageService;
            _orderQueueService = queueServiceFactory.Create(orderQueueName);
            _accountTableStorageService = accountTableStorageService;
            _blobStorageService = blobStorageService;
            _fileServiceFactory = fileServiceFactory;
        }

        // Method to calculate the overall status of an order based on its items
        private static string CalculateWholeOrderStatus(List<Order> items)
        {
            // Check items statuses
            bool allPending = items.All(i => i.ItemStatus == "Pending");
            bool anyInProgress = items.Any(i => i.ItemStatus == "In Progress");
            bool allShipped = items.All(i => i.ItemStatus == "Shipped");

            // Calculate whole order status
            if (allPending)
            {
                return "Pending";
            }
            else if (anyInProgress)
            {
                return "In Progress";
            }
            else if (allShipped)
            {
                return "Shipped";
            }
            else
            {
                return "Mixed";
            }
        }

        // Index page for the admin to view all orders
        // GET: AdminOrders/Index
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Retrieve all grouped orders from the storage service
                var groupedOrders = await _orderTableStorageService.GetAllOrdersAsync();

                if (groupedOrders != null)
                {
                    List<AdminOrderIndexViewModel> orders = new List<AdminOrderIndexViewModel>();

                    foreach (var order in groupedOrders)
                    {
                        var orderItems = order.Value;

                        // Calculate whole order status
                        string orderStatus = CalculateWholeOrderStatus(orderItems);

                        // Calculate total price
                        decimal total = orderItems.Sum(i => i.TotalItemPrice);

                        // Get the first item to get the order date & ID
                        var firstItem = orderItems.First();

                        // Get the customer entity
                        var customer = await _accountTableStorageService.GetUserByIdAsync(firstItem.UserId);

                        if (customer != null)
                        {
                            // Get the customer name and ID
                            string customerName = customer.Name!;
                            string customerId = customer.Id;

                            // Get the order date
                            var localDateTime = firstItem.OrderDate;

                            // Create a new AdminOrderIndexViewModel
                            var orderHistoryItem = new AdminOrderIndexViewModel
                            {
                                OrderID = firstItem.OrderId,
                                OrderDate = localDateTime,
                                OrderStatus = orderStatus,
                                GrandTotal = total,
                                CustomerID = customerId,
                                CustomerName = customerName
                            };

                            orders.Add(orderHistoryItem);
                        }
                        else
                        {
                            _logger.LogError($"Failed to retrieve customer: {firstItem.UserId}");
                        }
                    }

                    // Log and send message if all orders are retrieved successfully
                    if (groupedOrders.Keys.Count == orders.Count)
                    {
                        _logger.LogInformation("Orders retrieved successfully for admin");
                        await _orderQueueService.SendMessageAsync("Orders retrieved successfully for admin", orderQueueName);
                    }
                    else
                    {
                        _logger.LogError($"Failed to retrieve all orders for admin. " +
                            $"Expected: {groupedOrders.Keys.Count} orders. Retrieved: {orders.Count}");
                        TempData["ErrorMessage"] = $"Failed to retrieve all orders for admin. " +
                            $"Expected: {groupedOrders.Keys.Count} orders. Retrieved: {orders.Count}";

                        await _orderQueueService.SendMessageAsync("Failed to retrieve all orders for admin", orderQueueName);
                    }

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Sort the order list by date (from newest to oldest)
                    orders = orders.OrderByDescending(o => o.OrderDate).ToList();

                    return View(orders);
                }

                _logger.LogError("Failed to retrieve orders for admin");
                TempData["ErrorMessage"] = "Failed to retrieve orders for admin";

                await _orderQueueService.SendMessageAsync("Failed to retrieve orders for admin", orderQueueName);

                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View();
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error retrieving orders for admin: {ex}");

                await _orderQueueService.SendMessageAsync($"Error retrieving orders for admin", orderQueueName);

                TempData["ErrorMessage"] = "Failed to retrieve orders for admin";

                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View();
            }
        }

        // GET: AdminOrders/Details/
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OrderDetails(string orderId, string customerId)
        {
            try
            {
                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId))
                {
                    TempData["ErrorMessage"] = "Invalid order ID or customer ID";
                    return RedirectToAction("Index");
                }

                // Retrieve order details
                var orderDetails = await _orderTableStorageService.GetOrderDetailsAsync(customerId, orderId);

                if (orderDetails == null)
                {
                    _logger.LogError($"Order with order ID: {orderId} not found.");
                    TempData["ErrorMessage"] = $"Order with order ID: {orderId} not found.";
                    return RedirectToAction("Index");
                }

                var allProducts = await _productTableStorageService.GetAllProductsAsync();

                // Get the products in the order
                var productsInOrder = allProducts.Where(p => orderDetails.Any(o => o.ProductId == p.Id)).ToList();

                // Create a list of OrderItems
                var items = new List<AdminOrderItemViewModel>();

                foreach (var item in orderDetails)
                {
                    var product = productsInOrder.FirstOrDefault(p => p.Id == item.ProductId);

                    string? base64Image = null;

                    if (product != null)
                    {
                        // Get the current product image from blob storage
                        bool imageExists = await _blobStorageService.ImageExistsAsync(product.Category, product.Id);

                        if (imageExists)
                        {

                            var fileContentResult = await _blobStorageService.DownloadProductImageAsync(product.Category, product.Id);

                            if (fileContentResult != null)
                            {
                                // Get the byte array from the FileContentResult
                                var imageBytes = fileContentResult.FileContents;

                                // Convert byte array to base64 string
                                base64Image = Convert.ToBase64String(imageBytes);
                            }
                        }

                        var orderItem = new AdminOrderItemViewModel
                        {
                            ProductName = product.Name,
                            Quantity = item.Quantity,
                            TotalItemPrice = item.TotalItemPrice,
                            ItemStatus = item.ItemStatus,
                            Base64ProductImage = base64Image!,
                            ProductId = product.Id
                        };

                        items.Add(orderItem);
                    }
                }

                // Get the order date & time
                var localDateTime = orderDetails.First().OrderDate;

                ViewBag.OrderID = orderId;
                ViewBag.OrderDate = localDateTime;
                ViewBag.OrderStatus = CalculateWholeOrderStatus(orderDetails);
                ViewBag.CustomerID = customerId;

                // Sort the order items by price (from lowest to highest)
                items = items.OrderBy(i => i.TotalItemPrice).ToList();

                return View(items);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error retrieving order items: {ex}");
                TempData["ErrorMessage"] = "Failed to retrieve order items";
                return RedirectToAction("Index");
            }
        }

        // POST: AdminOrders/DownloadInvoice
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadInvoice(string orderId, string customerId)
        {
            try
            {
                // Check if the order ID and customer ID is null or empty
                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId))
                {
                    TempData["ErrorMessage"] = "Invalid order ID or customer ID to download invoice";
                    _logger.LogError("Invalid order ID or customer ID to download invoice");
                    return RedirectToAction("Index");
                }

                var fileService = _fileServiceFactory.GetFileService(customerId);

                if (fileService == null)
                {
                    TempData["ErrorMessage"] = "File Service is null";
                    _logger.LogError("File Service is null");
                    return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
                }

                FileDownloadResult result = null!;

                if (!string.IsNullOrEmpty(orderId))
                {
                    string fileName = $"{orderId}_invoice.pdf";

                    result = await fileService.FileDownloadAsync(fileName);
                }

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"{orderId}_invoice.pdf downloaded successfully";

                    return File(result.Content, result.ContentType, result.FileName);
                }

                TempData["ErrorMessage"] = $"Failed to download invoice for order {orderId}";

                return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error getting invoice for order {orderId}: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to get invoice for order {orderId}";
                return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
            }
        }

        // POST: AdminOrders/UpdateOrderItemStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderItemStatus(string orderId, string customerId, string productId, string status)
        {
            try
            {
                // Check all inputs
                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(productId))
                {
                    TempData["ErrorMessage"] = "Invalid order ID, customer ID or product ID to update order item status";
                    _logger.LogError("Invalid order ID, customer ID or product ID to update order item status");
                    return RedirectToAction("Index");
                }

                // Update the order item status in table storage
                bool result = await _orderTableStorageService.UpdateOrderItemStatusAsync(orderId, productId, status);

                // Check if the update was successful
                if (result)
                {
                    TempData["SuccessMessage"] = $"Order item status updated successfully";
                    await _orderQueueService.SendMessageAsync($"Order ({orderId}) item ({productId})  status updated successfully", orderQueueName);
                    _logger.LogInformation($"Order item status updated successfully");
                    return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to update order item status";
                    await _orderQueueService.SendMessageAsync($"Failed to update order item status", orderQueueName);
                    _logger.LogError($"Failed to update order item status");
                    return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error updating order item status: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to update order item status";
                await _orderQueueService.SendMessageAsync($"Failed to update order item status", orderQueueName);
                return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
            }
        }

        // POST: AdminOrders/UpdateWholeOrderStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWholeOrderStatus(string orderId, string customerId, string status)
        {
            try
            {
                // Validate the inputs
                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(status))
                {
                    TempData["ErrorMessage"] = "Invalid order ID or customer ID or status to update whole order status";
                    _logger.LogError("Invalid order ID or customer ID or status to update whole order status");
                    return RedirectToAction("Index");
                }

                // Update the whole order status in table storage
                var result = await _orderTableStorageService.UpdateWholeOrderStatusAsync(orderId, status);

                // Check if the update was successful
                if (result)
                {
                    TempData["SuccessMessage"] = $"Whole order status updated successfully";
                    await _orderQueueService.SendMessageAsync($"Whole order ({orderId}) status updated successfully", orderQueueName);
                    _logger.LogInformation($"Whole order status updated successfully");
                    return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to update whole order status";
                    await _orderQueueService.SendMessageAsync($"Failed to update whole order status", orderQueueName);
                    _logger.LogError($"Failed to update whole order status");
                    return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error updating whole order status: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to update whole order status";
                await _orderQueueService.SendMessageAsync($"Failed to update whole order ({orderId}) status", orderQueueName);
                return RedirectToAction("OrderDetails", "AdminOrders", new { orderId, customerId });
            }
        }
    }
}
