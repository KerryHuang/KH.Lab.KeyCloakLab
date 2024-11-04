using KH.Lab.KeyCloakLab.Models;
using System.Text;
using System.Text.Json;

namespace KH.Lab.KeyCloakLab.Services;

public class KeycloakService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private string _accessToken = string.Empty;

    public KeycloakService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        Authenticate().Wait();
    }

    /// <summary>
    /// Authenticates with the Keycloak server using the client credentials flow.
    /// </summary>
    /// <remarks>
    /// This method sends a POST request to the Keycloak token endpoint with the client ID, client secret, username, password, and grant type.
    /// It then reads the response content as JSON and extracts the access token.
    /// </remarks>
    public async Task Authenticate()
    {
        var tokenUrl = $"{_configuration["Keycloak:BaseUrl"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["Keycloak:ClientId"]),
            new KeyValuePair<string, string>("client_secret", _configuration["Keycloak:ClientSecret"]),
            new KeyValuePair<string, string>("username", _configuration["Keycloak:Username"]),
            new KeyValuePair<string, string>("password", _configuration["Keycloak:Password"]),
            new KeyValuePair<string, string>("grant_type", "password")
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        response.EnsureSuccessStatusCode();

        _accessToken = await GetAccessTokenFromResponse(response);
    }

    private async Task<string> GetAccessTokenFromResponse(HttpResponseMessage response)
    {
        var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return responseData?["access_token"]?.ToString() ?? string.Empty;
    }

    private HttpRequestMessage CreateRequest(string endpoint, HttpMethod method)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }

    #region Clients
    public async Task<IEnumerable<Client>?> GetClientsAsync()
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/clients";
        var request = CreateRequest(endpoint, HttpMethod.Get);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<Client>>();
    }

    public async Task<Client?> GetClientAsync(string clientId)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/clients/{clientId}";
        var request = CreateRequest(endpoint, HttpMethod.Get);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Client>();
    }

    public async Task CreateClientAsync(Client client)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/clients";
        var request = CreateRequest(endpoint, HttpMethod.Post);
        request.Content = JsonContent.Create(client);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateClientAsync(string clientId, Client client)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/clients/{clientId}";
        var request = CreateRequest(endpoint, HttpMethod.Put);
        request.Content = JsonContent.Create(client);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteClientAsync(string clientId)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/clients/{clientId}";
        var request = CreateRequest(endpoint, HttpMethod.Delete);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
    #endregion

    #region Users
    public async Task<IEnumerable<User>?> GetUsersAsync()
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users";
        var request = CreateRequest(endpoint, HttpMethod.Get);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<User>>();
    }

    public async Task<User?> GetUserAsync(string username)
    {
        var baseUrl = _configuration["Keycloak:BaseUrl"];
        var realm = _configuration["Keycloak:Realm"];
        var endpoint = $"{baseUrl}/admin/realms/{realm}/users?username={username}";

        var request = CreateRequest(endpoint, HttpMethod.Get);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<IEnumerable<User>>();

        return users?.FirstOrDefault();
    }

    public async Task CreateUserAsync(User user)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users";
        var request = CreateRequest(endpoint, HttpMethod.Post);
        request.Content = JsonContent.Create(user);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserAsync(string userId, User user)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId}";
        var request = CreateRequest(endpoint, HttpMethod.Put);
        request.Content = JsonContent.Create(user);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string userId)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId}";
        var request = CreateRequest(endpoint, HttpMethod.Delete);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, bool temporary = false)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId}/reset-password";
        var request = CreateRequest(endpoint, HttpMethod.Put);

        var passwordPayload = new
        {
            type = "password",
            value = newPassword,
            temporary = temporary
        };

        request.Content = new StringContent(JsonSerializer.Serialize(passwordPayload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
    #endregion
}