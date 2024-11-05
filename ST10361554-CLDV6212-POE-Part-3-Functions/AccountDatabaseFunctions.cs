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
    public class AccountDatabaseFunctions
    {
        private readonly ILogger<AccountDatabaseFunctions> _logger;
        private readonly ApplicationDbContext _context;

        public AccountDatabaseFunctions(ILogger<AccountDatabaseFunctions> logger, ApplicationDbContext dbContext)
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

        [Function(nameof(CreateUserAsync))]
        public async Task<IActionResult> CreateUserAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Creation of user started");

            try
            {
                #region Deserialize request body and validate input

                // Get the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialize the request body into a User object
                User? user;

                try
                {
                    user = JsonConvert.DeserializeObject<User>(requestBody);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Error deserializing request body: {jsonEx.Message}");
                    return new BadRequestObjectResult("Invalid request body. Please provide a valid JSON object.");
                }

                // Check if the user object is null
                if (user == null)
                {
                    _logger.LogWarning("The user object is null.");
                    return new BadRequestObjectResult("The user object is null.");
                }

                // Check for null fields in the user object
                if (CheckForNullFields(user))
                {
                    _logger.LogWarning("One or more required fields in the user entity are null.");
                    return new BadRequestObjectResult("One or more required fields are missing or null.");
                }

                #endregion

                #region Add user to the database

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                // add the user to the database
                await _context.Users.AddAsync(user);

                // Save the changes to the database
                var response = await _context.SaveChangesAsync();

                // Check if the user was created successfully
                if (response == 0)
                {
                    _logger.LogWarning($"User creation failed with status code.");
                    return new BadRequestObjectResult($"User creation failed.");
                }

                _logger.LogInformation($"User with ID {user.Id} created successfully.");
                return new OkObjectResult($"User ({user.Id}) created successfully.");

                #endregion
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, $"Database update failed for User.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding user entity to the table: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetUserByEmail))]
        public IActionResult GetUserByEmail([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Get user by email function started");

            try
            {
                #region Retrieve and Validate Email from Query String

                // Get the email from the query string
                string? email = req.Query["email"];

                // Check if the email is null or empty
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email query parameter is null or empty.");
                    return new BadRequestObjectResult("The email query parameter is null or empty");
                }

                #endregion

                #region Check Database Context Initialization

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                #endregion

                #region Query User by Email

                // Get the user entity from the table
                var query = _context.Users.Where(u => u.Username == email).ToList();

                // Check if the user was found
                if (query.Count == 0)
                {
                    _logger.LogWarning($"User with email {email} not found.");
                    return new NotFoundObjectResult($"User with email {email} not found");
                }

                User? userEntity = null;

                // Get the first user from the query
                foreach (User user in query)
                {
                    userEntity = user;

                    break; // Exit after the first match
                }

                // log that the user was found
                _logger.LogInformation($"User {email} was found successfully");

                #endregion

                #region Return the User Entity

                // return the user
                return new OkObjectResult(userEntity);

                #endregion
            }
            catch (Exception ex)
            {
                // Log the error and return a bad request object result
                _logger.LogError($"Error getting user by email: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetUserById))]
        public IActionResult GetUserById([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Get user by Id function started");

            try
            {
                #region Retrieve and validate user ID from query string

                // Get the email from the query string
                string? id = req.Query["id"];

                // Check if the email is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("The user ID is null or empty.");
                    return new BadRequestObjectResult("The user ID is required and cannot be null or empty.");
                }

                #endregion

                #region Check database context and fetch user entity

                /// check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                // Query the table for the user  based on the id (user ID)
                var query = _context.Users.Where(u => u.Id == id).ToList();

                // check if the user was found
                if (query.Count == 0)
                {
                    _logger.LogWarning($"User with id {id} not found.");
                    return new NotFoundObjectResult($"User with id {id} not found");
                }

                User? userEntity = null;

                // Retrieve the first matching user entity
                foreach (User user in query)
                {
                    userEntity = user;

                    break; // Exit after the first match
                }

                // log that the user was found
                _logger.LogInformation($"User {id} was found successfully");

                #endregion

                #region Return the user entity

                // return the user
                return new OkObjectResult(userEntity);

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception and return a generic internal server error
                _logger.LogError($"An error occurred while retrieving the user by id: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function(nameof(GetUserByEmailAndPasswordAsync))]
        public async Task<IActionResult> GetUserByEmailAndPasswordAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Get user by email and password function started");

            try
            {
                #region Deserialize request body and validate input


                // Get the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialize the request body and extract the email and password
                var body = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);

                // Check if body is null
                if (body == null || !body.TryGetValue("email", out var email) || !body.TryGetValue("password", out var password))
                {
                    _logger.LogWarning("Email or password not found in the request body.");
                    return new BadRequestObjectResult("Email or password not found in the request body.");
                }

                // Trim input
                email = email.Trim();
                password = password.Trim();

                #endregion

                #region Check database context and fetch user entity

                // check if dbContext is null
                if (_context == null)
                {
                    _logger.LogError("Database Context is not initialized.");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                // Query the table for the user  based on the email and password
                var query = _context.Users.Where(u => u.Username == email && u.PasswordHash == password).ToList();

                // check if the user was found
                if (query.Count == 0)
                {
                    _logger.LogWarning($"User with email {email} not found.");
                    return new NotFoundObjectResult($"User with email {email} not found");
                }

                User? userEntity = null;

                // Retrieve the first matching user
                foreach (User user in query)
                {
                    userEntity = user;

                    break; // Exit after the first match
                }

                // Check if the user was found
                if (userEntity == null)
                {
                    _logger.LogWarning($"User with email {email} not found.");
                    return new NotFoundObjectResult($"User with email {email} not found.");
                }

                // log that the user was found
                _logger.LogInformation($"User {email} was found successfully");

                #endregion

                #region Return the user entity

                // return the user
                return new OkObjectResult(userEntity);

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception and return a generic internal server error
                _logger.LogError($"An error occurred while retrieving the user by id: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
