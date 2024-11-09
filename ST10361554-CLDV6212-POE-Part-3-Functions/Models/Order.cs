using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Models
{
    public class Order
    {
        public string Id { get; set; } // Primary key

        public string OrderId { get; set; } // Order id

        public string ProductId { get; set; } // Product id

        public string UserId { get; set; } // id of the user who placed the order

        public int Quantity { get; set; } // quantity of item ordered

        public decimal TotalItemPrice { get; set; } // total price of the ordered quantity of item

        public string ItemStatus { get; set; } // status of the order item

        public DateTime OrderDate { get; set; } // date the order was placed
    }
}
