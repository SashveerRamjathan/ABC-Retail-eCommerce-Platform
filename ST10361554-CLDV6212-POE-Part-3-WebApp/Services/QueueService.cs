using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using System.Text;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class QueueService : IQueueService
    {
        private readonly ILogger<QueueService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // Constructor to initialize the connection string and logger
        public QueueService(
            ILogger<QueueService> logger,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = clientFactory.CreateClient();
            _configuration = configuration;
        }

        /*
           * Code Attribution:
           * All the methods below use the http client to make HTTP requests to Azure Functions.
           * IEvangelist
           * 11 February 2023
           * Make HTTP requests with the HttpClient class
           * https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
        */

        // Method to send a message to the specified queue
        public async Task<SendReceipt> SendMessageAsync(string message, string queueName)
        {
            #region validate the inputs

            // validate the input
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError("Message is empty.");
                return null!;
            }

            if (string.IsNullOrWhiteSpace(queueName))
            {
                _logger.LogError("Queue name is empty.");
                return null!;
            }

            #endregion

            #region get the request URL from the configuration

            // get the request URL from the configuration
            var requestUrl = _configuration["QueueServiceUrl:SendMessageAsync"];

            // validate the request URL
            if (string.IsNullOrWhiteSpace(requestUrl))
            {
                _logger.LogError("Request URL is empty.");
                return null!;
            }

            #endregion

            try
            {
                #region create the request body as a JSON object and send the request

                // Create the request body as a JSON object
                var requestBody = new
                {
                    queueName = queueName,
                    message = message
                };

                // Serialize the request body to JSON
                var json = JsonConvert.SerializeObject(requestBody);

                // Create the request content
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send the request to the queue service
                var response = await _httpClient.PostAsync(requestUrl, content);

                #endregion

                #region check the response and deserialize the response body

                // Check if the response indicates success
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response body if needed
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // check if the response body is empty
                    if (string.IsNullOrWhiteSpace(responseBody))
                    {
                        _logger.LogError("Response body is empty.");
                        return null!;
                    }

                    var sendReceipt = JsonConvert.DeserializeObject<SendReceipt>(responseBody);

                    if (sendReceipt == null)
                    {
                        _logger.LogError("Failed to deserialize the response body.");
                        return null!;
                    }

                    return sendReceipt!;
                }
                else
                {
                    _logger.LogError($"Failed to send message to queue. Status Code: {response.StatusCode}");
                    return null!;
                }

                #endregion
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex.Message, $"Error sending message to queue {queueName}");
                return null!;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex.Message, $"Error sending message to queue {queueName}");

                return null!;
            }
        }
    }
}
