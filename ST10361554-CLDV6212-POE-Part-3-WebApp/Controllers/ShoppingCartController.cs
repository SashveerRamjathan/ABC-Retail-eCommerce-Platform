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
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly IProductDatabaseService _productTableStorageService;
        private readonly IOrderDatabaseService _orderTableStorageService;
        private readonly string orderQueueName = "order-queue";
        private readonly QueueService _orderQueueService;
        private readonly IInvoiceInformationService _invoiceInformationService;
        private readonly FileService _fileService;
        private readonly IInvoiceGenerationService _invoiceGenerationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor to initialize dependencies
        public ShoppingCartController(ILogger<ShoppingCartController> logger,
            IShoppingCartService shoppingCartService,
            IProductDatabaseService productTableStorageService,
            IOrderDatabaseService orderTableStorageService,
            IQueueServiceFactory queueServiceFactory,
            IInvoiceInformationService invoiceInformationService,
            IFileServiceFactory fileServiceFactory,
            IInvoiceGenerationService invoiceGenerationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _shoppingCartService = shoppingCartService;
            _productTableStorageService = productTableStorageService;
            _orderTableStorageService = orderTableStorageService;
            _orderQueueService = queueServiceFactory.Create(orderQueueName);
            _invoiceInformationService = invoiceInformationService;
            _httpContextAccessor = httpContextAccessor;

            // Get the user ID from the HTTP context
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId), "User ID claim is missing.");
            }

            _fileService = fileServiceFactory.GetFileService(userId);
            _invoiceGenerationService = invoiceGenerationService;
        }

        // Display the shopping cart items
        public IActionResult Index()
        {
            var cartItems = _shoppingCartService.GetCartItems();

            var items = cartItems.Select(item => new CartItemViewModel
            {
                Product = item,
                Quantity = item.Quantity,
                TotalPrice = item.Price * item.Quantity
            }).ToList();

            // Calculate total price
            decimal total = items.Sum(i => i.TotalPrice);

            // Pass success and error messages to the view
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];

            // Pass items and total to the view
            var model = new CartIndexViewModel
            {
                CartItems = items,
                GrandTotal = total
            };

            return View(model);
        }

        // GET: ShoppingCart/AddToCart
        [HttpGet]
        public async Task<IActionResult> AddToCart(string partitionKey, string rowKey)
        {
            var product = await _productTableStorageService.GetProductAsync(rowKey);

            if (product == null)
            {
                _logger.LogWarning($"Product with PartitionKey: {partitionKey} and RowKey: {rowKey} not found.");
                TempData["ErrorMessage"] = $"Product with PartitionKey: {partitionKey} and RowKey: {rowKey} not found.";
                return RedirectToAction("Index");
            }

            var viewModel = new CartItemViewModel
            {
                Product = product,
                Quantity = 1
            };

            return View(viewModel);
        }

        // POST: ShoppingCart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(CartItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var success = await _shoppingCartService.AddToCart(viewModel.Product.Id, viewModel.Quantity);

                if (success)
                {
                    _logger.LogInformation($"Product {viewModel.Product.Name} added to cart");
                    TempData["SuccessMessage"] = $"Product {viewModel.Product.Name} added to cart successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogWarning($"Product {viewModel.Product.Name} could not be added to cart");
                    TempData["ErrorMessage"] = $"Product {viewModel.Product.Name} could not be added to cart.";
                    return RedirectToAction("Index");
                }
            }

            return View(viewModel);
        }

        // GET: ShoppingCart/EditQuantity
        [HttpGet]
        public async Task<IActionResult> EditQuantity(string partitionKey, string rowKey, int quantity)
        {
            var product = await _productTableStorageService.GetProductAsync(rowKey);

            if (product == null)
            {
                _logger.LogWarning($"Product with PartitionKey: {partitionKey} and RowKey: {rowKey} not found.");
                TempData["ErrorMessage"] = $"Product with PartitionKey: {partitionKey} and RowKey: {rowKey} not found.";
                return RedirectToAction("Index");
            }

            product.Quantity = quantity;

            CartItemViewModel viewModel = new CartItemViewModel
            {
                Product = product,
                Quantity = quantity
            };

            return View(viewModel);
        }

        // POST: ShoppingCart/EditQuantity
        [HttpPost]
        public async Task<IActionResult> EditQuantity(CartItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var success = await _shoppingCartService.EditQuantity(viewModel.Product.Id, viewModel.Quantity);

                if (success)
                {
                    if (viewModel.Quantity > 0)
                    {
                        _logger.LogInformation($"Product {viewModel.Product.Name} quantity updated to {viewModel.Quantity}");
                        TempData["SuccessMessage"] = $"Product {viewModel.Product.Name}  quantity updated to  {viewModel.Quantity}";
                    }
                    else
                    {
                        _logger.LogInformation($"Product {viewModel.Product.Name} removed from cart");
                        TempData["SuccessMessage"] = $"Product {viewModel.Product.Name}  removed from cart";
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogWarning($"Product {viewModel.Product.Name} quantity could not be updated");
                    TempData["ErrorMessage"] = $"Product {viewModel.Product.Name} quantity could not be updated.";
                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }

        // POST: ShoppingCart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string partitionKey, string rowKey)
        {
            var success = await _shoppingCartService.RemoveFromCart(rowKey);

            if (success)
            {
                _logger.LogInformation($"Product with PartitionKey: {partitionKey} and RowKey: {rowKey} removed from cart.");
                TempData["SuccessMessage"] = "Product removed from cart successfully.";
            }
            else
            {
                _logger.LogWarning($"Failed to remove product with PartitionKey: {partitionKey} and RowKey: {rowKey} from cart.");
                TempData["ErrorMessage"] = "Failed to remove product from cart.";
            }

            return RedirectToAction("Index");
        }

        // POST: ShoppingCart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var success = await _shoppingCartService.ClearCart();

            if (success)
            {
                _logger.LogInformation("Cart cleared");
                TempData["SuccessMessage"] = "Cart cleared successfully.";
            }
            else
            {
                _logger.LogWarning("Failed to clear cart");
                TempData["ErrorMessage"] = "Failed to clear cart.";
            }

            return RedirectToAction("Index");
        }

        // POST: ShoppingCart/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cartItems = _shoppingCartService.GetCartItems();

                var items = cartItems.Select(item => new CartItemViewModel
                {
                    Product = item,
                    Quantity = item.Quantity,
                    TotalPrice = item.Price * item.Quantity
                }).ToList();

                // Calculate total price
                decimal total = items.Sum(i => i.TotalPrice);

                // Pass items and total to the view model
                var model = new CartIndexViewModel
                {
                    CartItems = items,
                    GrandTotal = total
                };

                if (model != null && model.CartItems != null && model.CartItems.Count > 0)
                {
                    string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    string orderId = Guid.NewGuid().ToString();

                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("User not found.");
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction("Index");
                    }

                    List<Order> orderEntities = new List<Order>();

                    foreach (var cartItem in model.CartItems)
                    {
                        var item = new Order
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = orderId,
                            ProductId = cartItem.Product.Id,
                            OrderDate = DateTime.Now,
                            UserId = userId,
                            Quantity = cartItem.Quantity,
                            TotalItemPrice = cartItem.TotalPrice,
                            ItemStatus = "Pending"
                        };

                        orderEntities.Add(item);
                    }

                    var result = await _orderTableStorageService.CreateOrderAsync(orderEntities);

                    if (result)
                    {
                        // Get the current users username before they logout
                        var username = User.FindFirstValue(ClaimTypes.Name);

                        _logger.LogInformation($"User {username} created order {orderId} at {DateTime.Now.ToString()}");

                        await _orderQueueService.SendMessageAsync($"User {username} created order {orderId} at {DateTime.Now.ToString()}", orderQueueName);

                        TempData["SuccessMessage"] = $"Order ({orderId}) created successfully";

                        // Decrease the quantity of all items in the order
                        foreach (var item in model.CartItems)
                        {
                            var product = await _productTableStorageService.GetProductAsync(item.Product.Id);

                            if (product != null)
                            {
                                _logger.LogInformation($"Decreasing quantity of product {product.Name} by {item.Quantity}");
                                product.Quantity -= item.Quantity;
                                await _productTableStorageService.UpdateProductAsync(product);
                            }
                        }

                        var success = await _shoppingCartService.ClearCart();

                        if (success)
                        {
                            _logger.LogInformation("Cart cleared");
                            TempData["SuccessMessage"] += (TempData["SuccessMessage"] != null ?
                                "\nCart cleared successfully" : "Cart cleared successfully");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to clear cart");
                            TempData["ErrorMessage"] = "Failed to clear cart.";
                        }

                        // Generate the invoice for the order
                        var invoiceFile = await _invoiceGenerationService.GenerateInvoiceAsync(userId, orderId);

                        // Upload the invoice to the file storage
                        if (invoiceFile != null)
                        {
                            string fileName = $"{orderId}_invoice.pdf";

                            bool status = await _fileService.FileUploadAsync(invoiceFile, fileName);

                            if (!status)
                            {
                                TempData["ErrorMessage"] = "Failed to upload invoice";
                            }
                        }

                        return RedirectToAction("Index", "CustomerOrders");
                    }
                    else
                    {
                        // Get the current users username before they logout
                        var username = User.FindFirstValue(ClaimTypes.Name);

                        _logger.LogWarning($"User {username} failed to create order {orderId} at {DateTime.Now.ToString()}");

                        await _orderQueueService.SendMessageAsync($"User {username} failed to create order {orderId} at {DateTime.Now.ToString()}", orderQueueName);

                        TempData["ErrorMessage"] = $"Failed to create order at {DateTime.Now.ToString()}";
                        return RedirectToAction("Index");
                    }
                }

                _logger.LogWarning("No items in cart to checkout");
                TempData["ErrorMessage"] = "No items in cart to checkout";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to create order: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
