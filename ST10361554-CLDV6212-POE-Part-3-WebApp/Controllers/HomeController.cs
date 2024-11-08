using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels;
using System.Diagnostics;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductBlobStorageService _blobStorageService;
        private readonly IProductDatabaseService _productService;

        public HomeController(
            ILogger<HomeController> logger, 
            IProductDatabaseService productService, 
            IProductBlobStorageService blobStorageService)
        {
            _logger = logger;
            _productService = productService;
            _blobStorageService = blobStorageService;
        }

        // GET: Home/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Retrieve all products from the product service
            var products = await _productService.GetAllProductsAsync();

            // Get products that are in stock
            var inStockProducts = products.Where(x => x.Quantity > 0).ToList();

            // Randomize the order of the in-stock products
            var randomProducts = inStockProducts
                .OrderBy(x => Guid.NewGuid())  // Shuffle the list using a random GUID
                .Take(3)                      // Take up to 3 random products
                .ToList();

            List<ProductViewModel> productsViewModel = new List<ProductViewModel>();

            // Get all the images and create the view model
            foreach (var p in randomProducts)
            {
                string? base64Image = null;

                // Check if the product image exists in blob storage
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

                    // Add the product to the view model list
                    productsViewModel.Add(new ProductViewModel
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
                    // Add the product to the view model list without an image
                    productsViewModel.Add(new ProductViewModel
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

            // Return the view with the list of products
            return View(productsViewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
