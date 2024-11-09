using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Models
{
    public class User
    {
        public string Id { get; set; } // Primary key

        public string Role { get; set; } // Role of the user

        public string Username { get; set; } // Username

        public string PasswordHash { get; set; } // Hashed password

        public string Email { get; set; } // Email address

        public string? Name { get; set; } // Name of the user

        public string? StreetAddress { get; set; } // Street address

        public string? City { get; set; } // City

        public string? Province { get; set; } // Province

        public string? PostalCode { get; set; } // Postal code

        public string? Country { get; set; } // Country

        public string? PhoneNumber { get; set; } // Phone number
    }
}
