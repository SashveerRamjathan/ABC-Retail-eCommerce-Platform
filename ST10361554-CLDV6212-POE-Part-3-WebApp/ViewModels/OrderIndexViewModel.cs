namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class OrderIndexViewModel
    {
        // Unique identifier for the order
        public required string OrderID { get; set; }

        // Date when the order was placed
        public DateTime OrderDate { get; set; }

        // Current status of the order (e.g., Pending, Shipped, Delivered)
        public required string OrderStatus { get; set; }

        // Total amount for the order
        public decimal GrandTotal { get; set; }
    }
}
