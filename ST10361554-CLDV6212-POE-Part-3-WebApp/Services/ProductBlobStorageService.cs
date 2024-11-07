using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using System.Net.Http.Headers;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class ProductBlobStorageService : IProductBlobStorageService
    {
        IConfiguration _configuration;
        private readonly ILogger<ProductBlobStorageService> _logger;
        private readonly HttpClient _httpClient;

        // Constructor to initialize the BlobServiceClient and Logger
        public ProductBlobStorageService(
            ILogger<ProductBlobStorageService> logger,
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

        // Upload the image to the blob storage
        public async Task<bool> UploadProductImageAsync(string category, string productId, IFormFile file)
        {
            #region Validate inputs and get the request url from the configuration

            // Validate inputs
            if (string.IsNullOrEmpty(category))
            {
                _logger.LogError("Category cannot be null or empty.");
                return false;
            }

            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogError("Row key cannot be null or empty.");
                return false;
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogError("File cannot be null or empty.");
                return false;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["ProductBlobStorageUrls:UploadProductImageAsync"];

            // check if the request url is null or empty
            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Error getting request url from configuration");
                return false;
            }

            #endregion

            try
            {
                #region create the form data and send the POST request to the function

                // Create the form data
                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(category), "category");
                formData.Add(new StringContent(productId), "productId");

                using var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentLength = file.Length;
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                formData.Add(streamContent, "file", file.FileName);

                // send the POST request to the function
                var response = await _httpClient.PostAsync(requestUrl, formData);

                #endregion

                #region Check if the response is successful

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    var resultMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Upload successful: {resultMessage}");
                    return true;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Upload failed: {errorMessage}");
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogInformation($"Error uploading product ({productId}) image: {ex.Message}");

                return false;
            }
        }

        // Download the image from the blob storage
        public async Task<FileContentResult?> DownloadProductImageAsync(string category, string productId)
        {
            #region validate the category and product id and get the request url from the configuration

            // validate the category and product id
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productId))
            {
                _logger.LogWarning("Category or image name is null or empty.");
                return null;
            }

            // Get the request URL from the configuration
            string? requestUrl = _configuration["ProductBlobStorageUrls:GetProductImageAsync"];

            // validate the request URL
            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogWarning("Request URL for getting product image is not configured.");
                return null;
            }

            #endregion

            try
            {
                #region Send a GET request to the function

                // Construct the full URL with parameters
                string constructedUrl = $"{requestUrl}?category={category}&imageName={productId}";

                // Send a GET request to the function
                var response = await _httpClient.GetAsync(constructedUrl);

                #endregion

                #region Check if the response is successful and return the image as a memory stream

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {

                    // Read the byte array from the response content
                    var byteArray = await response.Content.ReadAsByteArrayAsync();

                    _logger.LogInformation("Image downloaded successfully.");

                    // Return the FileContentResult with byte array and content type
                    return new FileContentResult(byteArray, response.Content.Headers.ContentType!.ToString());
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error {response.StatusCode}: {errorMessage}");
                    return null;
                }

                #endregion

            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the HTTP request
                _logger.LogError($"Error getting product image ({productId}): {ex.Message}");

                return null;
            }
        }

        // Delete the image from the blob storage
        public async Task<bool> DeleteProductImageAsync(string category, string productId)
        {
            #region validate the category and product id and get the request url from the configuration

            // validate the category and product id
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productId))
            {
                _logger.LogError("Invalid category or product id");
                return false;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["ProductBlobStorageUrls:DeleteProductImageAsync"];

            // check if the request url is null or empty
            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Error getting request url from configuration");
                return false;
            }

            #endregion

            try
            {
                #region Send a DELETE request to the function

                // add the parameters to the request url
                string constructedUrl = $"{requestUrl}?category={category}&imageName={productId}";

                // Send a DELETE request to the function 
                var response = await _httpClient.DeleteAsync(constructedUrl);

                #endregion

                #region Check if the response is successful

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    var successMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(successMessage);
                    return true;
                }
                else
                {
                    // Read the error message from the response
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    // Log the error message
                    _logger.LogError($"Error {response.StatusCode}: {errorMessage}");

                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogInformation($"Error deleting product ({productId}) image: {ex.Message}");

                return false;
            }
        }

        // Check if the image exists in the blob storage
        public async Task<bool> ImageExistsAsync(string category, string productId)
        {
            #region Validate the category and productId and get the request url from the configuration

            // Validate the category and rowKey
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productId))
            {
                _logger.LogWarning("Category or image name is null or empty.");
                return false;
            }

            // get the request url from the configuration
            string? requestUrl = _configuration["ProductBlobStorageUrls:CheckImageExistsAsync"];

            // check if the request url is null or empty
            if (string.IsNullOrEmpty(requestUrl))
            {
                _logger.LogError("Error getting request url from configuration");
                return false;
            }

            #endregion

            try
            {
                #region Send a GET request to the function

                // Construct the full URL with parameters
                string constructedUrl = $"{requestUrl}?category={category}&imageName={productId}";

                // Send a GET request to the function
                var response = await _httpClient.GetAsync(constructedUrl);

                #endregion

                #region Check if the response is successful

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    var successMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(successMessage);
                    return true;
                }
                else
                {
                    // Read the error message from the response
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    // Log the error message
                    _logger.LogError($"Error {response.StatusCode}: {errorMessage}");

                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogInformation($"Error checking if product ({productId}) image exists: {ex.Message}");

                return false;
            }
        }
    }
}
