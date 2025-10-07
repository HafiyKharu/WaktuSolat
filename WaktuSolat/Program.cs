using WaktuSolat.Services;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddScoped<ScrapWaktuSolatService>();
services.AddScoped<ExtractExcel>();

var serviceProvider = services.BuildServiceProvider();

try
{
    using var scope = serviceProvider.CreateScope();
    var scopedProvider = scope.ServiceProvider;
    
    var zoneCode = config["ZoneCode"] ?? throw new InvalidOperationException("ZoneCode not configured");

    Console.WriteLine($"Starting waktu solat scraper for zone: {zoneCode}");

    var scrapService = scopedProvider.GetRequiredService<ScrapWaktuSolatService>();
    var exportService = scopedProvider.GetRequiredService<ExtractExcel>();

    var waktu = await scrapService.GetWaktuSolatAsync(zoneCode);

    if (waktu == null)
    {
        Console.WriteLine("✗ Failed to retrieve waktu solat data.");
        Environment.ExitCode = 1;
        return;
    }

    await exportService.ExportToCsvAsync(waktu);

    Console.WriteLine("✓ Process completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Fatal error: {ex.Message}");
    Environment.ExitCode = 1;
}
finally
{
    await serviceProvider.DisposeAsync();
}