using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QaAutomation.Config;

namespace QaAutomation.Pages;

/// <summary>
/// Shared behaviour for every page object: the driver, an explicit wait, and
/// the small set of interactions that are allowed to touch Selenium directly.
/// There are no Thread.Sleep calls anywhere in the suite; every wait is conditional.
/// </summary>
public abstract class BasePage
{
    protected readonly IWebDriver Driver;
    protected readonly WebDriverWait Wait;

    protected BasePage(IWebDriver driver)
    {
        Driver = driver;
        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(TestSettings.Current.TimeoutSeconds));
    }

    protected IWebElement WaitForVisible(By locator) =>
        Wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(locator);
                return element.Displayed ? element : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        })!;

    protected void Type(By locator, string text)
    {
        var element = WaitForVisible(locator);
        element.Clear();
        element.SendKeys(text);
    }

    protected void Click(By locator) => WaitForVisible(locator).Click();

    protected string ReadText(By locator) => WaitForVisible(locator).Text.Trim();

    protected bool Exists(By locator) => Driver.FindElements(locator).Count > 0;
}
