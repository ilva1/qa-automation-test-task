using OpenQA.Selenium;
using QaAutomation.Config;

namespace QaAutomation.Pages;

public sealed class LoginPage : BasePage
{
    private static readonly By UsernameField = By.Id("user-name");
    private static readonly By PasswordField = By.Id("password");
    private static readonly By LoginButton = By.Id("login-button");
    private static readonly By ErrorMessage = By.CssSelector("[data-test='error']");

    public LoginPage(IWebDriver driver) : base(driver) { }

    public LoginPage Open()
    {
        Driver.Navigate().GoToUrl(TestSettings.Current.UiBaseUrl);
        WaitForVisible(UsernameField);
        return this;
    }

    /// <summary>
    /// Returns the next page object, so the test reads as a journey through the
    /// application rather than as a list of clicks.
    /// </summary>
    public InventoryPage LoginAs(string username, string password)
    {
        Type(UsernameField, username);
        Type(PasswordField, password);
        Click(LoginButton);

        return new InventoryPage(Driver).WaitUntilLoaded();
    }

    /// <summary>
    /// The negative path is a separate method rather than a flag on LoginAs.
    /// A test that expects to stay on the login page should not wait for the
    /// inventory page to appear and then time out; it should wait for the error.
    /// </summary>
    public LoginPage SubmitExpectingFailure(string username, string password)
    {
        Type(UsernameField, username);
        Type(PasswordField, password);
        Click(LoginButton);

        WaitForVisible(ErrorMessage);
        return this;
    }

    public string ErrorText => ReadText(ErrorMessage);

    public bool HasError => Exists(ErrorMessage);
}
