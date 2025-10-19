using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WaktuSolat.Models;
using WaktuSolat.Repository;
using WaktuSolat.Helpers;

namespace WaktuSolat.Services;

public class ZoneService
{
    private readonly ZoneRepository _repository;
    private readonly IConfiguration _config;
    private readonly string _url;
    private readonly int _timeout;
    private readonly int _waitForPageToLoad;

    public ZoneService(ZoneRepository repository, IConfiguration config)
    {
        _repository = repository;
        _config = config;
        _url = _config["WaktuSolatApiBaseUrl"] ?? throw new InvalidOperationException("WaktuSolatApiBaseUrl is not configured in appsettings.json");
        _timeout = _config.GetValue<int>("WaktuSolatApiTimeout");
        _waitForPageToLoad = _config.GetValue<int>("WaitForPageToLoad");

        if (_timeout <= 0) throw new InvalidOperationException("WaktuSolatApiTimeout must be greater than 0");
        if (_waitForPageToLoad <= 0) throw new InvalidOperationException("WaitForPageToLoad must be greater than 0");
    }

    /// Get zones from database, if empty scrape and save first
    public async Task<List<ZoneGroup>> GetZonesGroupedAsync()
    {
        try
        {
            var zones = await _repository.GetZonesGroupedAsync();

            // If database is empty, scrape and save first
            if (!zones.Any())
            {
                Console.WriteLine("No zones in database. Scraping from website...");
                var scraped = await ScrapAndSaveZonesAsync();

                if (scraped)
                {
                    zones = await _repository.GetZonesGroupedAsync();
                }
            }

            return zones;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting grouped zones: {ex.Message}");
            throw;
        }
    }

    /// Get all zones from database as flat list
    public async Task<List<Zone>> GetAllZonesAsync()
    {
        try
        {
            var zones = await _repository.GetAllZonesAsync();

            // If database is empty, scrape and save first
            if (!zones.Any())
            {
                Console.WriteLine("No zones in database. Scraping from website...");
                var scraped = await ScrapAndSaveZonesAsync();

                if (scraped)
                {
                    zones = await _repository.GetAllZonesAsync();
                }
            }

            return zones;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting all zones: {ex.Message}");
            throw;
        }
    }

    /// Scrape zones from website and save to database
    public async Task<bool> ScrapAndSaveZonesAsync()
    {
        try
        {
            Console.WriteLine("=== Scraping zones from website ===");

            // Get zones from scraping
            var zoneGroups = await ScrapeZonesFromWebsiteAsync();

            if (!zoneGroups.Any())
            {
                Console.WriteLine("✗ No zones scraped from website");
                return false;
            }

            // Convert to ZoneInput format
            var zones = new List<ZoneInput>();
            foreach (var group in zoneGroups)
            {
                foreach (var zone in group.Zones)
                {
                    zones.Add(new ZoneInput
                    {
                        ZoneCode = zone.Value,
                        State = group.State,
                        Description = zone.Text
                    });
                }
            }

            Console.WriteLine($"Scraped {zones.Count} zones from {zoneGroups.Count} states");

            // Bulk insert to database
            var saved = await _repository.BulkInsertZonesAsync(zones);

            if (saved)
            {
                Console.WriteLine($"✓ Successfully saved {zones.Count} zones to database");
            }
            else
            {
                Console.WriteLine("✗ Failed to save zones to database");
            }

            return saved;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping and saving zones: {ex.Message}");
            throw;
        }
    }

    /// Get zones scrape and save first
    public async Task<List<ZoneGroup>> ScrapZonesGroupedAsync()
    {
        try
        {
            var zones = await _repository.GetZonesGroupedAsync();
            Console.WriteLine("Scraping from website...");
            var scraped = await ScrapAndSaveZonesAsync();
            if (scraped)
            {
                zones = await _repository.GetZonesGroupedAsync();
            }

            return zones;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting grouped zones: {ex.Message}");
            throw;
        }
    }

    #region Scrape Helpers
    private async Task<List<ZoneGroup>> ScrapeZonesFromWebsiteAsync()
    {
        return await Task.Run(() => ScrapeZones());
    }

    private List<ZoneGroup> ScrapeZones()
    {
        IWebDriver? driver = null;
        ChromeDriverService? service = null;

        try
        {
            var options = SeleniumHelper.ConfigureChromeOptions();
            service = SeleniumHelper.ConfigureDriverService();
            driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(_timeout));

            SeleniumHelper.ConfigureDriver(driver, _timeout);
            driver.Navigate().GoToUrl(_url);
            Thread.Sleep(_waitForPageToLoad);

            var zoneGroups = WaktuSolatScrapHelper.ExtractZoneGroups(driver);

            Console.WriteLine($"✓ Retrieved {zoneGroups.Count} states with {zoneGroups.Sum(z => z.Zones.Count)} zones");
            return zoneGroups;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping zones: {ex.Message}");
            return new List<ZoneGroup>();
        }
        finally
        {
            SeleniumHelper.SafeDispose(driver, service);
        }
    }
    #endregion
}