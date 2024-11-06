using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for product-related operations in table storage
    public interface IProductDatabaseService
    {
        // Retrieve all products from table storage
        Task<List<Product>> GetAllProductsAsync();

        // Retrieve a specific product by partition key and row key
        Task<Product?> GetProductAsync(string productId);

        // Add a new product to table storage
        Task<bool> AddProductAsync(Product product);

        // Update an existing product in table storage
        Task<bool> UpdateProductAsync(Product product);

        // Delete a product from table storage by partition key and row key
        Task<bool> DeleteProductAsync(string productId);
    }
}
