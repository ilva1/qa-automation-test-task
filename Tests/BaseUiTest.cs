using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using QaAutomation.Config;
using QaAutomation.Drivers;

namespace QaAutomation.Tests;

/// <summary>
/// Owns the driver lifecycle for every UI test.
///
/// InstancePerTestCase gives each test its own fixture instance, so the Driver field
/// is never shared between threads. That is what makes ParallelScope.All safe here:
/// one browser per test, created in SetUp and closed in TearDown, with no state
/// carried from one test to the next.
/// </summary>
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class BaseUiTest
{
    protected IWebDriver Driver = null!;

    [SetUp]
    public void StartBrowser()
    {
        Driver = DriverFactory.Create(TestSettings.Current);
    }

    [TearDown]
    public void StopBrowser()
    {
        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                CaptureScreenshot();
            }
        }
        catch (Exception ex)
        {
            // A diagnostics failure must never replace the real test failure.
            TestContext.WriteLine($"Could not capture screenshot: {ex.Message}");
        }
        finally
        {
            Driver?.Quit();
        }
    }

    private void CaptureScreenshot()
    {
        if (Driver is not ITakesScreenshot screenshotDriver)
        {
            return;
        }

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);

        var safeName = string.Join("_", TestContext.CurrentContext.Test.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(directory, $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.png");

        screenshotDriver.GetScreenshot().SaveAsFile(path);

        TestContext.AddTestAttachment(path, "Screenshot at point of failure");
        TestContext.WriteLine($"Screenshot: {path}");
        TestContext.WriteLine($"URL at failure: {Driver.Url}");
    }
}
