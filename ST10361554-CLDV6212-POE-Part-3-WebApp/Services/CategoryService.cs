using Microsoft.AspNetCore.Mvc.Rendering;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Services
{
    // Service for managing product categories
    public class CategoryService : ICategoryService
    {
        // Method to get a list of product categories
        public List<SelectListItem> GetCategories()
        {
            return new List<SelectListItem>()
            {
                new SelectListItem {Value = "Electronics", Text = "Electronics"},
                new SelectListItem {Value = "Clothing", Text = "Clothing"},
                new SelectListItem {Value = "Furniture", Text = "Furniture"},
                new SelectListItem {Value = "Books", Text = "Books"},
                new SelectListItem {Value = "Toys", Text = "Toys"},
                new SelectListItem {Value = "Food", Text = "Food"},
                new SelectListItem {Value = "Appliances", Text = "Appliances"},
                new SelectListItem {Value = "Tools", Text = "Tools"},
                new SelectListItem {Value = "Automotive", Text = "Automotive"},
                new SelectListItem {Value = "Health", Text = "Health"},
                new SelectListItem {Value = "Beauty", Text = "Beauty"},
                new SelectListItem {Value = "Sports", Text = "Sports"},
                new SelectListItem {Value = "Outdoors", Text = "Outdoors"},
                new SelectListItem {Value = "Music", Text = "Music"},
                new SelectListItem {Value = "Movies", Text = "Movies"},
                new SelectListItem {Value = "Games", Text = "Games"},
                new SelectListItem {Value = "Pets", Text = "Pets"},
                new SelectListItem {Value = "Garden", Text = "Garden"},
                new SelectListItem {Value = "Jewellery", Text = "Jewellery"}
            }.OrderBy(x => x.Text).ToList();
        }
    }
}
