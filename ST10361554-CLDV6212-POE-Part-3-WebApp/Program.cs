using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using QuestPDF;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Data;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // add license setting for questPDF service
            Settings.License = LicenseType.Community;

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // add the http client factory to the services collection
            builder.Services.AddHttpClient();

            //Get the connection string
            var connectionString = builder.Configuration.GetConnectionString("AzureDatabaseConnectionString") ?? throw new InvalidOperationException("Connection string 'AzureDatabaseConnectionString' not found.");

            // Register the DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
