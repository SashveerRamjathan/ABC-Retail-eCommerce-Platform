using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Models
{
    public class Product
    {
        public string Id { get; set; } // Product Id (Primary Key) 

        public string Category { get; set; } // Product Category

        public string Name { get; set; } // Product Name

        public string Description { get; set; } // Product Description

        public decimal Price { get; set; } // Product Price

        public int Quantity { get; set; } // Product Quantity
    }
}
