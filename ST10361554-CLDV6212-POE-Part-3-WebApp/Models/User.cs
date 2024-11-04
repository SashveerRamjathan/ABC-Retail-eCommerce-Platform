using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    public class User
    {
        [Required(ErrorMessage = "A primary key is required for a user")]
        public string Id { get; set; }

        [Required(ErrorMessage = "A role is required for the user")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        public string? Name { get; set; }

        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public string? Province { get; set; }

        public string? PostalCode { get; set; }

        public string? Country { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
