using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace WaktuSolat.Helpers;

/// <summary>
/// Helper class for Selenium WebDriver operations
/// </summary>
public static class SeleniumHelper
{
    /// <summary>
    /// Configure Chrome options for headless scraping
    /// </summary>
    public static ChromeOptions ConfigureChromeOptions()
    {
        var options = new ChromeOptions();
        options.AddArguments(
            "--headless",
            "--disable-gpu",
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--window-size=1920,1080",
            "--disable-blink-features=AutomationControlled",
            "--disable-extensions",
            "--disable-infobars",
            "--disable-notifications",
            "--disable-popup-blocking",
            "--ignore-certificate-errors"
        );
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        
        // Add random user agent to avoid detection
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        return options;
    }

    /// <summary>
    /// Configure Chrome driver service
    /// </summary>
    public static ChromeDriverService ConfigureDriverService()
    {
        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;
        return service;
    }

    /// <summary>
    /// Configure driver timeouts
    /// </summary>
    public static void ConfigureDriver(IWebDriver driver, int timeoutSeconds = 30)
    {
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(timeoutSeconds);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Safely get text from element by ID
    /// </summary>
    public static string GetTextByIdSafe(IWebDriver driver, string id)
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

    /// <summary>
    /// Safely get text from element by CSS selector
    /// </summary>
    public static string GetTextByCssSelectorSafe(IWebDriver driver, string cssSelector)
    {
        try
        {
            var element = driver.FindElement(By.CssSelector(cssSelector));
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

    /// <summary>
    /// Wait for element to be present and visible
    /// </summary>
    public static IWebElement? WaitForElement(IWebDriver driver, By by, int timeoutSeconds = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
            {
                PollingInterval = TimeSpan.FromMilliseconds(500)
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            
            return wait.Until(d =>
            {
                var element = d.FindElement(by);
                return element.Displayed ? element : null;
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Wait for elements to be present
    /// </summary>
    public static IReadOnlyCollection<IWebElement>? WaitForElements(IWebDriver driver, By by, int timeoutSeconds = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
            {
                PollingInterval = TimeSpan.FromMilliseconds(500)
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            
            return wait.Until(d =>
            {
                var elements = d.FindElements(by);
                return elements.Count > 0 ? elements : null;
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Select option from dropdown by value
    /// </summary>
    public static bool SelectDropdownByValue(IWebDriver driver, string elementId, string value)
    {
        try
        {
            var selectElement = new SelectElement(driver.FindElement(By.Id(elementId)));
            selectElement.SelectByValue(value);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error selecting dropdown value: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Select option from dropdown by text
    /// </summary>
    public static bool SelectDropdownByText(IWebDriver driver, string elementId, string text)
    {
        try
        {
            var selectElement = new SelectElement(driver.FindElement(By.Id(elementId)));
            selectElement.SelectByText(text);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error selecting dropdown text: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if element exists
    /// </summary>
    public static bool ElementExists(IWebDriver driver, By by)
    {
        try
        {
            var elements = driver.FindElements(by);
            return elements.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dispose driver and service safely
    /// </summary>
    public static void SafeDispose(IWebDriver? driver, ChromeDriverService? service)
    {
        try
        {
            driver?.Quit();
            driver?.Dispose();
            service?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error disposing driver: {ex.Message}");
        }
    }

    /// <summary>
    /// Take screenshot for debugging
    /// </summary>
    public static void TakeScreenshot(IWebDriver driver, string filename)
    {
        try
        {
            var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(filename);
            Console.WriteLine($"✓ Screenshot saved: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error taking screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute JavaScript
    /// </summary>
    public static object? ExecuteJavaScript(IWebDriver driver, string script, params object[] args)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            return jsExecutor.ExecuteScript(script, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error executing JavaScript: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Scroll to element
    /// </summary>
    public static void ScrollToElement(IWebDriver driver, IWebElement element)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView(true);", element);
            Thread.Sleep(500); // Wait for scroll
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scrolling to element: {ex.Message}");
        }
    }
}