using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;
using ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels;
using System.Security.Claims;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrdersController : Controller
    {
        private readonly IOrderDatabaseService _orderTableStorageService;
        private readonly ILogger<CustomerOrdersController> _logger;
        private readonly IProductDatabaseService _productTableStorageService;
        private readonly IProductBlobStorageService _blobStorageService;
        private readonly FileService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomerOrdersController(IOrderDatabaseService orderService,
            ILogger<CustomerOrdersController> logger,
            IProductDatabaseService productTableStorageService,
            IProductBlobStorageService productBlobStorageService,
            IFileServiceFactory fileServiceFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _orderTableStorageService = orderService;
            _logger = logger;
            _productTableStorageService = productTableStorageService;
            _blobStorageService = productBlobStorageService;
            _httpContextAccessor = httpContextAccessor;

            // Retrieve the user ID from the HTTP context
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId), "User ID claim is missing.");
            }

            // Initialize the file service with the user ID
            _fileService = fileServiceFactory.GetFileService(userId);
        }

        // Calculate the overall status of an order based on the status of individual items
        private static string CalculateWholeOrderStatus(List<Order> items)
        {
            // Check if all items are pending
            bool allPending = items.All(i => i.ItemStatus == "Pending");
            // Check if any item is in progress
            bool anyInProgress = items.Any(i => i.ItemStatus == "In Progress");
            // Check if all items are shipped
            bool allShipped = items.All(i => i.ItemStatus == "Shipped");

            // Determine the overall order status
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

        // GET: CustomerOrders/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Retrieve the user ID from the claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not found.");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get the order history for the user
                var groupedOrders = await _orderTableStorageService.GetOrdersHistoryByUserIdAsync(userId);

                if (groupedOrders == null)
                {
                    _logger.LogWarning($"No orders found for user {userId}");
                    TempData["ErrorMessage"] = "No orders found.";

                    return View(new List<OrderIndexViewModel> { });
                }

                List<OrderIndexViewModel> orderHistory = new List<OrderIndexViewModel>();

                foreach (var order in groupedOrders)
                {
                    var orderItems = order.Value;

                    // Calculate the overall order status
                    string orderStatus = CalculateWholeOrderStatus(orderItems);

                    // Calculate the total price of the order
                    decimal total = orderItems.Sum(i => i.TotalItemPrice);

                    // Get the first item to retrieve the order date & ID
                    var firstItem = orderItems.First();

                    // Get the order date
                    var localDateTime = firstItem.OrderDate;

                    // Create a new OrderHistoryViewModel
                    var orderHistoryItem = new OrderIndexViewModel
                    {
                        OrderID = firstItem.OrderId,
                        OrderDate = localDateTime,
                        OrderStatus = orderStatus,
                        GrandTotal = total
                    };

                    orderHistory.Add(orderHistoryItem);
                }

                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                // Sort the order list by date (from newest to oldest)
                orderHistory = orderHistory.OrderByDescending(o => o.OrderDate).ToList();

                return View(orderHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order history: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to retrieve order history.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: CustomerOrders/Details
        [HttpGet]
        public async Task<IActionResult> OrderDetails(string partitionKey)
        {
            try
            {
                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                // Retrieve the user ID from the claims
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("User not found.");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Orders");
                }

                // Get the order details for the specified partition key
                var orderDetails = await _orderTableStorageService.GetOrderDetailsAsync(id, partitionKey);

                if (orderDetails == null)
                {
                    _logger.LogError($"Order with PartitionKey: {partitionKey} not found.");
                    TempData["ErrorMessage"] = $"Order with PartitionKey: {partitionKey} not found.";
                    return RedirectToAction("Index", "Orders");
                }

                // Get all products
                var allProducts = await _productTableStorageService.GetAllProductsAsync();

                // Get the products in the order
                var productsInOrder = allProducts.Where(p => orderDetails.Any(o => o.ProductId == p.Id)).ToList();

                // Create a list of OrderItems
                var items = new List<OrderItemViewModel>();

                foreach (var item in orderDetails)
                {
                    var product = productsInOrder.FirstOrDefault(p => p.Id == item.ProductId);

                    string? base64Image = null;

                    if (product != null)
                    {
                        // Check if the product image exists in blob storage
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

                        // Create a new OrderItemViewModel
                        var orderItem = new OrderItemViewModel
                        {
                            ProductName = product.Name,
                            Quantity = item.Quantity,
                            TotalItemPrice = item.TotalItemPrice,
                            ItemStatus = item.ItemStatus,
                            Base64ProductImage = base64Image!
                        };

                        items.Add(orderItem);
                    }
                }

                // Get the order date & time
                var localDateTime = orderDetails.First().OrderDate;

                ViewBag.OrderID = partitionKey;
                ViewBag.OrderDate = localDateTime;
                ViewBag.OrderStatus = CalculateWholeOrderStatus(orderDetails);

                // Sort the order items by price (from lowest to highest)
                items = items.OrderBy(i => i.TotalItemPrice).ToList();

                return View(items);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error retrieving order ({partitionKey}) details: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to retrieve order details for order {partitionKey}";
                return RedirectToAction("Index", "Orders");
            }
        }

        // POST: CustomerOrders/DownloadInvoice
        [HttpPost]
        public async Task<IActionResult> DownloadInvoice(string orderId)
        {
            try
            {
                FileDownloadResult result = null!;

                if (!string.IsNullOrEmpty(orderId))
                {
                    string fileName = $"{orderId}_invoice.pdf";

                    // Download the invoice file
                    result = await _fileService.FileDownloadAsync(fileName);
                }

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"{orderId}_invoice.pdf downloaded successfully";

                    return File(result.Content, result.ContentType, result.FileName);
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to download invoice for order {orderId}";
                }

                return RedirectToAction("OrderDetails", "CustomerOrders", new { orderId });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error getting invoice for order {orderId}: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to get invoice for order {orderId}";
                return RedirectToAction("OrderDetails", "Orders");
            }
        }
    }
}
