using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ST10361554_CLDV6212_POE_Part_3_Functions
{
    public class ProductBlobStorageFunctions
    {
        private readonly ILogger<ProductBlobStorageFunctions> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public ProductBlobStorageFunctions(ILogger<ProductBlobStorageFunctions> logger)
        {
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (connectionString == null)
            {
                throw new ArgumentNullException("AzureWebJobsStorage environment variable is not set.");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // Get the blob container client for the specified category
        private async Task<BlobContainerClient> GetBlobContainerClient(string category)
        {
            #region Creates a new blob container client for the specified category

            try
            {
                // Validate the category parameter
                if (string.IsNullOrWhiteSpace(category))
                {
                    throw new ArgumentNullException(nameof(category), "Category cannot be null or empty.");
                }

                // Ensure the blob service client is available
                if (_blobServiceClient == null)
                {
                    throw new InvalidOperationException("Blob service client has not been initialized.");
                }

                // Get the BlobContainerClient for the specified category
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(category.ToLower());

                // Attempt to create the container if it doesn't exist
                var response = await blobContainerClient.CreateIfNotExistsAsync();

                // Check if the container was created or already exists
                if (response != null && response.GetRawResponse().Status == 201)  // Status 201 = Created
                {
                    _logger.LogInformation($"{category} container created successfully.");
                }
                else
                {
                    _logger.LogInformation($"{category} container already exists.");
                }

                return blobContainerClient;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning($"Invalid argument: {ex.ParamName}. {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log detailed error message
                _logger.LogError(ex, $"Error getting blob container client for category: {category}");
                throw;
            }

            #endregion
        }

        /*
           * Code Attribution:
           * Deleting a blob in a blob container in Azure Storage
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(DeleteProductImageAsync))]
        public async Task<IActionResult> DeleteProductImageAsync([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequest req)
        {
            #region retrieve and validate the category and image name from the query string

            // get the category and image name from the query string
            string? category = req.Query["category"];

            string? imageName = req.Query["imageName"];

            if (string.IsNullOrEmpty(category))
            {
                _logger.LogWarning("Category is missing in the query string.");
                return new BadRequestObjectResult("Please pass a category on the query string.");
            }

            if (string.IsNullOrEmpty(imageName))
            {
                _logger.LogWarning("Image name is missing in the query string.");
                return new BadRequestObjectResult("Please pass an image name on the query string.");
            }

            _logger.LogInformation($"Deleting blob in container: {category} and image name: {imageName}");

            #endregion

            try
            {
                #region get the blob container client and delete the blob

                var blobContainerClient = await GetBlobContainerClient(category);

                // check if the blob container client is null
                if (blobContainerClient == null)
                {
                    throw new InvalidOperationException($"Failed to retrieve BlobContainerClient for category: {category}");
                }

                // Get the blob client for the specified image name
                var blobClient = blobContainerClient.GetBlobClient(imageName);

                // Delete the blob
                var response = await blobClient.DeleteIfExistsAsync();

                #endregion

                #region check the response status and return the appropriate response

                // check the response status
                if (response)
                {
                    _logger.LogInformation($"Blob {imageName} deleted successfully");
                    return new OkObjectResult($"Blob {imageName} deleted successfully");
                }
                else
                {
                    _logger.LogInformation($"Blob {imageName} could not be deleted because it does not exist.");
                    return new NotFoundObjectResult($"Blob {imageName} could not be deleted because it does not exist.");
                }

                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob for imageName: {imageName} in category: {category}");
                return new BadRequestObjectResult($"Error deleting blob: {ex.Message}");
            }
        }

        /*
           * Code Attribution:
           * Downloading a blob from a blob container in Azure Storage
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(GetProductImageAsync))]
        public async Task<IActionResult> GetProductImageAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            #region retrieve and validate the category and image name from the query string

            // get the category and image name from the query string
            string? category = req.Query["category"];

            string? imageName = req.Query["imageName"];

            if (string.IsNullOrEmpty(category))
            {
                _logger.LogWarning("Category is missing in the query string.");
                return new BadRequestObjectResult("Please pass a category on the query string.");
            }

            if (string.IsNullOrEmpty(imageName))
            {
                _logger.LogWarning("Image name is missing in the query string.");
                return new BadRequestObjectResult("Please pass an image name on the query string.");
            }

            _logger.LogInformation($"Downloading blob in container: {category} and image name: {imageName}");

            #endregion

            try
            {
                #region get the blob container client and check if the blob exists

                var blobContainerClient = await GetBlobContainerClient(category);

                // check if the blob container client is null
                if (blobContainerClient == null)
                {
                    throw new InvalidOperationException($"Failed to retrieve BlobContainerClient for category: {category}");
                }

                // Get the blob client for the specified image name
                var blobClient = blobContainerClient.GetBlobClient(imageName);

                // check if the blob exists
                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogInformation($"Blob {imageName} could not be downloaded because it does not exist.");
                    return new NotFoundObjectResult($"Blob {imageName} could not be downloaded because it does not exist.");
                }

                #endregion

                #region download the blob and return the byte array

                var memoryStream = new MemoryStream();

                // Retrieve the blob properties to get the content type
                var blobProperties = await blobClient.GetPropertiesAsync();
                var contentType = blobProperties.Value.ContentType;

                // Download the blob from the blob client
                var response = await blobClient.DownloadToAsync(memoryStream);

                // Check if the download was successful
                if (response.Status == 200 || response.Status == 206) // 200: Success, 206: Partial Content 
                {
                    _logger.LogInformation($"Blob {imageName} downloaded successfully.");

                    // Reset the memory stream position to read from the beginning
                    memoryStream.Position = 0;

                    // Return the memory stream as a file with the appropriate content type
                    return new FileContentResult(memoryStream.ToArray(), contentType);
                }
                else
                {
                    // Handle the case where download status is not OK
                    _logger.LogWarning($"Blob {imageName} download failed with status: {response.Status}");
                    return new NotFoundObjectResult($"Blob {imageName} could not be downloaded.");
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading blob for imageName: {imageName} in category: {category}");
                return new BadRequestObjectResult($"Error downloading blob: {ex.Message}");
            }
        }

        /*
           * Code Attribution:
           * Uploading a blob to a blob container in Azure Storage
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(UploadProductImageAsync))]
        public async Task<IActionResult> UploadProductImageAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            #region Retrieve and validate the form data

            // Check if the request has any form data
            if (!req.HasFormContentType)
            {
                _logger.LogWarning("No form data was sent with the request.");
                return new BadRequestObjectResult("No form data was sent with the request.");
            }

            // Extract category and rowKey from form data or query string
            string? category = req.Form["category"];

            string? rowKey = req.Form["rowKey"];

            // Validate category and rowKey
            if (string.IsNullOrEmpty(category))
            {
                _logger.LogWarning("Category is missing in the form data.");
                return new BadRequestObjectResult("Please pass a category in the form data.");
            }

            if (string.IsNullOrEmpty(rowKey))
            {
                _logger.LogWarning("RowKey (image name) is missing in the form data.");
                return new BadRequestObjectResult("Please pass a rowKey in the form data.");
            }

            // Retrieve the uploaded file
            var file = req.Form.Files["file"];

            // Validate the file
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File is missing or empty.");
                return new BadRequestObjectResult("Please pass a valid image file in the form data.");
            }

            _logger.LogInformation($"Starting upload of image {rowKey} to category {category}");

            #endregion

            try
            {
                #region Upload file to blob storage

                // Get the blob container client
                var blobContainerClient = await GetBlobContainerClient(category);

                if (blobContainerClient == null)
                {
                    _logger.LogWarning($"Error getting blob container ({category})");
                    return new BadRequestObjectResult("Error getting blob container.");
                }

                // Get the blob client for the image using rowKey as blob name
                var blobClient = blobContainerClient.GetBlobClient(rowKey);

                // Upload the file to blob storage
                var response = await blobClient.UploadAsync(file.OpenReadStream(), true);

                // Check if the upload was successful
                if (response.GetRawResponse().Status == 201)  // Status 201 = Created
                {
                    _logger.LogInformation($"Blob {rowKey} uploaded successfully");
                    return new OkObjectResult($"Blob {rowKey} uploaded successfully");
                }
                else
                {
                    _logger.LogInformation($"Blob {rowKey} could not be uploaded.");
                    return new BadRequestObjectResult($"Blob {rowKey} could not be uploaded.");
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading blob {rowKey} to category {category}: {ex.Message}");
                return new BadRequestObjectResult($"Error uploading image: {ex.Message}");
            }
        }

        /*
           * Code Attribution:
           * Checking a blob in a blob container in Azure Storage exists
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(CheckImageExistsAsync))]
        public async Task<IActionResult> CheckImageExistsAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            #region retrieve and validate the category and image name from the query string

            // get the category and image name from the query string
            string? category = req.Query["category"];

            string? imageName = req.Query["imageName"];

            if (string.IsNullOrEmpty(category))
            {
                _logger.LogWarning("Category is missing in the query string.");
                return new BadRequestObjectResult("Please pass a category on the query string.");
            }

            if (string.IsNullOrEmpty(imageName))
            {
                _logger.LogWarning("Image name is missing in the query string.");
                return new BadRequestObjectResult("Please pass an image name on the query string.");
            }

            _logger.LogInformation($"Checking if blob exists in container: {category} and image name: {imageName}");

            #endregion

            try
            {
                #region get the blob container client and blob client

                var blobContainerClient = await GetBlobContainerClient(category);

                // check if the blob container client is null
                if (blobContainerClient == null)
                {
                    throw new InvalidOperationException($"Failed to retrieve BlobContainerClient for category: {category}");
                }

                // Get the blob client for the specified image name
                var blobClient = blobContainerClient.GetBlobClient(imageName);

                #endregion

                #region check if the blob exists and return the appropriate response

                // check if the blob exists
                if (await blobClient.ExistsAsync())
                {
                    _logger.LogInformation($"Blob {imageName} exists in container {category}");
                    return new OkObjectResult($"Blob {imageName} exists in container {category}");
                }
                else
                {
                    _logger.LogInformation($"Blob {imageName} does not exist in container {category}");
                    return new NotFoundObjectResult($"Blob {imageName} does not exist in container {category}");
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if blob exists for imageName: {imageName} in category: {category}");
                return new BadRequestObjectResult($"Error checking if blob exists: {ex.Message}");
            }
        }
    }
}
