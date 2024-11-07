using Azure;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Net.Http.Headers;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Service for managing file operations in Azure File Storage
    public class FileService : IFileService
    {
        private string? _customerDirectory;

        private readonly ILogger<FileService> _logger;

        private readonly QueueService _fileQueueService;
        private readonly string _fileQueueName = "file-queue";

        private readonly HttpClient _httpClient;

        private readonly IConfiguration _configuration;

        // Constructor to initialize the file service with connection string, logger, and queue service factory
        public FileService(
            ILogger<FileService> logger,
            IQueueServiceFactory queueServiceFactory,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _fileQueueService = queueServiceFactory.Create(_fileQueueName);
            _httpClient = clientFactory.CreateClient();
            _configuration = configuration;
        }

        // Set the customer directory for file operations
        public void SetCustomerDirectory(string customerId)
        {
            _customerDirectory = customerId;
        }

        /*
           * Code Attribution:
           * All the methods below use the http client to make HTTP requests to Azure Functions.
           * IEvangelist
           * 11 February 2023
           * Make HTTP requests with the HttpClient class
           * https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
        */

        // Download a file from the Azure File Storage
        public async Task<FileDownloadResult> FileDownloadAsync(string fileName)
        {
            #region input validation

            // Check if the customer directory is set
            if (string.IsNullOrWhiteSpace(_customerDirectory))
            {
                _logger.LogError("Customer directory is not set.");
                return null!;
            }

            // check if the file name is empty
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("File name is empty.");
                return null!;
            }

            #endregion

            #region get the URL from the configuration

            string? url = _configuration["FileServiceUrls:FileDownloadAsync"];

            // check if the url is empty
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogError("File upload URL is empty.");
                return null!;
            }

            #endregion

            try
            {
                #region construct and  send the request to the file download URL

                // Construct the full request URL
                string requestUrl = $"{url}?customerId={_customerDirectory}&fileName={fileName}";

                // Send a GET request to the Azure Function to download the file
                var response = await _httpClient.GetAsync(requestUrl);

                #endregion

                #region handle the response

                // Check the response status
                if (response.IsSuccessStatusCode)
                {
                    // Read the file content as a byte array
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                    // convert the file bytes to a memory stream
                    var fileStream = new MemoryStream(fileBytes);

                    // Create a file download result
                    var file = new FileDownloadResult
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Content = fileStream
                    };

                    await _fileQueueService.SendMessageAsync($"File '{fileName}' downloaded successfully for customer '{_customerDirectory}'", _fileQueueName);
                    _logger.LogInformation($"File '{fileName}' downloaded successfully.");
                    return file; // Indicate success
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error downloading file '{fileName}': {errorMessage}");
                    await _fileQueueService.SendMessageAsync($"Error downloading file '{fileName}': {errorMessage}", _fileQueueName);
                    return null!; // Indicate failure
                }

                #endregion

            }
            catch (RequestFailedException rfEx)
            {
                // Log the error
                _logger.LogError($"Azure Storage error while downloading file '{fileName}': {rfEx}");
                await _fileQueueService.SendMessageAsync($"Error downloading file '{fileName}': {rfEx.Message}", _fileQueueName);
                return null!;
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError($"An error occurred while downloading file ({fileName}): {ex.Message}");
                await _fileQueueService.SendMessageAsync($"Error downloading file '{fileName}'", _fileQueueName);
                return null!;
            }
        }

        // Upload a file to the Azure File Storage
        public async Task<bool> FileUploadAsync(byte[] bytes, string fileName)
        {
            #region input validation

            // Check if the customer directory is set
            if (string.IsNullOrWhiteSpace(_customerDirectory))
            {
                _logger.LogError("Customer directory is not set.");
                return false;
            }

            // check if the file name is empty
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("File name is empty.");
                return false;
            }

            // check if the byte array is empty
            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogError("Byte array is empty.");
                return false;
            }

            #endregion

            #region get the URL from the configuration

            string? url = _configuration["FileServiceUrls:FileUploadAsync"];

            // check if the url is empty
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogError("File upload URL is empty.");
                return false;
            }

            #endregion

            try
            {
                #region create the form content

                // Create MultipartFormDataContent to send the file
                var formContent = new MultipartFormDataContent();

                // Add customerId and fileName to the form
                formContent.Add(new StringContent(_customerDirectory), "customerId");
                formContent.Add(new StringContent(fileName), "fileName");

                // Create ByteArrayContent for the file bytes
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); // Set to PDF
                formContent.Add(fileContent, "file", fileName);

                #endregion

                #region send the file to the file upload URL and handle the response

                // Send the file to the file upload URL
                var response = await _httpClient.PostAsync(url, formContent);

                if (response.IsSuccessStatusCode)
                {
                    var successMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"File '{fileName}' uploaded successfully for customer '{_customerDirectory}'. Message: {successMessage}");
                    await _fileQueueService.SendMessageAsync(successMessage, _fileQueueName);
                    return true; // Upload was successful
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error from Azure Function while uploading file '{fileName}' for customer '{_customerDirectory}': {errorMessage}");
                    await _fileQueueService.SendMessageAsync($"Error uploading file '{fileName}': {errorMessage}", _fileQueueName);
                    return false; // Upload failed
                }

                #endregion

            }
            catch (RequestFailedException rfEx)
            {
                // Log the error
                _logger.LogError($"Azure Storage error while uploading file '{fileName}' for customer '{_customerDirectory}': {rfEx}");
                await _fileQueueService.SendMessageAsync($"Error uploading file '{fileName}': {rfEx.Message}", _fileQueueName);
                return false; // Return false to indicate failure
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError($"An error occurred while uploading file ({fileName}): {ex}");
                await _fileQueueService.SendMessageAsync($"Error uploading file '{fileName}': {ex.Message}", _fileQueueName);
                return false; // Return false to indicate failure
            }
        }

    }
}
