using WaktuSolat.Model;
using System;
using System.IO;
using System.Globalization;
using CsvHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

// Configuration: set your desired zone code here
string zoneCode = "PRK01"; // Change this to your preferred zone (e.g., "JHR02", "KDH01", etc.)

var url = "https://www.e-solat.gov.my/";

var options = new ChromeOptions();
options.AddArgument("--headless");
options.AddArgument("--disable-gpu");
options.AddArgument("--no-sandbox");
options.AddArgument("--disable-dev-shm-usage");
options.AddArgument("--window-size=1920,1080");
options.AddExcludedArgument("enable-automation");
options.AddAdditionalOption("useAutomationExtension", false);

using var service = ChromeDriverService.CreateDefaultService();
service.SuppressInitialDiagnosticInformation = true;
service.HideCommandPromptWindow = true;

try
{
    using var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(60));
    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
    driver.Navigate().GoToUrl(url);

    // Wait for page to load
    Thread.Sleep(2000);

    // Select the zone from dropdown
    var zoneDropdown = driver.FindElement(By.Id("inputzone"));
    zoneDropdown.SendKeys(zoneCode);
    zoneDropdown.SendKeys(Keys.Enter);
    
    Console.WriteLine($"Selected zone: {zoneCode}");

    // Wait for the page to update with new zone data
    Thread.Sleep(3000);

    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(45))
    {
        PollingInterval = TimeSpan.FromMilliseconds(500)
    };
    wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

    bool HasLoadedPrayerTimes(IWebDriver d)
    {
        try
        {
            var nodes = d.FindElements(By.CssSelector(".timetablerow .masa-solat"));
            if (nodes == null || nodes.Count < 7) return false;
            return nodes.Count(n => !string.IsNullOrWhiteSpace(n.Text) && n.Text.Trim() != "-" && n.Text.Trim() != "00:00:00") >= 7;
        }
        catch
        {
            return false;
        }
    }

    wait.Until(d => HasLoadedPrayerTimes(d));

    string GetTextByIdSafe(string id)
    {
        try
        {
            var el = driver.FindElement(By.Id(id));
            var txt = el?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(txt) || txt == "-" || txt == "00:00:00") return string.Empty;
            return txt ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    Dictionary<string, string> ExtractFromColumns()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cols = driver.FindElements(By.CssSelector(".timetablerow .col"));
        foreach (var col in cols)
        {
            try
            {
                var titleEl = col.FindElement(By.CssSelector("h1"));
                var timeEl = col.FindElement(By.CssSelector("p.masa-solat"));
                var title = titleEl?.Text?.Trim()?.ToUpperInvariant();
                var time = timeEl?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(time))
                {
                    map[title] = time;
                }
            }
            catch { }
        }
        return map;
    }

    var waktu = new WaktuSolatEntity
    {
        czone = GetTextByIdSafe("czone"),
        cbearing = GetTextByIdSafe("cbearing"),
        TarikhMasehi = DateTime.Now.ToString("dd/MM/yyyy"),
        TarikhHijrah = string.Empty,
        Imsak = GetTextByIdSafe("timsak"),
        Subuh = GetTextByIdSafe("tsubuh"),
        Syuruk = GetTextByIdSafe("tsyuruk"),
        Zohor = GetTextByIdSafe("tzohor"),
        Asar = GetTextByIdSafe("tasar"),
        Maghrib = GetTextByIdSafe("tmagrib"),
        Isyak = GetTextByIdSafe("tisyak")
    };

    if (new[] { waktu.Imsak, waktu.Subuh, waktu.Syuruk, waktu.Zohor, waktu.Asar, waktu.Maghrib, waktu.Isyak }.Any(string.IsNullOrWhiteSpace))
    {
        var fb = ExtractFromColumns();
        if (string.IsNullOrWhiteSpace(waktu.Imsak) && fb.TryGetValue("IMSAK", out var v1)) waktu.Imsak = v1;
        if (string.IsNullOrWhiteSpace(waktu.Subuh) && fb.TryGetValue("SUBUH", out var v2)) waktu.Subuh = v2;
        if (string.IsNullOrWhiteSpace(waktu.Syuruk) && fb.TryGetValue("SYURUK", out var v3)) waktu.Syuruk = v3;
        if (string.IsNullOrWhiteSpace(waktu.Zohor) && fb.TryGetValue("ZOHOR", out var v4)) waktu.Zohor = v4;
        if (string.IsNullOrWhiteSpace(waktu.Asar) && fb.TryGetValue("ASAR", out var v5)) waktu.Asar = v5;
        if (string.IsNullOrWhiteSpace(waktu.Maghrib) && fb.TryGetValue("MAGHRIB", out var v6)) waktu.Maghrib = v6;
        if (string.IsNullOrWhiteSpace(waktu.Isyak) && fb.TryGetValue("ISYAK", out var v7)) waktu.Isyak = v7;
    }

    var outDir = Path.Combine(Directory.GetCurrentDirectory(), "WaktuSolatCSV");
    Directory.CreateDirectory(outDir);
    var outFile = Path.Combine(outDir, $"waktusolat_{zoneCode}_{DateTime.Now:yyyyMMdd}.csv");

    using var writer = new StreamWriter(outFile);
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    csv.WriteRecords(new[] { waktu });

    Console.WriteLine($"Saved waktu solat for zone {zoneCode} to: {outFile}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}