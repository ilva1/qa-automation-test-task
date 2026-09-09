using QaAutomation.Api.Models;
using QaAutomation.Config;
using RestSharp;

namespace QaAutomation.Api;

/// <summary>
/// The API client owns transport: base address, timeout, headers, and later auth.
/// It returns the full RestResponse rather than only the payload, because contract
/// tests need to assert on status and headers as well as the body.
/// Tests never build a RestRequest themselves, so a change to how the service is
/// called touches this class only.
/// </summary>
public sealed class JsonPlaceholderClient : IDisposable
{
    private readonly RestClient _client;

    public JsonPlaceholderClient()
    {
        var options = new RestClientOptions(TestSettings.Current.ApiBaseUrl)
        {
            ThrowOnAnyError = false,
            Timeout = TimeSpan.FromSeconds(TestSettings.Current.TimeoutSeconds)
        };

        _client = new RestClient(options);
    }

    public Task<RestResponse<Post>> GetPostAsync(int id) =>
        _client.ExecuteAsync<Post>(new RestRequest("posts/{id}", Method.Get)
            .AddUrlSegment("id", id));

    public Task<RestResponse<List<Post>>> GetPostsByUserAsync(int userId) =>
        _client.ExecuteAsync<List<Post>>(new RestRequest("posts", Method.Get)
            .AddQueryParameter("userId", userId));

    public Task<RestResponse<List<User>>> GetUsersAsync() =>
        _client.ExecuteAsync<List<User>>(new RestRequest("users", Method.Get));

    public void Dispose() => _client.Dispose();
}
