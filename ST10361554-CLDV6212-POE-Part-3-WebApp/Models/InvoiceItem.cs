namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    // Class representing an item in an invoice
    public class InvoiceItem
    {
        public string ProductName { get; set; } // Name of the product
        public int Quantity { get; set; } // Quantity of the product
        public decimal Price { get; set; } // Price per unit of the product
    }
}