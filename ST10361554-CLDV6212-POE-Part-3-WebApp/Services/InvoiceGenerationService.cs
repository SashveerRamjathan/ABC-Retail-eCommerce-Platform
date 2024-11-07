using QuestPDF.Fluent;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models.QuestPDF;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Service for generating invoices in PDF format
    public class InvoiceGenerationService : IInvoiceGenerationService
    {
        private readonly IInvoiceInformationService _invoiceInformationService;
        private readonly QueueService _fileQueueService;
        private readonly string _fileQueueName = "file-queue";
        private readonly ILogger<InvoiceGenerationService> _logger;

        // Constructor to initialize the service with dependencies
        public InvoiceGenerationService(IInvoiceInformationService invoiceInformationService,
            IQueueServiceFactory queueServiceFactory,
            ILogger<InvoiceGenerationService> logger)
        {
            _invoiceInformationService = invoiceInformationService;
            _fileQueueService = queueServiceFactory.Create(_fileQueueName);
            _logger = logger;
        }

        public async Task<byte[]> GenerateInvoiceAsync(string userId, string orderId)
        {
            try
            {
                // Retrieve invoice details from the invoice information service
                var invoiceDetails = await _invoiceInformationService.GetInvoiceInfoAsync(userId, orderId);

                if (invoiceDetails != null)
                {
                    _logger.LogInformation($"Generating Invoice for user {userId}, order {orderId}");
                    await _fileQueueService.SendMessageAsync($"Generating Invoice for user {userId}, order {orderId}", _fileQueueName);

                    // Create the InvoiceDocument using QuestPDF
                    var invoiceDocument = new InvoiceDocument(invoiceDetails);

                    // Generate the PDF into a memory stream
                    using var memoryStream = new MemoryStream();
                    invoiceDocument.GeneratePdf(memoryStream);

                    _logger.LogInformation($"Invoice generated for user {userId}, order {orderId}");
                    await _fileQueueService.SendMessageAsync($"Invoice generated for user {userId}, order {orderId}", _fileQueueName);

                    // Convert the memory stream to a byte array
                    var bytes = memoryStream.ToArray();

                    return bytes;
                }

                return null!;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error generating invoice for user {userId}, order {orderId}: {ex.Message}");
                await _fileQueueService.SendMessageAsync($"Error generating invoice for user {userId}, order {orderId}", _fileQueueName);
                return null!;
            }
        }
    }
}
