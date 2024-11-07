using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class ProductViewModel
    {
        // Name of the product
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        // Description of the product
        [Display(Name = "Product Description")]
        public string ProductDescription { get; set; }

        // Price of the product
        [Display(Name = "Product Price")]
        public decimal ProductPrice { get; set; }

        // Quantity of the product available
        [Display(Name = "Product Quantity")]
        public int ProductQuantity { get; set; }

        // Base64 encoded image of the product
        [Display(Name = "Product Image")]
        public string Base64ProductImage { get; set; }

        // Partition key for the product 
        public string Category { get; set; }

        // Row key for the product 
        public string ProductId { get; set; }
    }
}
