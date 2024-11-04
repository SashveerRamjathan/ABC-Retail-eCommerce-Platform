using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Models
{
    public class User
    {
        public string Id { get; set; }

        public string Role { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string StreetAddress { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }

        public string PhoneNumber { get; set; }
    }
}
