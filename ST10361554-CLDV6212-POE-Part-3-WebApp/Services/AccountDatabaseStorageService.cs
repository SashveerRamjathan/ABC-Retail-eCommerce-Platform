using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Service for managing user accounts using a HTTP client to interact with a database function
    public class AccountDatabaseStorageService : IAccountDatabaseService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountDatabaseStorageService> _logger;
        private readonly HttpClient _httpClient;

        // Constructor to initialize the table client and HTTP context accessor
        public AccountDatabaseStorageService(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<AccountDatabaseStorageService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /*
          * Code Attribution:
          * All the methods below use the http client to make HTTP requests to Azure Functions.
          * IEvangelist
          * 11 February 2023
          * Make HTTP requests with the HttpClient class
          * https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
        */

        // Function Call Method
        // Create a new user in the users table based on the user object
        public async Task CreateUserAsync(User user)
        {
            try
            {
                #region serialize user object and make an HTTP POST request

                var jsonObject = JsonConvert.SerializeObject(user);

                var content = new StringContent(jsonObject, Encoding.UTF8, "application/json");

                var url = _configuration["AccountDatabaseStorageUrls:CreateUserAsync"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("The CreateUserAsync URL is not configured.");
                    throw new InvalidOperationException("The URL for CreateUserAsync is not configured.");
                }

                // make an HTTP POST request to the function
                var response = await _httpClient.PostAsync(url, content);

                #endregion

                #region handle the response based on the status code

                // Handle the response based on the status code
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // http response code in the range 200 - 299
                    _logger.LogInformation($"User created successfully: {responseBody}");
                    return;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // http response code 400
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    _logger.LogError($"Error creating user: {errorMessage}");
                    throw new HttpRequestException(errorMessage);
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    // http response code 500 Internal Server Error
                    _logger.LogError("Internal Server Error occurred while creating the user.");

                    throw new HttpRequestException("Internal Server Error (500) occurred while creating the user.");
                }
                else
                {
                    // Handle other unexpected statuses
                    _logger.LogError($"Unexpected status code received: {response.StatusCode}");
                    throw new HttpRequestException($"Unexpected status code received: {response.StatusCode}");
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                throw;
            }
        }

        // Function Call Method
        // Get a user based on the email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                #region Input validation

                // check if the email is null or empty
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is null or empty.");
                    return null;
                }

                #endregion

                #region make a GET request 

                // Get the URL from configuration
                var url = _configuration["AccountDatabaseStorageUrls:GetUserByEmailAsync"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("The GetUserByEmailAsync URL is not configured.");
                    throw new InvalidOperationException("The URL for GetUserByEmailAsync is not configured.");
                }

                // Append email query parameter to the URL
                var fullUrl = $"{url}?email={email}";

                // Make the GET request to the Azure Function
                var response = await _httpClient.GetAsync(fullUrl);

                #endregion

                #region handle the response based on the status code

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response body to UserEntity
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<User>(responseBody);

                    // Check if deserialization was successful
                    if (user == null)
                    {
                        _logger.LogWarning("User entity could not be deserialized from the response.");
                        throw new JsonSerializationException("Error deserializing the user entity.");
                    }

                    _logger.LogInformation($"User retrieved successfully: {user?.Username}");
                    return user;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"User with email {email} not found.");

                    // User not found
                    return null;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting user by email: {errorMessage}");
                    throw new HttpRequestException(errorMessage);
                }

                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while retrieving user by email: {ex.Message}");
                throw;
            }
        }

        // Function Call Method
        // Get a user based on a user ID
        public async Task<User?> GetUserByIdAsync(string id)
        {
            try
            {
                #region Input validation

                // check if the id is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("ID is null or empty.");
                    return null;
                }

                #endregion

                #region make a GET request 

                // Get the URL from configuration
                var url = _configuration["AccountDatabaseStorageUrls:GetUserByIdAsync"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("The GetUserByIdAsync URL is not configured.");
                    throw new InvalidOperationException("The URL for GetUserByIdAsync is not configured.");
                }

                // Append id query parameter to the URL
                var fullUrl = $"{url}?id={id}";

                // Make the GET request to the Azure Function
                var response = await _httpClient.GetAsync(fullUrl);

                #endregion

                #region handle the response based on the status code

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response body to UserEntity
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<User>(responseBody);

                    // Check if deserialization was successful
                    if (user == null)
                    {
                        _logger.LogWarning("User entity could not be deserialized from the response.");
                        throw new JsonSerializationException("Error deserializing the user entity.");
                    }

                    _logger.LogInformation($"User retrieved successfully: {user?.Username}");
                    return user;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"User with id {id} not found.");

                    // User not found
                    return null;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting user by id: {errorMessage}");
                    throw new HttpRequestException(errorMessage);
                }

                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while retrieving user by email: {ex.Message}");
                throw;
            }
        }

        // Function Call Method
        // Get a user by email and password
        private async Task<User?> GetUserByEmailAndPasswordAsync(string email, string passwordHash)
        {
            try
            {
                #region input validation

                // Check if the email is null or empty
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is null or empty.");
                    return null;
                }

                // Check if the password hash is null or empty
                if (string.IsNullOrEmpty(passwordHash))
                {
                    _logger.LogWarning("Password hash is null or empty.");
                    return null;
                }

                #endregion

                #region Make POST request

                // Get the URL from configuration
                var url = _configuration["AccountDatabaseStorageUrls:GetUserByEmailAndPasswordAsync"];

                // Ensure that the URL is not null
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("The GetUserByEmailAndPasswordAsync URL is not configured.");
                    throw new InvalidOperationException("The URL for GetUserByEmailAndPasswordAsync is not configured.");
                }

                string password = passwordHash;

                // Create the request body
                var requestBody = JsonConvert.SerializeObject(new { email, password });

                // Create the StringContent for the POST request
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Make the POST request to the Azure Function
                var response = await _httpClient.PostAsync(url, content);

                #endregion

                #region Handle the response based on the status code

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response to a UserEntity object
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<User>(responseBody);

                    // Check if deserialization was successful
                    if (user == null)
                    {
                        _logger.LogWarning("User entity could not be deserialized from the response.");
                        throw new JsonSerializationException("Error deserializing the user entity.");
                    }

                    _logger.LogInformation($"User retrieved successfully: {user?.Username}");
                    return user;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"User with email {email} not found.");
                    return null;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting user by email and password: {errorMessage}");
                    throw new HttpRequestException(errorMessage);
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user by email and password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RegisterUserAsync(string password, string email, string role, string name, string streetAddress, 
            string city, string province, string postalCode, string country, string phoneNumber)
        {
            try
            {
                #region Input validation

                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role) ||
                    string.IsNullOrEmpty(name) || string.IsNullOrEmpty(streetAddress) || string.IsNullOrEmpty(city) ||
                    string.IsNullOrEmpty(province) || string.IsNullOrEmpty(postalCode) || string.IsNullOrEmpty(country) ||
                    string.IsNullOrEmpty(phoneNumber))
                {
                    return false;
                }

                #endregion

                #region check if the user already exists

                // get the user by email
                var existingUser = await GetUserByEmailAsync(email);

                // if the user already exists, return false
                if (existingUser != null)
                {
                    return false;
                }

                #endregion

                #region Create a new user entity and store it in the table storage

                // Hash the password
                string passwordHash = HashPassword(password);

                // If the password hash is null or empty, return false
                if (string.IsNullOrEmpty(passwordHash))
                {
                    return false;
                }

                // Create a new user entity
                var user = new User
                {
                    Role = role,
                    Id = Guid.NewGuid().ToString(),
                    Username = email,
                    PasswordHash = passwordHash,
                    Email = email,
                    Name = name,
                    StreetAddress = streetAddress,
                    City = city,
                    Province = province,
                    PostalCode = postalCode,
                    Country = country,
                    PhoneNumber = phoneNumber
                };

                // Create the user in the database
                await CreateUserAsync(user);

                #endregion

                #region create a new user cookie and sign in

                // Create the claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                // Create the claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create the authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                // Sign in the user
                await _httpContextAccessor.HttpContext!.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                #endregion

                return true; // Registration successful
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"An error occurred while registering the user: {ex.Message}");

                return false; // Registration failed
            }
        }

        // Sign in a user
        public async Task SignInAsync(string username)
        {
            try
            {
                #region Input validation

                // Check if the username is null or empty
                if (string.IsNullOrEmpty(username))
                {
                    return;
                }

                #endregion

                #region Get the user by email and check if it is null

                // Get the user by email
                var user = await GetUserByEmailAsync(username);

                // If the user is null, return
                if (user == null)
                {
                    return;
                }

                #endregion

                #region Create the claims and sign in the user

                // Create the claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                // Create the claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create the authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                // Sign in the user
                await _httpContextAccessor.HttpContext!.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                #endregion
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"An error occurred while signing in the user: {ex.Message}");
                return;
            }
        }

        // Sign out a user
        public async Task SignOutAsync()
        {
            await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<(bool isValid, string? role)> ValidateUserAsync(string username, string password)
        {
            try
            {
                #region Input validation

                if (string.IsNullOrEmpty(username))
                {
                    return (false, null);
                }

                if (string.IsNullOrEmpty(password))
                {
                    return (false, null);
                }

                #endregion

                #region Get the user by username and password

                // Hash the password
                string passwordHash = HashPassword(password);

                // If the password hash is null or empty, return false
                if (string.IsNullOrEmpty(passwordHash))
                {
                    return (false, null);
                }

                // Get the user by email and password
                var user = await GetUserByEmailAndPasswordAsync(username, passwordHash);

                #endregion

                // If the user is null, return false
                if (user == null)
                {
                    return (false, null);
                }

                // Return true and the role of the user
                return (true, user.Role);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"An error occurred during user validation: {ex.Message}");
                return (false, null);
            }
        }

        // Hash a password using SHA256
        private string HashPassword(string password)
        {
            #region Input validation

            if (string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }

            #endregion

            #region Create a SHA256 hash of the password

            // Create a SHA256 hash of the password
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                return Convert.ToBase64String(bytes);
            }

            #endregion
        }
    }
}
