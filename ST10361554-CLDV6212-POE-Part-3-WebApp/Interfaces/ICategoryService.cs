using Microsoft.AspNetCore.Mvc.Rendering;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces
{
    // Interface for category-related operations
    public interface ICategoryService
    {
        // Retrieve a list of categories as SelectListItem objects for use in dropdowns
        List<SelectListItem> GetCategories();
    }
}
