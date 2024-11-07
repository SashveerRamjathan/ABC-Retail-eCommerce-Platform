namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class AdminOrderItemViewModel
    {
        // Name of the product
        public required string ProductName { get; set; }

        // Quantity of the product ordered
        public int Quantity { get; set; }

        // Total price for this item (Quantity * Unit Price)
        public decimal TotalItemPrice { get; set; }

        // Current status of the item (e.g., In Stock, Out of Stock, Shipped)
        public required string ItemStatus { get; set; }

        // Base64 encoded image of the product
        public string Base64ProductImage { get; set; }

        // Unique identifier for the product
        public string ProductId { get; set; }
    }
}
