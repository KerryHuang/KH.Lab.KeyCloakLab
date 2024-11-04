using KH.Lab.KeyCloakLab.Models;
using KH.Lab.KeyCloakLab.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KH.Lab.KeyCloakLab.Tests;

public class Keycloak2ServiceTests
{
    private readonly KeycloakService _keycloakService;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly IConfiguration _configuration;

    public Keycloak2ServiceTests()
    {
        // Mock configuration
        var inMemorySettings = new Dictionary<string, string> {
            {"Keycloak:BaseUrl", "http://localhost:8080"},
            {"Keycloak:Realm", "master"},
            {"Keycloak:ClientId", "admin-cli"},
            {"Keycloak:ClientSecret", "cKMFDv1Hm70NFClzMCAa77kmzyrVBdE0"},
            {"Keycloak:Username", "admin"},
            {"Keycloak:Password", "admin"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();


        // Mock HttpClient
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        _keycloakService = new KeycloakService(_configuration, httpClient);
    }

    [Fact]
    public async Task Authenticate_Should_Set_AccessToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "fake_access_token" };
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            });

        // Act
        await _keycloakService.Authenticate();

        // Assert
        // Assert
        Assert.NotNull(_keycloakService.GetAccessToken());
        Assert.Equal("fake_access_token", _keycloakService.GetAccessToken());
    }

    [Fact]
    public async Task GetUsersAsync_Should_Return_Users_List()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "1", Username = "user1" },
            new User { Id = "2", Username = "user2" }
        };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(users)
            });

        // Act
        var result = await _keycloakService.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}