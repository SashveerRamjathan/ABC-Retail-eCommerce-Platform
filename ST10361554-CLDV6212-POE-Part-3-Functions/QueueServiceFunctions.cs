using Azure;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ST10361554_CLDV6212_POE_Part_3_Functions
{
    public class QueueServiceFunctions
    {
        private readonly ILogger<QueueServiceFunctions> _logger;
        private readonly QueueServiceClient _serviceClient;

        public QueueServiceFunctions(ILogger<QueueServiceFunctions> logger)
        {
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (connectionString == null)
            {
                throw new ArgumentNullException("AzureWebJobsStorage environment variable is not set.");
            }

            _serviceClient = new QueueServiceClient(connectionString);

        }

        // Method to create a queue in the storage account or get an existing one
        private async Task<QueueClient?> GetQueueClient(string queueName)
        {
            #region input validation

            // Check if the service client is null
            if (_serviceClient == null)
            {
                _logger.LogError("QueueServiceClient is not initialized.");
                throw new ArgumentNullException(nameof(_serviceClient), "QueueServiceClient is not initialized.");
            }

            // Check if the queue name is null or empty
            if (string.IsNullOrWhiteSpace(queueName))
            {
                _logger.LogError("Queue name is null or empty.");
                throw new ArgumentNullException(nameof(queueName), "Queue name is null or empty.");
            }

            #endregion

            try
            {
                #region create or get queue client

                var queueClient = _serviceClient.GetQueueClient(queueName);

                // Create the queue if it does not exist
                var response = await queueClient.CreateIfNotExistsAsync();

                return queueClient;

                #endregion
            }
            catch (RequestFailedException rfEx)
            {
                // Log the specific Azure Storage error
                _logger.LogError(rfEx, $"Azure Storage error while creating or getting queue {queueName}");
                return null;
            }
            catch (Exception ex)
            {
                // Log any generic errors
                _logger.LogError(ex, $"Unexpected error while creating or getting queue {queueName}");
                return null;
            }
        }

        [Function(nameof(SendMessageAsync))]
        public async Task<IActionResult> SendMessageAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                #region input validation

                // read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // parse the request body
                dynamic? data = JsonConvert.DeserializeObject(requestBody);

                // check if the data is null
                if (data == null)
                {
                    _logger.LogError("Request body is null.");
                    return new BadRequestObjectResult("Request body is null.");
                }

                // get the queue name from the request body
                string? queueName = data?.queueName;

                // check if the queue name is null or empty
                if (string.IsNullOrWhiteSpace(queueName))
                {
                    _logger.LogError("Queue name is null or empty.");
                    return new BadRequestObjectResult("Queue name is null or empty.");
                }

                // get the message from the request body
                string? message = data?.message;

                // check if the message is null or empty
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogError("Message is null or empty.");
                    return new BadRequestObjectResult("Message is null or empty.");
                }

                #endregion

                #region get or create queue client

                // get the queue client
                var queueClient = await GetQueueClient(queueName);

                // check if the queue client is null
                if (queueClient == null)
                {
                    _logger.LogError($"Error creating or getting queue {queueName}.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region send message to queue and return response

                // send the message to the queue
                var response = await queueClient.SendMessageAsync(message);

                // Check if the response indicates an error
                if (response.GetRawResponse().IsError)
                {
                    _logger.LogError($"Error sending message to queue {queueName}. Response status: {response.GetRawResponse().Status}");
                    return new BadRequestObjectResult("Error sending message to queue.");
                }

                _logger.LogInformation($"Message successfully sent to queue {queueName}.");

                return new OkObjectResult(response.Value);

                #endregion
            }
            catch (RequestFailedException rfEx)
            {
                // Log Azure-specific errors
                _logger.LogError(rfEx.Message, $"Azure Queue Storage error when sending message to queue.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                // Log any other unexpected errors
                _logger.LogError(ex.Message, $"Unexpected error when sending message to queue.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
