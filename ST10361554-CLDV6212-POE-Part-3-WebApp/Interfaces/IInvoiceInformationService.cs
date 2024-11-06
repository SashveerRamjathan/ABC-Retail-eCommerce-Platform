using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for retrieving invoice information
    public interface IInvoiceInformationService
    {
        // Get the invoice information for a specific user and order
        Task<InvoiceModel?> GetInvoiceInfoAsync(string userId, string orderId);
    }
}
