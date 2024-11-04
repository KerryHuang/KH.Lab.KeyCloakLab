# ASP.NET Core KeyCloak ��@

�H�U�O��X .NET 8 WebAPI �M�פ� Keycloak ���Τ� CRUD �M���m�K�X�\�઺����d�ҡC�o�ӽd�Ҳ[�\�F�H�U�����G

1. �]�m Keycloak �t�m�C
2. �ϥ� `HttpClient` �{�Ҩýե� Keycloak API�C
3. Keycloak �Τ᪺ CRUD �ާ@�C
4. �Τ᭫�m�K�X�ާ@�C

### �]�m����

�����A�b�׺ݤ��Ыطs�� .NET 8 WebAPI ���ءG

```bash
dotnet new webapi -n KeycloakUserAPI
cd KeycloakUserAPI
dotnet add package Microsoft.Extensions.Configuration
```

### �]�m `appsettings.json`

�b `appsettings.json` ���K�[ Keycloak ���t�m�G

```json
{
  "Keycloak": {
    "BaseUrl": "http://localhost:8080",
    "Realm": "your-realm-name",
    "ClientId": "admin-cli",
    "ClientSecret": "your-client-secret",
    "Username": "admin",
    "Password": "admin_password"
  }
}
```

### �Ы� KeycloakService

�b `Services` ��Ƨ����Ы� `KeycloakService.cs`�A�ӪA�ȱN�B�z Keycloak ���Τ� CRUD �M���m�K�X�ާ@�C

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

public class KeycloakService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private string _accessToken;

    public KeycloakService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        Authenticate().Wait();
    }

    private async Task Authenticate()
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
        
        var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        _accessToken = responseData["access_token"].ToString();
    }

    private HttpRequestMessage CreateRequest(string endpoint, HttpMethod method)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users";
        var request = CreateRequest(endpoint, HttpMethod.Get);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<User>>();
    }

    public async Task<User> GetUserAsync(string userId)
    {
        var endpoint = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId}";
        var request = CreateRequest(endpoint, HttpMethod.Get);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<User>();
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
}
```

### �Ы� User �ҫ�

�b `Models` ��Ƨ����Ы� `User.cs` �ҫ��G

```csharp
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool Enabled { get; set; }
    public Dictionary<string, List<string>> Attributes { get; set; }
}
```

### �Ы� UsersController

�b `Controllers` ��Ƨ����Ы� `UsersController.cs`�A�B�z Keycloak �ϥΪ̪� CRUD �M�K�X���m�\��C

```csharp
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly KeycloakService _keycloakService;

    public UsersController(KeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _keycloakService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _keycloakService.GetUserAsync(userId);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        await _keycloakService.CreateUserAsync(user);
        return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] User user)
    {
        await _keycloakService.UpdateUserAsync(userId, user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        await _keycloakService.DeleteUserAsync(userId);
        return NoContent();
    }

    [HttpPut("{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
    {
        await _keycloakService.ResetPasswordAsync(userId, request.NewPassword, request.Temporary);
        return NoContent();
    }
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; }
    public bool Temporary { get; set; } = false;
}
```

### �]�m DI �M�ҰʱM��

�b `Program.cs` �����U `KeycloakService` �M `HttpClient`�G

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<KeycloakService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

