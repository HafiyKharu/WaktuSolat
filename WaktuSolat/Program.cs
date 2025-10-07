using WaktuSolat.Model;
using System.Globalization;
using CsvHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

// Change this to your preferred zone
string zoneCode = "WLY01"; 

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
var hijriCal = new HijriCalendar();

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

    var columnMap = ExtractFromColumns();

    var waktu = new WaktuSolatEntity
    {
        czone = GetTextByIdSafe("czone"),
        cbearing = GetTextByIdSafe("cbearing"),
        TarikhMasehi = DateTime.Now.ToString("dd/MM/yyyy"),
        TarikhHijrah = $"{hijriCal.GetDayOfMonth(DateTime.Now)}/{hijriCal.GetMonth(DateTime.Now)}/{hijriCal.GetYear(DateTime.Now)}",
        Imsak = GetTextByIdSafe("timsak"),
        Subuh = GetTextByIdSafe("tsubuh"),
        Syuruk = GetTextByIdSafe("tsyuruk"),
        Dhuha = GetTextByIdSafe("tdhuha"),
        Zohor = GetTextByIdSafe("tzohor"),
        Asar = GetTextByIdSafe("tasar"),
        Maghrib = GetTextByIdSafe("tmagrib"),
        Isyak = GetTextByIdSafe("tisyak")
    };

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