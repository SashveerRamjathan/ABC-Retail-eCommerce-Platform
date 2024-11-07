using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ILogger<ShoppingCartService> _logger;
        private ImmutableList<Product> products = ImmutableList<Product>.Empty;
        private readonly IProductDatabaseService _productTableStorageService;
        private readonly QueueService _shoppingCartQueueService;
        private readonly string _queueName = "shopping-cart-queue";

        // Constructor to initialize the logger, product table storage service, and queue service
        public ShoppingCartService(ILogger<ShoppingCartService> logger,
            IProductDatabaseService productTableStorageService,
            IQueueServiceFactory queueServiceFactory)
        {
            _logger = logger;
            _productTableStorageService = productTableStorageService;
            _shoppingCartQueueService = queueServiceFactory.Create(_queueName);
        }

        public async Task<bool> AddToCart(string productId, int quantity)
        {
            try
            {
                var entity = await _productTableStorageService.GetProductAsync(productId);

                if (entity != null)
                {
                    if (products.Any(p => p.Id == productId))
                    {
                        var existingProduct = products.FirstOrDefault(p => p.Id == productId);

                        if (existingProduct != null)
                        {
                            // Update the quantity of the existing product
                            existingProduct.Quantity += quantity;

                            var editedProducts = products.Remove(existingProduct);
                            products = editedProducts.Add(existingProduct);
                        }

                        _logger.LogWarning($"Product {entity.Name} is already in the cart, updated the quantity by {quantity}");
                        await _shoppingCartQueueService.SendMessageAsync($"Product {entity.Name} is already in the cart, updated the quantity by {quantity}", _queueName);

                        return true;
                    }

                    // Add new product to the cart
                    entity.Quantity = quantity;

                    var updatedProducts = products.Add(entity);
                    products = updatedProducts;  // Reassign after operation.
                    _logger.LogInformation($"Product {entity.Name}, quantity {quantity} added to cart");
                    await _shoppingCartQueueService.SendMessageAsync($"Product {entity.Name}, quantity {quantity} added to cart", _queueName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding product to cart: {ex.Message}");
                await _shoppingCartQueueService.SendMessageAsync($"Error adding product to cart: {ex.Message}", _queueName);
                return false;
            }
        }

        public async Task<bool> EditQuantity(string productId, int quantity)
        {
            try
            {
                var productToEdit = products.FirstOrDefault(p => p.Id == productId);

                if (productToEdit == null)
                {
                    _logger.LogInformation($"Product with ID: {productId} not found in cart.");
                    await _shoppingCartQueueService.SendMessageAsync($"Product with ID: {productId} not found in cart.", _queueName);
                    return false;
                }

                if (quantity == 0)
                {
                    return await RemoveFromCart(productId); // This will log the removal.
                }

                // Update the quantity of the product
                productToEdit.Quantity = quantity;

                Product updatedProduct = productToEdit;

                // Update the product list atomically
                var list = products.Remove(productToEdit);

                // Check if the removal resulted in a different list instance (i.e., the product was actually removed).
                if (list != products)
                {
                    products = list.Add(updatedProduct); // Update the reference to the new list.
                    _logger.LogInformation($"Product {updatedProduct.Name} quantity updated to {quantity}");
                    await _shoppingCartQueueService.SendMessageAsync($"Product {updatedProduct.Name} quantity updated to {quantity}", _queueName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error editing product quantity: {ex.Message}");
                await _shoppingCartQueueService.SendMessageAsync($"Error editing product quantity: {ex.Message}", _queueName);
                return false;
            }
        }

        public async Task<bool> ClearCart()
        {
            products = ImmutableList<Product>.Empty;

            _logger.LogInformation("Cart cleared");
            await _shoppingCartQueueService.SendMessageAsync("Cart cleared", _queueName);

            return true;
        }

        public ImmutableList<Product> GetCartItems()
        {
            return products;
        }

        public async Task<bool> RemoveFromCart(string productId)
        {
            try
            {
                var productToRemove = products.FirstOrDefault(p => p.Id == productId);

                if (productToRemove != null)
                {
                    var updatedProducts = products.Remove(productToRemove);

                    // Check if the removal resulted in a different list instance (i.e., the product was actually removed).
                    if (updatedProducts != products)
                    {
                        products = updatedProducts; // Update the reference to the new list.
                        _logger.LogInformation($"Product {productToRemove.Name} removed from cart");
                        await _shoppingCartQueueService.SendMessageAsync($"Product {productToRemove.Name} removed from cart", _queueName);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing product from cart: {ex.Message}");
                await _shoppingCartQueueService.SendMessageAsync($"Error removing product from cart: {ex.Message}", _queueName);
            }

            return false; // Return false if the product was not found or removed.
        }
    }
}
