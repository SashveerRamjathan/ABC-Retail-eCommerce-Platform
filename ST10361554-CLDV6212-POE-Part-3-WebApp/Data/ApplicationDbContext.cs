using Microsoft.EntityFrameworkCore;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Models;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }
    }
}
