using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ST10361554_CLDV6212_POE_Part_3_Functions
{
    public class FileServiceFunctions
    {
        private readonly ILogger<FileServiceFunctions> _logger;
        private ShareClient _shareClient;

        public FileServiceFunctions(ILogger<FileServiceFunctions> logger)
        {
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (connectionString == null)
            {
                throw new ArgumentNullException("AzureWebJobsStorage environment variable is not set.");
            }

            _shareClient = new ShareClient(connectionString, "abc-customer-invoices");

            _shareClient.CreateIfNotExists();
        }

        private ShareDirectoryClient GetShareDirectoryClient(string directoryName)
        {
            #region Validate the input

            // Validate the input
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                _logger.LogError("Directory name is required.");
                throw new ArgumentException("Directory name cannot be null or empty.", nameof(directoryName));
            }

            #endregion

            #region Create the directory client if it doesn't exist

            ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directoryName);

            try
            {
                // Attempt to create the directory if it doesn't exist
                directoryClient.CreateIfNotExists();
            }
            catch (RequestFailedException rfEx)
            {
                // Handle specific Azure Storage exceptions
                _logger.LogError($"Failed to create directory '{directoryName}': {rfEx.Message} (Status Code: {rfEx.Status})");
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                _logger.LogError($"An unexpected error occurred while creating directory '{directoryName}': {ex.Message}");
                throw;
            }

            #endregion

            return directoryClient;
        }

        /*
           * Code Attribution:
           * Uploading a file to a file share in Azure Storage
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Files.Shares/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(FileUploadAsync))]
        public async Task<IActionResult> FileUploadAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            // Ensure the request has content
            if (req.ContentLength == 0)
            {
                _logger.LogError("Request body is empty.");
                return new BadRequestObjectResult("Request body is empty.");
            }

            try
            {
                #region Read the form data and retrieve the parameters, validate inputs

                // Read the form data
                var formCollection = await req.ReadFormAsync();

                // Retrieve the parameters from the form data
                string? customerId = formCollection["customerId"];

                var file = formCollection.Files.FirstOrDefault();

                string? fileName = formCollection["fileName"];

                // Validate inputs
                if (string.IsNullOrEmpty(customerId))
                {
                    _logger.LogError("Customer ID is required.");
                    return new BadRequestObjectResult("Customer ID is required.");
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    _logger.LogError("File name is required.");
                    return new BadRequestObjectResult("File name is required.");
                }

                if (file == null || file.Length == 0)
                {
                    _logger.LogError("No file uploaded.");
                    return new BadRequestObjectResult("No file uploaded.");
                }

                #endregion

                #region get the directory client, check if the directory exists and get a reference to the file, check if the file already exists

                // Get the directory client
                var directoryClient = GetShareDirectoryClient(customerId);

                // check if the directory exists
                bool directoryExists = await directoryClient.ExistsAsync();

                if (!directoryExists)
                {
                    _logger.LogError($"Directory '{customerId}' does not exist.");
                    return new BadRequestObjectResult($"Directory '{customerId}' does not exist.");
                }

                // Get a reference to the file
                var fileClient = directoryClient.GetFileClient(fileName);

                // Check if the file already exists
                bool fileExists = await fileClient.ExistsAsync();

                if (await fileClient.ExistsAsync())
                {
                    try
                    {
                        await fileClient.DeleteAsync(); // Handle potential exceptions
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, $"Error deleting existing file '{fileName}'.");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                }

                #endregion

                #region Upload the file and check if the file was uploaded successfully

                // Upload the file
                using (var fileStream = file.OpenReadStream())
                {
                    await fileClient.CreateAsync(file.Length); // Create the file before uploading

                    var uploadResponse = await fileClient.UploadAsync(fileStream);

                    // check if the file was uploaded successfully
                    if (uploadResponse.GetRawResponse().Status != StatusCodes.Status201Created)
                    {
                        _logger.LogError($"Failed to upload file '{fileName}' for customer '{customerId}'.");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }

                    _logger.LogInformation($"File '{fileName}' uploaded successfully for customer '{customerId}'.");
                    return new OkObjectResult($"File '{fileName}' uploaded successfully for customer '{customerId}'.");
                }

                #endregion

            }
            catch (RequestFailedException rfEx)
            {
                // Handle specific Azure Storage exceptions
                _logger.LogError($"Azure Storage error while uploading file: {rfEx.Message} (Status Code: {rfEx.Status})");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while uploading the file.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /*
           * Code Attribution:
           * Downloading a file from a file share in Azure Storage
           * Jonathan Cárdenas
           * 15 July 2024
           * GitHub: azure-sdk-for-net
           * https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Files.Shares/samples/Sample01b_HelloWorldAsync.cs
        */

        [Function(nameof(FileDownloadAsync))]
        public async Task<IActionResult> FileDownloadAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            #region Retrieve the parameters and validate inputs

            // get the customer ID and file name from the query string
            string? customerId = req.Query["customerId"];

            string? fileName = req.Query["fileName"];

            // Validate inputs
            if (string.IsNullOrEmpty(customerId))
            {
                _logger.LogError("Customer ID is required.");
                return new BadRequestObjectResult("Customer ID is required.");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogError("File name is required.");
                return new BadRequestObjectResult("File name is required.");
            }

            #endregion

            try
            {
                #region Get the directory client, check if the directory exists, get a reference to the file, check if the file already exists

                // Get the directory client
                var directoryClient = GetShareDirectoryClient(customerId);

                // check if the directory exists
                bool directoryExists = await directoryClient.ExistsAsync();

                if (!directoryExists)
                {
                    _logger.LogError($"Directory '{customerId}' does not exist.");
                    return new BadRequestObjectResult($"Directory '{customerId}' does not exist.");
                }

                // Get a reference to the file
                var fileClient = directoryClient.GetFileClient(fileName);

                // Check if the file already exists
                bool fileExists = await fileClient.ExistsAsync();

                #endregion

                #region Download the file and return the file content

                if (fileExists)
                {
                    var memoryStream = new MemoryStream();
                    ShareFileDownloadInfo download = await fileClient.DownloadAsync();

                    await download.Content.CopyToAsync(memoryStream);

                    memoryStream.Position = 0;

                    var properties = await fileClient.GetPropertiesAsync();
                    var contentType = properties.Value.ContentType;

                    _logger.LogInformation($"File '{fileName}' downloaded successfully.");

                    // Return the file as a downloadable content result
                    return new FileContentResult(memoryStream.ToArray(), contentType)
                    {
                        FileDownloadName = fileName
                    };
                }

                _logger.LogError($"File '{fileName}' does not exist.");
                return new BadRequestObjectResult($"File '{fileName}' does not exist.");

                #endregion

            }
            catch (RequestFailedException rfEx)
            {
                // Handle specific Azure Storage exceptions
                _logger.LogError($"Azure Storage error while downloading file: {rfEx.Message} (Status Code: {rfEx.Status})");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"An error occurred while downloading the file: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
