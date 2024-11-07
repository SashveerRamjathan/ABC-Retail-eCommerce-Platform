using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using QuestPDF;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Data;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;

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

            // add the http context accessor to the services collection
            builder.Services.AddHttpContextAccessor();

            //Get the connection string
            var connectionString = builder.Configuration.GetConnectionString("AzureDatabaseConnectionString") ?? throw new InvalidOperationException("Connection string 'AzureDatabaseConnectionString' not found.");

            // Register the DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // add the account table storage service to the services collection
            builder.Services.AddSingleton<IAccountDatabaseService>(sp =>
            new AccountDatabaseStorageService(
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<AccountDatabaseStorageService>>(),
                sp.GetRequiredService<IHttpClientFactory>()
            ));

            // add the category service to the services collection
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            // add the queue service factory to the services collection
            builder.Services.AddSingleton<IQueueServiceFactory>(sp =>
            new QueueServiceFactory(
                sp.GetRequiredService<ILogger<QueueService>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IConfiguration>()));

            // add the file service factory to the services collection
            builder.Services.AddSingleton<IFileServiceFactory>(sp =>
            new FileServiceFactory(sp.GetRequiredService<ILogger<FileService>>(),
                                   sp.GetRequiredService<IQueueServiceFactory>(),
                                   sp.GetRequiredService<IHttpClientFactory>(),
                                   sp.GetRequiredService<IConfiguration>()));

            // add the product blob storage service to the services collection
            builder.Services.AddTransient<IProductBlobStorageService>(sp =>
            new ProductBlobStorageService(
                sp.GetRequiredService<ILogger<ProductBlobStorageService>>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IHttpClientFactory>()));

            // add the product table storage service to the services collection
            builder.Services.AddTransient<IProductDatabaseService>(sp =>
                new ProductDatabaseStorageService(
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ILogger<ProductDatabaseStorageService>>(),
                    sp.GetRequiredService<IHttpClientFactory>()));

            // add the order table storage service to the services collection
            builder.Services.AddTransient<IOrderDatabaseService>(sp =>
            new OrderDatabaseStorageService(
                sp.GetRequiredService<ILogger<OrderDatabaseStorageService>>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IHttpClientFactory>()));

            // configure authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {

                    options.LoginPath = "/Account/Login"; // the path to the login page
                    options.LogoutPath = "/Account/Logout"; // the path to the logout page
                    options.AccessDeniedPath = "/Account/AccessDenied"; // the path to the access denied page
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // the time span after which the cookie will expire
                    options.SlidingExpiration = true; // // Refresh cookie expiration time on each request
                });

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

            // use authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
