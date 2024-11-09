using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    public class User
    {
        [Required(ErrorMessage = "A primary key is required for a user")]
        public string Id { get; set; } // User ID : GUID

        [Required(ErrorMessage = "A role is required for the user")]
        public string Role { get; set; } // User role

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } // Username

        [Required]
        public string PasswordHash { get; set; } // Password hash

        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } // Email address

        public string? Name { get; set; } // Name of the user

        public string? StreetAddress { get; set; } // Street address

        public string? City { get; set; } // City

        public string? Province { get; set; } // Province or state

        public string? PostalCode { get; set; } // Postal or ZIP code

        public string? Country { get; set; } // Country

        public string? PhoneNumber { get; set; } // Contact phone number
    }
}
