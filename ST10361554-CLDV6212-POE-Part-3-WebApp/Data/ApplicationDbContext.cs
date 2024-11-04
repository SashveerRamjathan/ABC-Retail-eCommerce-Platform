using Microsoft.EntityFrameworkCore;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }


    }
}
