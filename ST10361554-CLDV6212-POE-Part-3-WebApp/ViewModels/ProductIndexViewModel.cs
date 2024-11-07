using Microsoft.AspNetCore.Mvc.Rendering;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels
{
    public class ProductIndexViewModel
    {
        // List of products to be displayed
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();

        // List of categories for filtering products
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

        // Currently selected category for filtering
        public string? SelectedCategory { get; set; }
    }
}
