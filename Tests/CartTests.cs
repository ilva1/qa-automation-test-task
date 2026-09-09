using NUnit.Framework;
using QaAutomation.Config;
using QaAutomation.Pages;

namespace QaAutomation.Tests;

[TestFixture]
[Category("UI")]
[Category("Smoke")]
public sealed class CartTests : BaseUiTest
{
    private const string Backpack = "Sauce Labs Backpack";
    private const string Bikelight = "Sauce Labs Bike Light";

    [Test]
    [Description("A logged-in user can add an item to the cart and see it in the cart.")]
    public void AddingAnItemPutsItInTheCart()
    {
        var settings = TestSettings.Current;

        var inventory = new LoginPage(Driver)
            .Open()
            .LoginAs(settings.StandardUser, settings.StandardPassword)
            .AddToCart(Backpack);

        Assert.That(inventory.CartItemCount, Is.EqualTo(1), "cart badge after adding one item");

        var cart = inventory.OpenCart();

        Assert.Multiple(() =>
        {
            Assert.That(cart.ProductNames, Has.Count.EqualTo(1), "number of lines in the cart");
            Assert.That(cart.ProductNames, Does.Contain(Backpack), "the item that was added");
            Assert.That(cart.QuantityOf(Backpack), Is.EqualTo(1), "quantity of the added item");
        });
    }

    [Test]
    [Category("UI")]
    [Description("Two items added in sequence both reach the cart. Proves the cart accumulates rather than replaces.")]
    public void AddingTwoItemsPutsBothInTheCart()
    {
        var settings = TestSettings.Current;

        var cart = new LoginPage(Driver)
            .Open()
            .LoginAs(settings.StandardUser, settings.StandardPassword)
            .AddToCart(Backpack)
            .AddToCart(Bikelight)
            .OpenCart();

        Assert.That(cart.ProductNames, Is.EquivalentTo(new[] { Backpack, Bikelight }));
    }

    [Test]
    [Category("UI")]
    [Description("A rejected login shows an error and does not reach the inventory.")]
    public void LoginWithAWrongPasswordIsRejected()
    {
        var login = new LoginPage(Driver)
            .Open()
            .SubmitExpectingFailure(TestSettings.Current.StandardUser, "not-the-password");

        Assert.Multiple(() =>
        {
            Assert.That(login.HasError, Is.True, "an error message is shown");
            Assert.That(login.ErrorText, Does.Contain("do not match"), "the error explains why");
            Assert.That(Driver.Url, Does.Not.Contain("inventory"), "the user stays on the login page");
        });
    }
}
