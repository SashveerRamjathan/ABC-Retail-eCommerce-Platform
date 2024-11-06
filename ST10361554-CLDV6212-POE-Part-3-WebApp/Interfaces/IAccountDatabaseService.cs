using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for account-related operations in table storage
    public interface IAccountDatabaseService
    {
        // Create a new user in the table storage
        Task CreateUserAsync(User user);

        // Retrieve a user by their email address
        Task<User?> GetUserByEmailAsync(string email);

        // Validate a user's credentials and return their role if valid
        Task<(bool isValid, string? role)> ValidateUserAsync(string username, string password);

        // Sign in a user by their username
        Task SignInAsync(string username);

        // Sign out the current user
        Task SignOutAsync();

        // Register a new user with the provided details
        Task<bool> RegisterUserAsync(string password, string email, string role, string name,
            string streetAddress, string city, string province, string postalCode, string country, string phoneNumber);

        // Retrieve a user by their ID
        Task<User?> GetUserByIdAsync(string id);
    }
}
