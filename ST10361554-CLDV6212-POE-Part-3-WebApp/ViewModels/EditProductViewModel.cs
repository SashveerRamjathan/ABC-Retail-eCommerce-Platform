using System.ComponentModel.DataAnnotations;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class EditProductViewModel
    {
        // Unique identifier for the product
        [Display(Name = "Product ID")]
        public string ProductID { get; set; }

        // Name of the product
        [Display(Name = "Product Name")]
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }

        // Description of the product
        [Display(Name = "Product Description")]
        [Required(ErrorMessage = "Product description is required")]
        public string ProductDescription { get; set; }

        // Price of the product
        [Display(Name = "Product Price")]
        [Required(ErrorMessage = "Product price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double ProductPrice { get; set; }

        // Quantity of the product available
        [Display(Name = "Product Quantity")]
        [Required(ErrorMessage = "Product quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int ProductQuantity { get; set; }

        // Image of the product in Base64 format (optional)
        [Display(Name = "Product Image")]
        public IFormFile? Base64ProductImage { get; set; }

        // Category of the product
        [Display(Name = "Category")]
        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }
    }
}
