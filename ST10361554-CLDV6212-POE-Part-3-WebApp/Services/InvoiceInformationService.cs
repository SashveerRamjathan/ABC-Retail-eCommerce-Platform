using QuestPDF.Infrastructure;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Service for retrieving invoice information
    public class InvoiceInformationService : IInvoiceInformationService
    {
        private readonly ILogger<InvoiceInformationService> _logger;
        private readonly IProductDatabaseService _productTableStorageService;
        private readonly IOrderDatabaseService _orderTableStorageService;
        private readonly IAccountDatabaseService _accountTableStorageService;
        private readonly string queueName = "order-queue";
        private readonly QueueService _queueService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor to initialize the service with dependencies
        public InvoiceInformationService(ILogger<InvoiceInformationService> logger,
            IProductDatabaseService productTableStorageService,
            IOrderDatabaseService orderTableStorageService,
            IAccountDatabaseService accountTableStorageService,
            IQueueServiceFactory queueServiceFactory,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _productTableStorageService = productTableStorageService;
            _orderTableStorageService = orderTableStorageService;
            _accountTableStorageService = accountTableStorageService;
            _queueService = queueServiceFactory.Create(queueName);
            _webHostEnvironment = webHostEnvironment;
        }

        // Method to get invoice information for a given user and order
        public async Task<InvoiceModel?> GetInvoiceInfoAsync(string userId, string orderId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(orderId))
                {
                    _logger.LogError("User Id or Order Id is null or empty");
                    return null!;
                }

                // Retrieve user account information
                var user = await _accountTableStorageService.GetUserByIdAsync(userId);

                // Check if user is null
                if (user == null)
                {
                    _logger.LogError($"User {userId} not found");
                    return null!;
                }

                await _queueService.SendMessageAsync($"User {user.Email} requested invoice for order {orderId} at {DateTime.Now.ToString()}", queueName);

                // Create the user's address details
                Address CustomerAddress = new Address
                {
                    Name = user.Name!,
                    Street = user.StreetAddress!,
                    City = user.City!,
                    Province = user.Province!,
                    PostalCode = user.PostalCode!,
                    Country = user.Country!,
                    PhoneNumber = user.PhoneNumber!,
                    Email = user.Email
                };

                // Create ABC retail address details
                Address RetailerAddress = new Address
                {
                    Name = "ABC Retail",
                    Street = "64 Main Street",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    PostalCode = "2060",
                    Country = "South Africa",
                    PhoneNumber = "011 682 7901",
                    Email = "abc@retail.co.za"
                };

                // Create an invoice number
                Random random = new Random();
                int invoiceNumber = random.Next(100000, 999999);

                // Get the issue date
                DateTime issueDate = DateTime.Now;

                // Get the invoice comments
                string comments = "Thank you for shopping with us! " +
                    $"\nYour Order ({orderId}) is being processed. " +
                    "\n\n For any inquiries, please contact our support team on 011 682 7901 or drop us a mail at abc@support.co.za";

                // Retrieve order details
                var orderDetails = await _orderTableStorageService.GetOrderDetailsAsync(userId, orderId);

                // Check if order items are null
                if (orderDetails == null)
                {
                    _logger.LogError($"No order items found for user {userId}");
                    return null!;
                }

                var allProducts = await _productTableStorageService.GetAllProductsAsync();

                // Check if all products are null
                if (allProducts == null)
                {
                    _logger.LogError("No products found");
                    return null!;
                }

                // Get the products in the order
                var productsInOrder = allProducts.Where(p => orderDetails.Any(o => o.ProductId == p.Id)).ToList();

                // Create a list of InvoiceItems
                List<InvoiceItem> invoiceItems = new List<InvoiceItem>();

                // Get a list of the product details
                foreach (var item in orderDetails)
                {
                    var product = productsInOrder.FirstOrDefault(p => p.Id == item.ProductId);

                    // Check if product is not null
                    if (product != null)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            ProductName = product.Name,
                            Quantity = item.Quantity,
                            Price = product.Price
                        };

                        invoiceItems.Add(invoiceItem);
                    }
                }

                // Get the image as a byte array
                var image = GetImageAsByteArray();

                // Check if image array is null
                if (image == null)
                {
                    _logger.LogError("Image array is null");
                    return null!;
                }

                // Prepare the invoice model
                var model = new InvoiceModel
                {
                    InvoiceNumber = invoiceNumber,
                    IssueDate = issueDate,
                    ShippingAddress = CustomerAddress,
                    ABCRetailAddress = RetailerAddress,
                    Items = invoiceItems,
                    Comments = comments,
                    LogoImage = image
                };

                return model;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error getting invoice information: {ex.Message}");

                return null!;
            }
        }

        // Method to get the image as a byte array
        private Image GetImageAsByteArray()
        {
            try
            {
                string relativePath = "Images/ABC Retail Logo.jpg";

                var stream = _webHostEnvironment.WebRootFileProvider.GetFileInfo(relativePath).CreateReadStream();

                if (stream == null)
                {
                    throw new FileNotFoundException("The file was not found.", relativePath);
                }

                Image image = Image.FromStream(stream);

                return image;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting file: {ex.Message}");

                return null!;
            }
        }
    }
}
