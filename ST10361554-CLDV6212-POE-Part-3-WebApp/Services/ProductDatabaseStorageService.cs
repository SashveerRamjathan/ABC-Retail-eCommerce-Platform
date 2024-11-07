using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Collections.Concurrent;
using System.Text;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class ProductDatabaseStorageService : IProductDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductDatabaseStorageService> _logger;
        private readonly HttpClient _httpClient;

        // Constructor to initialize the TableClient and Logger
        public ProductDatabaseStorageService(
            IConfiguration configuration,
            ILogger<ProductDatabaseStorageService> logger,
            IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _logger = logger;
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

        // Method to add a product to the database
        public async Task<bool> AddProductAsync(Product product)
        {
            #region Check if the product is null

            // Check if the product is null
            if (product == null)
            {
                _logger.LogError("Attempted to add a null product.");
                return false;
            }

            #endregion

            try
            {
                #region Serialize the product and send POST request

                var json = JsonConvert.SerializeObject(product);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Get the URL from the configuration
                string? url = _configuration["ProductDatabaseStorageUrls:AddProductAsync"];

                // Check if the URL is valid
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("AddProductAsync URL is not configured.");
                    return false;
                }

                // Send POST request
                var response = await _httpClient.PostAsync(url, content);

                #endregion

                #region Check for success response

                // Check for success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product {product.Name} added successfully.");
                    return true;
                }
                else
                {
                    // Log unsuccessful response details
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to add product {product.Name}. Status code: {response.StatusCode}. Response: {responseContent}");
                    return false;
                }

                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {product.Name} to the product table: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            #region Check for null or empty productId

            // Check for null or empty partitionKey and rowKey
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogError("productId is null or empty.");
                return false;
            }

            #endregion

            try
            {
                #region Construct the URL and send DELETE request

                // Get the URL from the configuration
                string? url = _configuration["ProductDatabaseStorageUrls:DeleteProductAsync"];

                // Check if the URL is valid
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("DeleteProductAsync URL is not configured.");
                    return false;
                }

                // Construct the URL with the partition key and row key
                string constructedUrl = $"{url}?id={productId}";

                // Send DELETE request
                var response = await _httpClient.DeleteAsync(constructedUrl);

                #endregion

                #region Check for success response

                // Check for success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product with productId {productId} deleted successfully.");
                    return true;
                }
                else
                {
                    // Log unsuccessful response details
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to delete product with productId {productId}. Status code: {response.StatusCode}. Response: {responseContent}");
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex.Message, $"Error deleting product with productId {productId} from the product database");
                return false;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var entities = new List<Product>();

            try
            {
                #region Get Url from configuration and send GET request

                string? url = _configuration["ProductDatabaseStorageUrls:GetAllProducts"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("GetAllProducts URL is not configured.");
                    return entities;
                }

                var response = await _httpClient.GetAsync(url);

                #endregion

                #region Check for success response and deserialize the response content

                if (response.IsSuccessStatusCode)
                {
                    // Read and deserialize the response content
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var products = JsonConvert.DeserializeObject<List<Product>>(jsonResponse);

                    if (products != null && products.Count > 0)
                    {
                        entities = products;
                        _logger.LogInformation($"{entities.Count} products retrieved successfully.");
                    }
                    else
                    {
                        _logger.LogWarning("No products found in the response.");
                    }
                }
                else
                {
                    // Log the error and response
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to get all products. Status code: {response.StatusCode}. Response: {responseContent}");
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex.Message, "Error getting all products from the product table");
                return new List<Product>();
            }

            return entities;
        }

        public async Task<Product?> GetProductAsync(string productId)
        {
            #region Check for null or empty productId

            // Check for null or empty partitionKey and rowKey
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogError("productId is null or empty.");
                return null;
            }

            #endregion

            try
            {
                #region Construct the URL and send GET request

                string? url = _configuration["ProductDatabaseStorageUrls:GetProductAsync"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("GetProductAsync URL is not configured.");
                    return null;
                }

                // Construct the full URL with query parameters
                string constructedUrl = $"{url}?id={productId}";

                var response = await _httpClient.GetAsync(constructedUrl);

                #endregion

                #region Check for success response and deserialize the response content

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<Product>(jsonResponse);

                    if (product != null)
                    {
                        _logger.LogInformation($"Product {product.Name} retrieved successfully.");
                        return product;
                    }
                    else
                    {
                        _logger.LogError("Deserialization returned null for the product.");
                        return null;
                    }
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to get product. Status code: {response.StatusCode}. Response: {responseContent}");
                    return null;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex.Message, $"Error getting product with productId {productId} from the product database");
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            #region Check for null product

            // Check for null product
            if (product == null)
            {
                _logger.LogError("Product is null.");
                return false;
            }

            #endregion

            try
            {
                #region Serialize the product and send PUT request

                string? url = _configuration["ProductDatabaseStorageUrls:UpdateProductAsync"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("UpdateProductAsync URL is not configured.");
                    return false;
                }

                // Serialize product entity to JSON
                string jsonProduct = JsonConvert.SerializeObject(product);

                var content = new StringContent(jsonProduct, Encoding.UTF8, "application/json");

                // Make the PUT request to the Azure Function
                var response = await _httpClient.PutAsync(url, content);

                #endregion

                #region Check for success response

                // Check for success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product with product Id {product.Id} updated successfully.");
                    return true;
                }
                else
                {
                    // Log unsuccessful response details
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to update product with product Id {product.Id}. Status code: {response.StatusCode}. Response: {responseContent}");
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex.Message, $"Error updating product with product Id {product.Id} in the product database");
                return false;
            }
        }
    }
}
