using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Collections.Immutable;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    public interface IShoppingCartService
    {
        // Interface for shopping cart-related operations
        public interface IShoppingCartService
        {
            // Add a product to the cart with specified quantity
            Task<bool> AddToCart(string partitionKey, string rowKey, int quantity);

            // Edit the quantity of a product in the cart
            Task<bool> EditQuantity(string partitionKey, string rowKey, int quantity);

            // Clear all items from the cart
            Task<bool> ClearCart();

            // Get all items currently in the cart as an immutable list
            ImmutableList<Product> GetCartItems();

            // Remove a specific product from the cart
            Task<bool> RemoveFromCart(string partitionKey, string rowKey);
        }
    }
}
