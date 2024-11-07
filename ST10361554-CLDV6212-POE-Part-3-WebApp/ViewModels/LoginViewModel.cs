using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class LoginViewModel
    {
        // Email address of the user
        [Required(ErrorMessage = "An email address is required to login")]
        [EmailAddress]
        public string Email { get; set; }

        // Password of the user
        [Required(ErrorMessage = "A password is required to login")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
