# QA Automation — Test Task

A small C# / .NET test suite: three UI tests against SauceDemo with Selenium WebDriver, and three API tests against JSONPlaceholder with RestSharp, on NUnit.

The suite is deliberately small. What I have tried to show is the shape of the framework around it — where each concern lives and what is not allowed to leak between layers — because that is what decides whether a suite is still maintainable in year three.

---

## Install and run

**Prerequisites:** .NET 8 SDK, and Chrome installed. Driver binaries are resolved automatically by Selenium Manager, which is built into Selenium 4.6+, so there is nothing else to install.

```bash
dotnet restore
dotnet build
```

Run the full suite:

```bash
dotnet test
```

Run a single test:

```bash
dotnet test --filter "FullyQualifiedName~AddingAnItemPutsItInTheCart"
```

Run one layer, or only the smoke tests:

```bash
dotnet test --filter "Category=API"
dotnet test --filter "Category=UI"
dotnet test --filter "Category=Smoke"
```

Run the UI tests with a visible browser, or in Firefox:

```bash
QA_Headless=false dotnet test --filter "Category=UI"
QA_Browser=firefox dotnet test --filter "Category=UI"
```

On Windows PowerShell, set the variable first: `$env:QA_Headless="false"`.

### Where the results land

```bash
dotnet test --logger "trx;LogFileName=results.trx" --results-directory ./TestResults
```

- The console shows pass/fail per test as it runs.
- `./TestResults/results.trx` is the machine-readable result file. This is the file a CI server reads to publish a test report; on Azure DevOps it is consumed by `PublishTestResults`, and Jenkins reads it with the MSTest plugin.
- On a UI failure, a PNG screenshot is written to `TestResults/screenshots/` and attached to the test result, and the URL at the moment of failure is written into the test output.

`TestResults/` and `screenshots/` are gitignored, along with `bin/` and `obj/`.

---

## Design decisions

- **Page Object Model with data-only returns.** Page objects expose `IReadOnlyList<string> ProductNames`, never `IWebElement`. Selenium types stop at the page layer, so a locator change or a driver upgrade cannot reach a test.
- **Fluent navigation between pages.** `LoginAs` returns an `InventoryPage`, `OpenCart` returns a `CartPage`. The test then reads as the journey a user takes, and an impossible sequence is difficult to write by accident.
- **A factory owns the driver, configuration owns the environment.** No test constructs a browser or knows a URL. Switching to headed, to Firefox, to a container or to a grid is a configuration change.
- **NUnit over xUnit** for its `[Category]` filtering, `Assert.Multiple`, and `[FixtureLifeCycle(InstancePerTestCase)]`, which is what makes `ParallelScope.All` safe with an instance-level driver field. I also come from TestNG in Java, and NUnit's fixture and parallelism model is the closest match.
- **Deliberately skipped:** SpecFlow, a reporting library, Docker, WireMock, and a CI workflow. With one hour, I judged that the layering and this document were worth more than a fourth tool. What I would add first is listed at the end.

### Assumptions I made

- "Assert the response contract" means status, content type, and every key property present, correctly typed, and non-empty — rather than a full JSON-schema validation, which I note below as the next step.
- The SauceDemo password is published on the site's own login page, so it is not a real secret. It is still read through the same configuration channel a real credential would use, so nothing has to change when the target is an environment with genuine secrets.
- The tests are written to describe correct behaviour of the system under test, not to pass regardless. If SauceDemo changes, a test should fail.

---

## Framework structure

This is the framework I would put in place for a suite that has to survive years of a real project. **Sections marked *built* exist in this repository; sections marked *described* are design only.**

### Layers, and what each one owns — *built*

```
Tests/       what is true about the product.   Assertions live here and nowhere else.
Pages/       how the product is operated.      Locators live here and nowhere else.
Api/         how the service is called.        Requests and transport live here.
Drivers/     how a browser is created.
Config/      what environment we are pointed at.
```

The rule that matters is the direction of dependency: tests depend on pages and API clients; pages and clients never depend on tests. Concretely, what is **not allowed to leak**:

- **No `IWebElement`, `By`, or `IWebDriver` above the page layer.** A test that reaches for a locator has moved a maintenance cost into a place where it will be duplicated.
- **No assertions inside page objects.** A page object reports state; the test decides whether that state is correct. Assertions inside pages produce failures that cannot be interpreted without reading the page class.
- **No URLs, credentials, or environment names in tests.** They come from `TestSettings`.
- **No `RestRequest` construction in tests.** Tests call `GetPostAsync(1)`; the client owns the route, headers and timeout.
- **No `Thread.Sleep` anywhere.** Every wait is a condition with a timeout.

### Driver lifecycle and browser configuration — *built*

`DriverFactory` is the only place in the suite that constructs a browser. `BaseUiTest` creates one in `[SetUp]` and quits it in `[TearDown]`, so a browser lives exactly as long as one test.

Running the same suite in three places is one setting each, and no code change:

| Where | How |
|---|---|
| Locally | default; `QA_Headless=false` to watch it |
| In a container | headless, with `--no-sandbox` and `--disable-dev-shm-usage` already set in `DriverFactory` |
| On a grid | set `QA_SeleniumRemoteUrl`; the factory returns a `RemoteWebDriver` with the same options object |

The same `ChromeOptions` are used in all three cases, which is the point — a test that passes locally and fails on the grid should not be failing because the browser was configured differently.

