using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Models
{
    public class Order
    {
        public string Id { get; set; } // order id

        public string ProductId { get; set; } // product id

        public string UserId { get; set; } // id of the user who placed the order

        public int Quantity { get; set; }

        public decimal TotalItemPrice { get; set; } // total price of the ordered quantity of items

        public string ItemStatus { get; set; } // status of the order
    }
}
