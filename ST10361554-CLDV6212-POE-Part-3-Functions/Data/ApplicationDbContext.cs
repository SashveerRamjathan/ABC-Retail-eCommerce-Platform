using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ST10361554_CLDV6212_POE_Part_3_Functions.Models;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; } // User table

        public DbSet<Product> Products { get; set; } // Product table

        public DbSet<Order> Orders { get; set; } // Order table
    }
}
