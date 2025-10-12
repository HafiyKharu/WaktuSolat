using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WaktuSolat.Services;
using WaktuSolat.Repository;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Register DbContext
builder.Services.AddDbContext<WaktuSolatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ScrapWaktuSolatService>();
builder.Services.AddScoped<WaktuSolatRepository>();

var host = builder.Build();

try
{
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    
    // Run migrations
    var dbContext = services.GetRequiredService<WaktuSolatDbContext>();
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("✓ Database migrations applied");
    
    var zoneCode = config["ZoneCode"] ?? throw new InvalidOperationException("ZoneCode not configured");

    Console.WriteLine($"Starting waktu solat scraper for zone: {zoneCode}");

    var scrapService = services.GetRequiredService<ScrapWaktuSolatService>();
    var repository = services.GetRequiredService<WaktuSolatRepository>();

    var waktu = await scrapService.GetWaktuSolatAsync(zoneCode);

    if (waktu == null)
    {
        Console.WriteLine("✗ Failed to retrieve waktu solat data.");
        Environment.ExitCode = 1;
        return;
    }

    await repository.SaveAsync(waktu);

    Console.WriteLine("✓ Process completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Fatal error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.ExitCode = 1;
}