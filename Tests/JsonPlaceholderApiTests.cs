using System.Net;
using NUnit.Framework;
using QaAutomation.Api;

namespace QaAutomation.Tests;

/// <summary>
/// API tests need no browser, so they do not inherit BaseUiTest. Keeping the two
/// suites separate is what lets the API layer run in seconds in CI while the UI
/// layer runs on a grid.
/// </summary>
[TestFixture]
[Category("API")]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class JsonPlaceholderApiTests
{
    private JsonPlaceholderClient _api = null!;

    [SetUp]
    public void CreateClient() => _api = new JsonPlaceholderClient();

    [TearDown]
    public void DisposeClient() => _api.Dispose();

    [Test]
    [Category("Smoke")]
    [Description("GET /posts/{id} returns 200 and a body that matches the expected contract.")]
    public async Task GettingASinglePostReturnsTheExpectedContract()
    {
        var response = await _api.GetPostAsync(1);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "status code");
        Assert.That(response.ContentType, Does.Contain("application/json"), "content type");

        var post = response.Data;
        Assert.That(post, Is.Not.Null, "the body deserialised into a Post");

        Assert.Multiple(() =>
        {
            Assert.That(post!.Id, Is.EqualTo(1), "id echoes the requested resource");
            Assert.That(post.UserId, Is.GreaterThan(0), "userId is populated");
            Assert.That(post.Title, Is.Not.Null.And.Not.Empty, "title");
            Assert.That(post.Body, Is.Not.Null.And.Not.Empty, "body");
        });
    }

    [Test]
    [Description("GET /posts?userId=1 returns only that user's posts, and every item is complete.")]
    public async Task FilteringPostsByUserReturnsOnlyThatUsersPosts()
    {
        const int userId = 1;

        var response = await _api.GetPostsByUserAsync(userId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null.And.Not.Empty, "at least one post is returned");

        Assert.Multiple(() =>
        {
            foreach (var post in response.Data!)
            {
                Assert.That(post.UserId, Is.EqualTo(userId), $"post {post.Id} belongs to user {userId}");
                Assert.That(post.Title, Is.Not.Null.And.Not.Empty, $"post {post.Id} title");
                Assert.That(post.Body, Is.Not.Null.And.Not.Empty, $"post {post.Id} body");
            }
        });
    }

    [Test]
    [Description("GET /users returns a list in which every user has its key properties populated.")]
    public async Task EveryUserInTheListHasItsKeyPropertiesPopulated()
    {
        var response = await _api.GetUsersAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null.And.Not.Empty, "the user list is not empty");

        Assert.Multiple(() =>
        {
            foreach (var user in response.Data!)
            {
                Assert.That(user.Id, Is.GreaterThan(0), "id");
                Assert.That(user.Name, Is.Not.Null.And.Not.Empty, $"user {user.Id} name");
                Assert.That(user.Username, Is.Not.Null.And.Not.Empty, $"user {user.Id} username");
                Assert.That(user.Email, Is.Not.Null.And.Not.Empty, $"user {user.Id} email");
                Assert.That(user.Email, Does.Contain("@"), $"user {user.Id} email looks like an address");
                Assert.That(user.Address?.City, Is.Not.Null.And.Not.Empty, $"user {user.Id} city");
            }
        });
    }
}
