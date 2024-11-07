using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;
using ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductDatabaseService _productService;
        private readonly ILogger<ProductController> _logger;
        private readonly ICategoryService _categoryService;
        private readonly IProductBlobStorageService _blobStorageService;
        private readonly string queueName = "product-inventory-queue";
        private readonly QueueService _queueService;

        // Constructor to initialize services
        public ProductController(IProductDatabaseService productService,
            ILogger<ProductController> logger,
            ICategoryService categoryService,
            IProductBlobStorageService blobStorageService,
            IQueueServiceFactory queueServiceFactory)
        {
            _productService = productService;
            _logger = logger;
            _categoryService = categoryService;
            _blobStorageService = blobStorageService;
            _queueService = queueServiceFactory.Create(queueName);
        }

        // GET: Product/Index
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var products = await _productService.GetAllProductsAsync();

            // Filter the products by category if provided
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category).ToList();
                TempData["SuccessMessage"] = $"Displaying products for category {category}";
            }

            var viewModel = new ProductIndexViewModel
            {
                Products = new List<ProductViewModel>(),
                Categories = _categoryService.GetCategories(),
                SelectedCategory = category
            };

            // Populate the view model with product details and images
            foreach (var p in products)
            {
                string? base64Image = null;

                // Get the current product image from the blob storage
                var imageExists = await _blobStorageService.ImageExistsAsync(p.Category, p.Id);

                if (imageExists)
                {

                    var fileContentResult = await _blobStorageService.DownloadProductImageAsync(p.Category, p.Id);

                    if (fileContentResult != null)
                    {
                        // Get the byte array from the FileContentResult
                        var imageBytes = fileContentResult.FileContents;

                        // Convert byte array to base64 string
                        base64Image = Convert.ToBase64String(imageBytes);
                    }

                    viewModel.Products.Add(new ProductViewModel
                    {
                        ProductName = p.Name,
                        ProductDescription = p.Description,
                        ProductPrice = p.Price,
                        ProductQuantity = p.Quantity,
                        Base64ProductImage = base64Image!,
                        Category = p.Category,
                        ProductId = p.Id
                    });
                }
                else
                {
                    viewModel.Products.Add(new ProductViewModel
                    {
                        ProductName = p.Name,
                        ProductDescription = p.Description,
                        ProductPrice = p.Price,
                        ProductQuantity = p.Quantity,
                        Base64ProductImage = base64Image!,
                        Category = p.Category,
                        ProductId = p.Id
                    });
                }
            }

            // Display success or error message
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        // GET: Product/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _categoryService.GetCategories();

            return View();
        }

        [Authorize(Roles = "Admin")]
        // POST: Product/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Category = model.Category,
                    Id = Guid.NewGuid().ToString(),
                    Name = model.ProductName,
                    Description = model.ProductDescription,
                    Price = model.ProductPrice,
                    Quantity = model.ProductQuantity
                };

                bool productResult = await _productService.AddProductAsync(product);

                bool uploadResult = true; // Default to true, will set to false if there's an image and it fails.

                // Upload the image to the blob storage
                if (model.Base64ProductImage != null)
                {
                    uploadResult = await _blobStorageService.UploadProductImageAsync(product.Category, product.Id, model.Base64ProductImage);
                }

                // Handle success case
                if (productResult && uploadResult)
                {
                    _logger.LogInformation($"Product {product.Name} added successfully");
                    await _queueService.SendMessageAsync($"Product {product.Name} added successfully at {DateTime.Now.ToString()}", queueName);
                    TempData["SuccessMessage"] = $"Product {product.Name} added successfully";
                    return RedirectToAction("Index");
                }

                // Handle product add failure
                if (!productResult)
                {
                    _logger.LogWarning($"Error adding product {product.Name}");
                    await _queueService.SendMessageAsync($"Error adding product {product.Name} at {DateTime.Now.ToString()}", queueName);
                    TempData["ErrorMessage"] = "Error adding product";
                }

                // Handle image upload failure, ensuring that if product creation fails, both errors are logged
                if (!uploadResult)
                {
                    _logger.LogWarning($"Error uploading product image for {product.Name}");
                    await _queueService.SendMessageAsync($"Error uploading product image for {product.Name} at {DateTime.Now.ToString()}", queueName);
                    TempData["ErrorMessage"] += (TempData["ErrorMessage"] != null ? " and image upload failed." : "Error uploading product image");
                }

                return RedirectToAction("Index");
            }

            ViewBag.Categories = _categoryService.GetCategories();

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        // GET: Product/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _productService.GetProductAsync(rowKey);

            // Get the current product image from the blob storage
            var imageExists = await _blobStorageService.ImageExistsAsync(partitionKey, rowKey);

            string? base64ProductImage = null;

            if (imageExists)
            {

                var fileContentResult = await _blobStorageService.DownloadProductImageAsync(partitionKey, rowKey);

                if (fileContentResult != null)
                {
                    // Get the byte array from the FileContentResult
                    var imageBytes = fileContentResult.FileContents;

                    // Convert byte array to base64 string
                    base64ProductImage = Convert.ToBase64String(imageBytes);
                }
            }

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";

                return RedirectToAction("Index");
            }

            var viewModel = new EditProductViewModel
            {
                ProductID = product.Id,
                ProductName = product.Name,
                ProductDescription = product.Description,
                ProductPrice = product.Price,
                ProductQuantity = product.Quantity,
                Base64ProductImage = null!,
                Category = product.Category
            };

            ViewBag.Categories = _categoryService.GetCategories();
            ViewBag.CurrentImage = base64ProductImage;

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        // POST: Product/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(EditProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Category = model.Category,
                    Id = model.ProductID,
                    Name = model.ProductName,
                    Description = model.ProductDescription,
                    Price = model.ProductPrice,
                    Quantity = model.ProductQuantity
                };

                bool updateResult = await _productService.UpdateProductAsync(product);

                bool uploadResult = true; // Default to true, only set to false if image upload fails.

                // Upload the image to Blob Storage if a new image is provided
                if (model.Base64ProductImage != null)
                {
                    uploadResult = await _blobStorageService.UploadProductImageAsync(product.Category, product.Id, model.Base64ProductImage);
                }

                // Handle success case
                if (updateResult && uploadResult)
                {
                    _logger.LogInformation($"Product {product.Name} updated successfully.");
                    await _queueService.SendMessageAsync($"Product {product.Name} updated successfully at {DateTime.Now.ToString()}", queueName);
                    TempData["SuccessMessage"] = $"Product {product.Name} updated successfully.";
                    return RedirectToAction("Index");
                }

                // Handle product update failure
                if (!updateResult)
                {
                    _logger.LogWarning($"Error updating product {product.Name}.");
                    await _queueService.SendMessageAsync($"Error updating product {product.Name} at {DateTime.Now.ToString()}", queueName);
                    TempData["ErrorMessage"] = "Error updating product.";
                }

                // Handle image upload failure
                if (!uploadResult)
                {
                    _logger.LogWarning($"Error uploading product image for {product.Name}.");
                    await _queueService.SendMessageAsync($"Error uploading product image for {product.Name} at {DateTime.Now.ToString()}", queueName);
                    TempData["ErrorMessage"] += (TempData["ErrorMessage"] != null ? " and image upload failed." : "Error uploading product image.");
                }

                // In case of any error, redirect to the Index with error messages
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _categoryService.GetCategories();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        // GET: Product/Delete
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _productService.GetProductAsync(rowKey);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";

                return RedirectToAction("Index");
            }

            // Get the current product image from the blob storage
            var imageExists = await _blobStorageService.ImageExistsAsync(partitionKey, rowKey);

            string? base64ProductImage = null;

            if (imageExists)
            {

                var fileContentResult = await _blobStorageService.DownloadProductImageAsync(partitionKey, rowKey);

                if (fileContentResult != null)
                {
                    // Get the byte array from the FileContentResult
                    var imageBytes = fileContentResult.FileContents;

                    // Convert byte array to base64 string
                    base64ProductImage = Convert.ToBase64String(imageBytes);
                }
            }

            var viewModel = new ProductViewModel
            {
                ProductName = product.Name,
                ProductDescription = product.Description,
                ProductPrice = product.Price,
                ProductQuantity = product.Quantity,
                Base64ProductImage = base64ProductImage!,
                Category = partitionKey,
                ProductId = rowKey
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        // POST: Product/Delete
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
            {
                _logger.LogWarning("Invalid partitionKey or rowKey provided for deletion.");
                await _queueService.SendMessageAsync($"Invalid partitionKey or rowKey provided for deletion at {DateTime.Now.ToString()}", queueName);
                TempData["ErrorMessage"] = "Invalid partition key or row key.";
                return RedirectToAction("Index");
            }

            // Delete the product from the table storage
            var result = await _productService.DeleteProductAsync(rowKey);

            // Delete the product image from the blob storage
            var imageDeleteResult = await _blobStorageService.DeleteProductImageAsync(partitionKey, rowKey);

            if (result && imageDeleteResult)
            {
                // Both product and image were deleted successfully
                _logger.LogInformation($"Product and image with partition key {partitionKey} and row key {rowKey} deleted successfully");

                await _queueService.SendMessageAsync($"Product and image with partition key {partitionKey} and row key {rowKey} deleted successfully at {DateTime.Now.ToString()}", queueName);

                TempData["SuccessMessage"] = $"Product and image with partition key {partitionKey} and row key {rowKey} deleted successfully";

                return RedirectToAction("Index");
            }
            else
            {
                // Handle individual failures
                if (!result)
                {
                    _logger.LogWarning($"Error deleting product with partition key {partitionKey} and row key {rowKey}");

                    await _queueService.SendMessageAsync($"Error deleting product with partition key {partitionKey} and row key {rowKey} at {DateTime.Now.ToString()}", queueName);

                    TempData["ErrorMessage"] = TempData["ErrorMessage"] != null
                        ? $"{TempData["ErrorMessage"]} Product deletion failed."
                        : "Error deleting product.";
                }

                if (!imageDeleteResult)
                {
                    _logger.LogWarning($"Error deleting image with partition key {partitionKey} and row key {rowKey}");

                    await _queueService.SendMessageAsync($"Error deleting image with partition key {partitionKey} and row key {rowKey} at {DateTime.Now.ToString()}", queueName);

                    TempData["ErrorMessage"] = TempData["ErrorMessage"] != null
                        ? $"{TempData["ErrorMessage"]} Image deletion failed."
                        : "Error deleting image.";
                }

                return RedirectToAction("Index");
            }
        }

        [Authorize]
        // GET: Product/Details
        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _productService.GetProductAsync(rowKey);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";

                return RedirectToAction("Index");
            }

            // Get the current product image from the blob storage
            var imageExists = await _blobStorageService.ImageExistsAsync(partitionKey, rowKey);

            string? base64ProductImage = null;

            if (imageExists)
            {

                var fileContentResult = await _blobStorageService.DownloadProductImageAsync(partitionKey, rowKey);

                if (fileContentResult != null)
                {
                    // Get the byte array from the FileContentResult
                    var imageBytes = fileContentResult.FileContents;

                    // Convert byte array to base64 string
                    base64ProductImage = Convert.ToBase64String(imageBytes);
                }
            }

            var viewModel = new ProductViewModel
            {
                ProductName = product.Name,
                ProductDescription = product.Description,
                ProductPrice = product.Price,
                ProductQuantity = product.Quantity,
                Base64ProductImage = base64ProductImage!,
                Category = product.Category,
                ProductId = product.Id
            };

            return View(viewModel);
        }
    }
}
