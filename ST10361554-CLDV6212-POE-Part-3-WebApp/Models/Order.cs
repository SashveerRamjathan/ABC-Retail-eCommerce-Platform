﻿namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    public class Order
    {
        public string Id { get; set; } // Unique identifier for the order

        public string OrderId { get; set; } // order id that will be shared amongst all items in the order

        public string UserId { get; set; } // id of the user who placed the order

        public string ProductId { get; set; } // product id

        public int Quantity { get; set; } // quantity of items ordered

        public decimal TotalItemPrice { get; set; } // total price of the ordered quantity of items

        public string ItemStatus { get; set; } // status of the order

        public DateTime OrderDate { get; set; } // date when the order was placed
    }
}
