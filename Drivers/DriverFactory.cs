using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using QaAutomation.Config;

namespace QaAutomation.Drivers;

/// <summary>
/// The only place in the suite that knows how a browser is created.
/// Tests and page objects never construct a driver, so switching browser,
/// going headless, or moving to a grid is a configuration change, not a code change.
/// Driver binaries are resolved by Selenium Manager (built into Selenium 4.6+),
/// so there is no driver-manager dependency and nothing to install by hand.
/// </summary>
public static class DriverFactory
{
    public static IWebDriver Create(TestSettings settings)
    {
        var browser = settings.Browser.Trim().ToLowerInvariant();

        DriverOptions options = browser switch
        {
            "firefox" => BuildFirefoxOptions(settings),
            "chrome" => BuildChromeOptions(settings),
            _ => throw new NotSupportedException(
                $"Browser '{settings.Browser}' is not supported. Use 'chrome' or 'firefox'.")
        };

        IWebDriver driver = string.IsNullOrWhiteSpace(settings.SeleniumRemoteUrl)
            ? CreateLocal(browser, options)
            : new RemoteWebDriver(new Uri(settings.SeleniumRemoteUrl), options);

        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(settings.TimeoutSeconds * 3);
        driver.Manage().Window.Maximize();

        return driver;
    }

    private static IWebDriver CreateLocal(string browser, DriverOptions options) => browser switch
    {
        "firefox" => new FirefoxDriver((FirefoxOptions)options),
        _ => new ChromeDriver((ChromeOptions)options)
    };

    private static ChromeOptions BuildChromeOptions(TestSettings settings)
    {
        var options = new ChromeOptions();

        if (settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        // Required when the suite runs inside a container.
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");

        // SauceDemo triggers Chrome's breached-password bubble, which can steal focus.
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);

        return options;
    }

    private static FirefoxOptions BuildFirefoxOptions(TestSettings settings)
    {
        var options = new FirefoxOptions();

        if (settings.Headless)
        {
            options.AddArgument("-headless");
        }

        return options;
    }
}
