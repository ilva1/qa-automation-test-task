using OpenQA.Selenium;

namespace QaAutomation.Pages;

public sealed class CartPage : BasePage
{
    private static readonly By CartList = By.CssSelector(".cart_list");
    private static readonly By CartItem = By.CssSelector(".cart_item");
    private static readonly By ItemName = By.CssSelector(".inventory_item_name");
    private static readonly By ItemQuantity = By.CssSelector(".cart_quantity");

    public CartPage(IWebDriver driver) : base(driver) { }

    public CartPage WaitUntilLoaded()
    {
        WaitForVisible(CartList);
        return this;
    }

    /// <summary>
    /// The page object returns plain data, never IWebElement. Selenium types are not
    /// allowed to leak into the test layer, so assertions read as domain statements
    /// and a change of locator or driver never reaches a test.
    /// </summary>
    public IReadOnlyList<string> ProductNames => Driver
        .FindElements(CartItem)
        .Select(row => row.FindElement(ItemName).Text.Trim())
        .ToList();

    public int QuantityOf(string productName) => Driver
        .FindElements(CartItem)
        .Where(row => row.FindElement(ItemName).Text.Trim() == productName)
        .Select(row => int.TryParse(row.FindElement(ItemQuantity).Text.Trim(), out var qty) ? qty : 0)
        .FirstOrDefault();
}
