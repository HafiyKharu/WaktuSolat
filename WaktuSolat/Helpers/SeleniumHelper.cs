using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace WaktuSolat.Helpers;

public static class SeleniumHelper
{
    /// Configure Chrome options for headless scraping
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

    /// Configure Chrome driver service
    public static ChromeDriverService ConfigureDriverService()
    {
        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;
        return service;
    }

    /// Configure driver timeouts
    public static void ConfigureDriver(IWebDriver driver, int timeoutSeconds = 30)
    {
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(timeoutSeconds);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
    }

    /// Safely get text from element by ID
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

    /// Safely get text from element by CSS selector
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

    /// Wait for element to be present and visible
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

    /// Wait for elements to be present
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

    /// Select option from dropdown by value
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

    /// Select option from dropdown by text
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

    /// Check if element exists
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

    /// Dispose driver and service safely
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

    /// Take screenshot for debugging
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

    /// Execute JavaScript
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

    /// Scroll to element
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