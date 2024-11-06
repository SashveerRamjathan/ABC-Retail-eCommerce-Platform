namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for invoice generation operations
    public interface IInvoiceGenerationService
    {
        // Generate an invoice for a specific user and order, returning the invoice as a byte array
        Task<byte[]> GenerateInvoiceAsync(string userId, string orderId);
    }
}
