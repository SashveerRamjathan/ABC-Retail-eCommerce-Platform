using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_Functions.Data;
using ST10361554_CLDV6212_POE_Part_3_Functions.Models;
using System.Reflection;

namespace ST10361554_CLDV6212_POE_Part_3_Functions
{
    public class ProductDatabaseFunctions
    {
        private readonly ILogger<ProductDatabaseFunctions> _logger;
        private readonly ApplicationDbContext _context;

        public ProductDatabaseFunctions(ILogger<ProductDatabaseFunctions> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _context = dbContext;
        }

        // Method to check for null fields in any object
        private bool CheckForNullFields(object obj)
        {
            #region Check for null fields in any object

            try
            {
                // Get all public instance properties of the object
                var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // Check if any property value is null
                return properties.Any(prop => prop.GetValue(obj) == null);
            }
            catch (Exception)
            {
                // Rethrow the exception to be handled by the calling code
                throw;
            }

            #endregion
        }

        [Function(nameof(AddProductAsync))]
        public async Task<IActionResult> AddProductAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Adding product to database");

            try
            {
                #region Deserialize request body and validate input

                // Get the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Object to hold the product details
                Product? product;

                // Deserialize the request body
                try
                {
                    product = JsonConvert.DeserializeObject<Product>(requestBody);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Error deserializing request body: {jsonEx.Message}");
                    return new BadRequestObjectResult("Invalid request body. Please provide a valid JSON object.");
                }

                // Check if the product object is null
                if (product == null)
                {
                    _logger.LogWarning("The product object is null.");
                    return new BadRequestObjectResult("The product object is null.");
                }

                // Check for null fields in the product object
                if (CheckForNullFields(product))
                {
                    _logger.LogWarning("One or more required fields in the product entity are null.");
                    return new BadRequestObjectResult("One or more required fields are missing or null.");
                }

                #endregion

                #region Add product entity to the database

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                // add the product to the database
                await _context.Products.AddAsync(product);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                // Check if the product was created successfully
                if (response == 0)
                {
                    _logger.LogWarning($"Product creation failed.");
                    return new BadRequestObjectResult($"Product creation failed.");
                }

                _logger.LogInformation($"Product {product.Name} created successfully.");
                return new OkObjectResult($"Product ({product.Name}) created successfully.");

                #endregion
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Product.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding product to the database: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }

        [Function(nameof(DeleteProductAsync))]
        public async Task<IActionResult> DeleteProductAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequest req)
        {
            try
            {
                #region retrieve and validate the id

                // Get the id from the query string
                string? id = req.Query["id"];

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Id is null or empty.");
                    return new BadRequestObjectResult("Id is null or empty.");
                }

                _logger.LogInformation($"Deleting product with id: {id}");

                #endregion

                #region Check if database context is initialized

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region Check if the product exists before attempting to delete

                var product = _context.Products.Find(id);

                if (product == null)
                {
                    _logger.LogWarning($"Product with id: {id} not found.");
                    return new NotFoundObjectResult($"Product with id: {id} not found.");
                }

                #endregion

                #region Delete entity from the database

                // remove the product from the database
                _context.Products.Remove(product);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                if (response == 0)
                {
                    _logger.LogWarning($"Product deletion failed.");
                    return new BadRequestObjectResult($"Product deletion failed.");
                }

                _logger.LogInformation($"Product with id: {id} deleted successfully.");
                return new OkObjectResult($"Product with id: {id} deleted successfully.");

                #endregion

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Product.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting product: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetAllProducts))]
        public IActionResult GetAllProducts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Getting all products from the database");

            try
            {
                #region Check if database context is initialized

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region Get all products from the database

                // Get all products from the database
                var entities = _context.Products.ToList();

                if (entities.Count == 0)
                {
                    _logger.LogInformation("No products found in the table.");
                    return new NoContentResult();
                }

                _logger.LogInformation("Products retrieved successfully.");
                return new OkObjectResult(entities);

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting products from the database: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetProductAsync))]
        public async Task<IActionResult> GetProductAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                #region retrieve and validate the id

                // Get the id from the query string
                string? id = req.Query["id"];

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Id is null or empty.");
                    return new BadRequestObjectResult("Id is null or empty.");
                }

                _logger.LogInformation($"Getting product with id: {id}");

                #endregion

                #region Check if database context is initialized

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region Get product from the database

                Product? product;

                // Get the product from the database
                product = await _context.Products.FindAsync(id);

                // Check if the product was found
                if (product == null)
                {
                    _logger.LogWarning($"Product with id: {id} not found.");
                    return new NotFoundObjectResult($"Product with id: {id} not found.");
                }

                _logger.LogInformation($"Product with id: {id} retrieved successfully.");
                return new OkObjectResult(product);

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(UpdateProductAsync))]
        public async Task<IActionResult> UpdateProductAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
        {
            _logger.LogInformation("Updating product in the database");

            try
            {
                #region Deserialize request body and validate input

                // Get the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Object to hold the product details
                Product? product;

                // Deserialize the request body
                try
                {
                    product = JsonConvert.DeserializeObject<Product>(requestBody);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Error deserializing request body: {jsonEx.Message}");
                    return new BadRequestObjectResult("Invalid request body. Please provide a valid JSON object.");
                }

                // Check if the product object is null
                if (product == null)
                {
                    _logger.LogWarning("The product object is null.");
                    return new BadRequestObjectResult("The product object is null.");
                }

                // Check for null fields in the product object
                if (CheckForNullFields(product))
                {
                    _logger.LogWarning("One or more required fields in the product are null.");
                    return new BadRequestObjectResult("One or more required fields are missing or null.");
                }

                #endregion

                #region Check if database context is initialized

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region Update row in the database

                // Update the product in the database
                _context.Products.Update(product);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                if (response == 0)
                {
                    _logger.LogWarning($"Product update failed.");
                    return new BadRequestObjectResult($"Product update failed.");

                }

                _logger.LogInformation($"Product with id: {product.Id} updated successfully.");
                return new OkObjectResult($"Product with id: {product.Id} updated successfully.");

                #endregion
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for Product.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating product in the database: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
