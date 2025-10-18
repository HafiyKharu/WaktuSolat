using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WaktuSolat.Models;
using System.Globalization;

namespace WaktuSolat.Helpers;

/// <summary>
/// Helper class specific to Waktu Solat scraping operations
/// </summary>
public static class WaktuSolatScrapHelper
{
    /// <summary>
    /// Navigate to e-solat website and select zone
    /// </summary>
    public static void NavigateAndSelectZone(IWebDriver driver, string url, string zoneCode, int waitMs = 2000)
    {
        driver.Navigate().GoToUrl(url);
        Thread.Sleep(waitMs);

        var selectElement = new SelectElement(driver.FindElement(By.Id("inputzone")));
        selectElement.SelectByValue(zoneCode);

        Console.WriteLine($"Selected zone: {zoneCode}");
        Thread.Sleep(waitMs + 500); // Extra wait after selection
    }

    /// <summary>
    /// Wait for prayer times to load on the page
    /// </summary>
    public static void WaitForPrayerTimesToLoad(IWebDriver driver, int timeoutSeconds = 30)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
        {
            PollingInterval = TimeSpan.FromMilliseconds(500)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        wait.Until(HasLoadedPrayerTimes);
    }

    /// <summary>
    /// Check if prayer times have loaded
    /// </summary>
    private static bool HasLoadedPrayerTimes(IWebDriver driver)
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

    /// <summary>
    /// Extract prayer times from the page
    /// </summary>
    public static WaktuSolatEntity ExtractPrayerTimes(IWebDriver driver)
    {
        var hijriCal = new HijriCalendar();
        var now = DateTime.Now;

        return new WaktuSolatEntity
        {
            czone = SeleniumHelper.GetTextByIdSafe(driver, "czone"),
            cbearing = SeleniumHelper.GetTextByIdSafe(driver, "cbearing"),
            TarikhMasehi = now.ToString("dd/MM/yyyy"),
            TarikhHijrah = $"{hijriCal.GetDayOfMonth(now):D2}/{hijriCal.GetMonth(now):D2}/{hijriCal.GetYear(now)}",
            Imsak = SeleniumHelper.GetTextByIdSafe(driver, "timsak"),
            Subuh = SeleniumHelper.GetTextByIdSafe(driver, "tsubuh"),
            Syuruk = SeleniumHelper.GetTextByIdSafe(driver, "tsyuruk"),
            Dhuha = SeleniumHelper.GetTextByIdSafe(driver, "tdhuha"),
            Zohor = SeleniumHelper.GetTextByIdSafe(driver, "tzohor"),
            Asar = SeleniumHelper.GetTextByIdSafe(driver, "tasar"),
            Maghrib = SeleniumHelper.GetTextByIdSafe(driver, "tmagrib"),
            Isyak = SeleniumHelper.GetTextByIdSafe(driver, "tisyak")
        };
    }

    /// <summary>
    /// Validate that scraped data is valid and matches expected zone
    /// </summary>
    public static bool IsValidData(WaktuSolatEntity? entity, string expectedZoneCode)
    {
        if (entity == null) return false;

        // Check if zone code matches (case insensitive)
        if (!entity.czone.ToUpper().Contains(expectedZoneCode.ToUpper()))
        {
            Console.WriteLine($"✗ Zone mismatch: Expected {expectedZoneCode}, got {entity.czone}");
            return false;
        }

        // Check if we have prayer times
        if (string.IsNullOrWhiteSpace(entity.Subuh) ||
            string.IsNullOrWhiteSpace(entity.Zohor) ||
            string.IsNullOrWhiteSpace(entity.Asar))
        {
            Console.WriteLine($"✗ Missing prayer times for zone {expectedZoneCode}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extract all zone options from dropdown
    /// </summary>
    public static List<ZoneGroup> ExtractZoneGroups(IWebDriver driver)
    {
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

        return zoneGroups;
    }

    /// <summary>
    /// Retry scraping with exponential backoff
    /// </summary>
    public static WaktuSolatEntity? ScrapeWithRetry(
        Func<WaktuSolatEntity?> scrapFunc,
        string zoneCode,
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"Attempt {attempt}/{maxRetries} for zone {zoneCode}");
                var result = scrapFunc();

                if (result != null && IsValidData(result, zoneCode))
                {
                    Console.WriteLine($"✓ Successfully scraped {zoneCode} on attempt {attempt}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Attempt {attempt} failed for {zoneCode}: {ex.Message}");

                if (attempt < maxRetries)
                {
                    Thread.Sleep(1000 * attempt); // Exponential backoff: 1s, 2s, 3s
                }
            }
        }

        Console.WriteLine($"✗ All {maxRetries} attempts failed for zone {zoneCode}");
        return null;
    }
}