### Environment configuration and secrets — *built for config, described for secrets*

`TestSettings` layers three sources, last one winning:

```
appsettings.json  →  appsettings.{TEST_ENV}.json  →  QA_* environment variables
```

Pointing the suite at another environment is `TEST_ENV=staging dotnet test`, with a committed `appsettings.staging.json` holding non-secret values.

Secrets are never in the repository and never in `appsettings.*`. They arrive as `QA_*` environment variables, which locally come from an untracked `.env` and in CI from the secret store — Azure DevOps variable groups backed by Key Vault, or Jenkins credentials binding. The value is injected at the start of the run and is masked in logs. *(Described: this repository has no real secret to protect, so only the reading mechanism exists.)*

### Where page objects, step definitions, test data and API clients belong — *partly built*

- **Page objects** — `Pages/`, one class per page, inheriting `BasePage`. *Built.*
- **API clients** — `Api/`, one client per service, with response models in `Api/Models/`. *Built.*
- **Test data** — currently constants in the test that uses them. At scale this becomes `Data/`, with builders that produce valid objects and let a test override only the field under test: `UserBuilder.Default().WithNoEmail().Build()`. The point is that a test states only what makes it different, so adding a required field to the domain does not touch fifty tests. *Described.*
- **Step definitions**, if SpecFlow is added — `Steps/`, and they stay thin. A step definition translates a Gherkin line into one or two page-object calls; it holds no locators and no business logic. Shared state between steps goes through an injected context object, not static fields. *Described.*

### Test independence and parallelism — *built*

Each test creates its own browser and its own data, and asserts nothing about the order it ran in. `[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]` gives every test its own fixture instance, so the `Driver` field cannot be shared across threads, which is what makes `[Parallelizable(ParallelScope.All)]` safe. `LevelOfParallelism` is 3 locally and would be raised in CI or replaced by a grid.

The constraint this puts on the suite: no test may depend on a record another test created, and any test that mutates shared state needs its own tenant or its own user. Where a real application has genuinely global state — a feature flag, a maintenance mode — those tests are marked into a serial category and run in their own stage rather than being allowed to make the whole suite serial.

### Failure reporting, and test problem vs product bug — *partly built*

On failure the suite captures a screenshot, the URL at the point of failure, and the assertion message naming what was being checked. *Built.* For a long-lived suite I would add the browser console log, the failing request/response pair for API tests, and a trace or video for UI tests. *Described.*

Distinguishing a test problem from a product bug is mostly a matter of designing failures to be self-explaining before they happen:

- **Assertion messages say what was expected of the product**, so a red test reads as a statement about the application rather than about the framework.
- **A failure that is not reproducible on a rerun is a suspect test, not a bug.** I would track a rerun-flakiness rate per test rather than allowing blanket automatic retries, because retries hide product race conditions as effectively as they hide test ones.
- **Same failure across unrelated tests points at the environment**; a single failure in one assertion with the surrounding steps passing points at the product.
- **The screenshot answers it fastest.** If the page shows the expected state and the assertion failed, the test is wrong — a timing or locator problem. If the page shows the wrong state, it is the product.
- **A test that has never passed is not evidence of a bug**, and a failing test whose last change was to the test rather than the application is a test problem until proved otherwise.

### CI integration, and what it is allowed to block — *described*

Three stages, ordered by how fast they give an answer:

1. **API and unit tests** on every push, in a container, a few minutes. **Blocks the merge.**
2. **UI smoke** (`Category=Smoke`) on every pull request, headless on a grid. **Blocks the merge.**
3. **Full UI regression** nightly and before release. **Does not block a merge**, but blocks the release and raises a ticket.

What the pipeline is allowed to block is the part worth being strict about. A suite that blocks merges must be trusted, and it only stays trusted if a red build reliably means a real problem. So the merge-blocking stages hold only tests that are fast and stable; a test that becomes flaky is moved out of the blocking stage the same day and fixed, rather than being left to erode confidence until people start ignoring red builds. Quarantine is a stage, not a delete — a quarantined test still runs and is still reported, it just does not stop anyone.

Alongside this the pipeline runs static analysis with a quality gate. In my current project a DevOps engineer owns the SonarQube configuration and I work alongside it: my job is that the automated suite in the pipeline stays stable enough for the gate to mean something.

---

## What I would add first, in order

1. **Response-schema validation** for the API layer, so a contract change is caught as a schema failure rather than as a null field three assertions later.
2. **A CI workflow** running the API tests and the UI smoke category on every pull request. Cheap, and it is the point at which the suite starts protecting the product rather than describing it.
3. **A test data builder layer**, before the number of tests makes retrofitting expensive.
4. **A reporting layer** (Allure or the ExtentReports equivalent) with screenshots and history attached, so a failure can be triaged without rerunning it locally.
5. **A Dockerfile and a compose file** running the suite against Selenium Grid, so local and CI runs are the same run.
6. **SpecFlow (or Reqnroll, which supersedes it)** for the customer-facing journeys — but only for those. I would not put the whole suite behind Gherkin; it earns its cost where a non-engineer reads the scenario, and it is overhead everywhere else.

## What is unfinished

Everything under *described* above is design only. The suite has six tests, which is enough to demonstrate the layering and not enough to be a regression pack. If the hour had continued, item 1 and item 2 on the list above are what I would have built next, in that order.
