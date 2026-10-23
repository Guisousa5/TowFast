using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;

namespace TowFast.IntegrationTests;

public sealed class ApiSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Swagger_ui_and_openapi_are_available()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html")).StatusCode);
        var document = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("TowFast API", document, StringComparison.Ordinal);
        Assert.Contains("Bearer", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Client_can_register_and_login()
    {
        var email = $"client-{Guid.NewGuid():N}@towfast.test";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterClientCommand(
            "Client Test", DocumentType.Cpf, "52998224725", email, "11999999999", "StrongPass1"));
        var registerBody = await register.Content.ReadAsStringAsync();
        Assert.True(register.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)register.StatusCode}: {registerBody}");
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "StrongPass1"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Liveness_is_available_without_database_details()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_a_bearer_token()
    {
        var response = await _client.GetAsync("/api/v1/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reused_refresh_token_revokes_its_replacement()
    {
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterClientCommand(
            "Refresh Test", DocumentType.Cpf, "11144477735", $"refresh-{Guid.NewGuid():N}@towfast.test",
            "11988888888", "StrongPass1"));
        register.EnsureSuccessStatusCode();
        var initial = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(initial);

        var firstRotation = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(initial.RefreshToken));
        firstRotation.EnsureSuccessStatusCode();
        var replacement = await firstRotation.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(replacement);

        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(initial.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var revokedReplacement = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(replacement.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, revokedReplacement.StatusCode);
    }

    [Fact]
    public async Task Invalid_cpf_returns_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterClientCommand(
            "Invalid CPF", DocumentType.Cpf, "12345678900", $"invalid-{Guid.NewGuid():N}@towfast.test",
            "11977777777", "StrongPass1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
