using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class CartItemViewModel
    {
        // The product associated with this cart item
        public required Product Product { get; set; }

        // Quantity of the product in the cart
        [Required(ErrorMessage = "Please enter a quantity")]
        public int Quantity { get; set; }

        // Total price for this cart item (Quantity * Unit Price)
        public decimal TotalPrice { get; set; }
    }
}
