using System.Text.Json.Serialization;

namespace QaAutomation.Api.Models;

public sealed class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address")]
    public Address? Address { get; set; }
}

public sealed class Address
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("zipcode")]
    public string? Zipcode { get; set; }
}
