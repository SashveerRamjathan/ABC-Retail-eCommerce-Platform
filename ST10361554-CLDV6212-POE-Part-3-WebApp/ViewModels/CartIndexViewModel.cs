namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class CartIndexViewModel
    {
        // List of items in the shopping cart
        public List<CartItemViewModel>? CartItems { get; set; }

        // Total price of all items in the cart
        public decimal GrandTotal { get; set; }
    }
}
