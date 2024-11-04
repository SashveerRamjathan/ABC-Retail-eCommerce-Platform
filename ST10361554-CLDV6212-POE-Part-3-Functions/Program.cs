using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ST10361554_CLDV6212_POE_Part_3_Functions.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Retrieve the connection string
        var connectionString = Environment.GetEnvironmentVariable("AzureDatabaseConnectionString") ?? throw new InvalidOperationException("Connection string 'AzureDatabaseConnectionString' not found.");

        // Register the DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

host.Run();
