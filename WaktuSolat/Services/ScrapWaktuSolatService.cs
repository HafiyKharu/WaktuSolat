using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WaktuSolat.Models;
using WaktuSolat.Helpers;
using Microsoft.Extensions.Configuration;

namespace WaktuSolat.Services;

public class ScrapWaktuSolatService
{
    private readonly IConfiguration _config;
    private readonly string _url;
    private readonly int _timeout;
    private readonly int _waitForPageToLoad;

    public ScrapWaktuSolatService(IConfiguration config)
    {
        _config = config;
        _url = _config["WaktuSolatApiBaseUrl"] ?? throw new InvalidOperationException("WaktuSolatApiBaseUrl is not configured in appsettings.json");
        _timeout = _config.GetValue<int>("WaktuSolatApiTimeout");
        _waitForPageToLoad = _config.GetValue<int>("WaitForPageToLoad");

        if (_timeout <= 0) throw new InvalidOperationException("WaktuSolatApiTimeout must be greater than 0");
        if (_waitForPageToLoad <= 0) throw new InvalidOperationException("WaitForPageToLoad must be greater than 0");
    }

    /// <summary>
    /// Scrape waktu solat data from e-solat website with retry
    /// </summary>
    public async Task<WaktuSolatEntity?> ScrapeWaktuSolatAsync(string zoneCode, int maxRetries = 3)
    {
        return await Task.Run(() => WaktuSolatScrapHelper.ScrapeWithRetry(
            () => ScrapeWaktuSolat(zoneCode),
            zoneCode,
            maxRetries
        ));
    }

    private WaktuSolatEntity? ScrapeWaktuSolat(string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(zoneCode))
            throw new ArgumentException("Zone code cannot be null or empty", nameof(zoneCode));

        IWebDriver? driver = null;
        ChromeDriverService? service = null;

        try
        {
            var options = SeleniumHelper.ConfigureChromeOptions();
            service = SeleniumHelper.ConfigureDriverService();
            driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(_timeout));

            SeleniumHelper.ConfigureDriver(driver, _timeout);
            WaktuSolatScrapHelper.NavigateAndSelectZone(driver, _url, zoneCode, _waitForPageToLoad);
            WaktuSolatScrapHelper.WaitForPrayerTimesToLoad(driver, _timeout);

            var waktu = WaktuSolatScrapHelper.ExtractPrayerTimes(driver);

            if (WaktuSolatScrapHelper.IsValidData(waktu, zoneCode))
            {
                Console.WriteLine($"✓ Successfully scraped waktu solat for zone {zoneCode}");
                return waktu;
            }
            else
            {
                Console.WriteLine($"✗ Invalid data scraped for zone {zoneCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping waktu solat for zone {zoneCode}: {ex.Message}");
            return null;
        }
        finally
        {
            SeleniumHelper.SafeDispose(driver, service);
        }
    }

    /// <summary>
    /// Get all available zones from e-solat website
    /// </summary>
    public async Task<List<ZoneGroup>> GetAllZonesAsync()
    {
        return await Task.Run(() => GetAllZones());
    }

    private List<ZoneGroup> GetAllZones()
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
            Console.WriteLine($"✗ Error listing zones: {ex.Message}");
            return new List<ZoneGroup>();
        }
        finally
        {
            SeleniumHelper.SafeDispose(driver, service);
        }
    }
}