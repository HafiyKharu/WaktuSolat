using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using WaktuSolat.Models;
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

    public async Task<WaktuSolatEntity?> GetWaktuSolatAsync(string zoneCode)
    {
        return await Task.Run(() => GetWaktuSolat(zoneCode));
    }

    private WaktuSolatEntity? GetWaktuSolat(string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(zoneCode))
            throw new ArgumentException("Zone code cannot be null or empty", nameof(zoneCode));

        IWebDriver? driver = null;
        ChromeDriverService? service = null;

        try
        {
            var options = ConfigureChromeOptions();
            service = ConfigureDriverService();
            driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(_timeout));
            
            ConfigureDriver(driver);
            NavigateAndSelectZone(driver, zoneCode);
            WaitForPrayerTimesToLoad(driver);

            var waktu = ExtractPrayerTimes(driver);
            return waktu;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scraping waktu solat: {ex.Message}");
            return null;
        }
        finally
        {
            driver?.Quit();
            driver?.Dispose();
            service?.Dispose();
        }
    }

    private ChromeOptions ConfigureChromeOptions()
    {
        var options = new ChromeOptions();
        options.AddArguments(
            "--headless",
            "--disable-gpu",
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--window-size=1920,1080",
            "--disable-blink-features=AutomationControlled"
        );
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        return options;
    }

    private ChromeDriverService ConfigureDriverService()
    {
        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;
        return service;
    }

    private void ConfigureDriver(IWebDriver driver)
    {
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_timeout);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
    }

    private void NavigateAndSelectZone(IWebDriver driver, string zoneCode)
    {
        driver.Navigate().GoToUrl(_url);
        Thread.Sleep(_waitForPageToLoad);

        var selectElement = new SelectElement(driver.FindElement(By.Id("inputzone")));
        selectElement.SelectByValue(zoneCode);

        Console.WriteLine($"Selected zone: {zoneCode}");
        Thread.Sleep(_waitForPageToLoad);
    }

    private void WaitForPrayerTimesToLoad(IWebDriver driver)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(_timeout))
        {
            PollingInterval = TimeSpan.FromMilliseconds(500)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        wait.Until(HasLoadedPrayerTimes);
    }

    private bool HasLoadedPrayerTimes(IWebDriver driver)
    {
        try
        {
            var nodes = driver.FindElements(By.CssSelector(".timetablerow .masa-solat"));
            if (nodes == null || nodes.Count < 7) return false;
            
            return nodes.Count(n => 
                !string.IsNullOrWhiteSpace(n.Text) && 
                n.Text.Trim() != "-" && 
                n.Text.Trim() != "00:00:00") >= 7;
        }
        catch
        {
            return false;
        }
    }

    private WaktuSolatEntity ExtractPrayerTimes(IWebDriver driver)
    {
        var hijriCal = new HijriCalendar();
        var now = DateTime.Now;

        return new WaktuSolatEntity
        {
            czone = GetTextByIdSafe(driver, "czone"),
            cbearing = GetTextByIdSafe(driver, "cbearing"),
            TarikhMasehi = now.ToString("dd/MM/yyyy"),
            TarikhHijrah = $"{hijriCal.GetDayOfMonth(now):D2}/{hijriCal.GetMonth(now):D2}/{hijriCal.GetYear(now)}",
            Imsak = GetTextByIdSafe(driver, "timsak"),
            Subuh = GetTextByIdSafe(driver, "tsubuh"),
            Syuruk = GetTextByIdSafe(driver, "tsyuruk"),
            Dhuha = GetTextByIdSafe(driver, "tdhuha"),
            Zohor = GetTextByIdSafe(driver, "tzohor"),
            Asar = GetTextByIdSafe(driver, "tasar"),
            Maghrib = GetTextByIdSafe(driver, "tmagrib"),
            Isyak = GetTextByIdSafe(driver, "tisyak")
        };
    }

    private string GetTextByIdSafe(IWebDriver driver, string id)
    {
        try
        {
            var element = driver.FindElement(By.Id(id));
            var text = element?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text) || text == "-" || text == "00:00:00")
                return string.Empty;

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }
    
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
            var op = ConfigureChromeOptions();
            service = ConfigureDriverService();
            driver = new ChromeDriver(service, op, TimeSpan.FromSeconds(_timeout));
            
            ConfigureDriver(driver);
            driver.Navigate().GoToUrl(_url);
            Thread.Sleep(_waitForPageToLoad);

            var zoneGroups = new List<ZoneGroup>();
            var selectElement = driver.FindElement(By.Id("inputzone"));
            var optGroups = selectElement.FindElements(By.TagName("optgroup"));

            foreach (var optGroup in optGroups)
            {
                var stateName = optGroup.GetAttribute("label")?.Trim();
                if (string.IsNullOrWhiteSpace(stateName)) continue;

                var zoneGroup = new ZoneGroup
                {
                    State = stateName,
                    Zones = new List<ZoneOption>()
                };

                var options = optGroup.FindElements(By.TagName("option"));
                foreach (var option in options)
                {
                    var value = option.GetAttribute("value")?.Trim();
                    var text = option.Text?.Trim();

                    if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(text))
                    {
                        zoneGroup.Zones.Add(new ZoneOption
                        {
                            Value = value,
                            Text = text
                        });
                    }
                }

                if (zoneGroup.Zones.Any())
                {
                    zoneGroups.Add(zoneGroup);
                }
            }

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
            driver?.Quit();
            driver?.Dispose();
            service?.Dispose();
        }
    }
}