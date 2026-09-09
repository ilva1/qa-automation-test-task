using Microsoft.Extensions.Configuration;

namespace QaAutomation.Config;

/// <summary>
/// Single source of truth for environment configuration.
/// Resolution order (last wins):
///   appsettings.json  ->  appsettings.{TEST_ENV}.json  ->  QA_* environment variables
/// Secrets are never committed: the password is read from QA_StandardPassword.
/// </summary>
public sealed class TestSettings
{
    private static readonly Lazy<TestSettings> Lazy = new(Load);

    public static TestSettings Current => Lazy.Value;

    public string UiBaseUrl { get; private init; } = "https://www.saucedemo.com/";
    public string ApiBaseUrl { get; private init; } = "https://jsonplaceholder.typicode.com/";
    public string Browser { get; private init; } = "chrome";
    public bool Headless { get; private init; } = true;
    public int TimeoutSeconds { get; private init; } = 10;
    public string StandardUser { get; private init; } = "standard_user";
    public string StandardPassword { get; private init; } = "secret_sauce";

    /// <summary>Set to point the UI suite at a Selenium Grid or a container. Null means run locally.</summary>
    public string? SeleniumRemoteUrl { get; private init; }

    private TestSettings() { }

    private static TestSettings Load()
    {
        var environmentName = Environment.GetEnvironmentVariable("TEST_ENV") ?? "local";

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("QA_")
            .Build();

        return new TestSettings
        {
            UiBaseUrl = configuration["UiBaseUrl"] ?? "https://www.saucedemo.com/",
            ApiBaseUrl = configuration["ApiBaseUrl"] ?? "https://jsonplaceholder.typicode.com/",
            Browser = configuration["Browser"] ?? "chrome",
            Headless = !bool.TryParse(configuration["Headless"], out var headless) || headless,
            TimeoutSeconds = int.TryParse(configuration["TimeoutSeconds"], out var timeout) ? timeout : 10,
            StandardUser = configuration["StandardUser"] ?? "standard_user",

            // SauceDemo publishes this password on its own login page, so it is not a real secret.
            // It is still read through the same channel a real credential would use, so that
            // nothing has to change when the target is an environment with genuine secrets.
            StandardPassword = configuration["StandardPassword"] ?? "secret_sauce",

            SeleniumRemoteUrl = configuration["SeleniumRemoteUrl"]
        };
    }
}
