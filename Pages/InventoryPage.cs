using OpenQA.Selenium;

namespace QaAutomation.Pages;

public sealed class InventoryPage : BasePage
{
    private static readonly By InventoryContainer = By.Id("inventory_container");
    private static readonly By CartLink = By.CssSelector(".shopping_cart_link");
    private static readonly By CartBadge = By.CssSelector(".shopping_cart_badge");

    public InventoryPage(IWebDriver driver) : base(driver) { }

    public InventoryPage WaitUntilLoaded()
    {
        WaitForVisible(InventoryContainer);
        return this;
    }

    /// <summary>
    /// Located by the product name the test asks for, not by a hard-coded element id.
    /// The test therefore states its intent ("add the backpack") and the page object
    /// owns how that is found on the page.
    /// </summary>
    public InventoryPage AddToCart(string productName)
    {
        Click(AddToCartButtonFor(productName));
        return this;
    }

    public int CartItemCount =>
        Exists(CartBadge) && int.TryParse(ReadText(CartBadge), out var count) ? count : 0;

    public CartPage OpenCart()
    {
        Click(CartLink);
        return new CartPage(Driver).WaitUntilLoaded();
    }

       private static By AddToCartButtonFor(string productName) => By.XPath(
        "//div[normalize-space(@class)='inventory_item']" +
        $"[.//div[contains(@class,'inventory_item_name')][normalize-space()=\"{productName}\"]]" +
        "//button");
}