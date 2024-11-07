using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class RegisterViewModel
    {
        // Email address of the user
        [Required(ErrorMessage = "An email address is required to register")]
        [EmailAddress]
        public string Email { get; set; }

        // Password of the user
        [Required(ErrorMessage = "A password is required to register")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Name of the user
        [Required(ErrorMessage = "Name is required to register")]
        public string Name { get; set; }

        // Street address of the user
        [Required(ErrorMessage = "Street address is required to register")]
        public string StreetAddress { get; set; }

        // City of the user
        [Required(ErrorMessage = "City is required to register")]
        public string City { get; set; }

        // Province of the user
        [Required(ErrorMessage = "Province is required to register")]
        public string Province { get; set; }

        // Postal code of the user
        [Required(ErrorMessage = "Postal Code is required to register")]
        public string PostalCode { get; set; }

        // Country of the user
        [Required(ErrorMessage = "Country is required to register")]
        public string Country { get; set; }

        // Phone number of the user
        [Required(ErrorMessage = "Phone Number is required to register")]
        [Phone]
        public string PhoneNumber { get; set; }
    }
}
