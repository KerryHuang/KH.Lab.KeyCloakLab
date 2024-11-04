using KH.Lab.KeyCloakLab.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KH.Lab.KeyCloakLab.Tests;

public class Keycloak1ServiceTests
{
    private readonly KeycloakService _keycloakService;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;

    public Keycloak1ServiceTests()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        _keycloakService = new KeycloakService(new Mock<IConfiguration>().Object, httpClient);
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Keycloak:BaseUrl"]).Returns("http://localhost:8080");
        configuration.Setup(c => c["Keycloak:Realm"]).Returns("master");
        configuration.Setup(c => c["Keycloak:ClientId"]).Returns("admin-cli");
        configuration.Setup(c => c["Keycloak:ClientSecret"]).Returns("cKMFDv1Hm70NFClzMCAa77kmzyrVBdE0");
        configuration.Setup(c => c["Keycloak:Username"]).Returns("admin");
        configuration.Setup(c => c["Keycloak:Password"]).Returns("admin");

        var httpClient = new Mock<HttpClient>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"myaccesstoken\"}", Encoding.UTF8, "application/json")
        };
        httpClient.Setup(h => h.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(response);

        var keycloakService = new KeycloakService(configuration.Object, httpClient.Object);

        // Act
        await keycloakService.Authenticate();

        // Assert
        Assert.NotEmpty(keycloakService.GetAccessToken());
    }

    [Fact]
    public async Task Authenticate_InvalidCredentials_ReturnsEmptyAccessToken()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Keycloak:BaseUrl"]).Returns("https://example.com");
        configuration.Setup(c => c["Keycloak:Realm"]).Returns("myrealm");
        configuration.Setup(c => c["Keycloak:ClientId"]).Returns("myclient");
        configuration.Setup(c => c["Keycloak:ClientSecret"]).Returns("mysecret");
        configuration.Setup(c => c["Keycloak:Username"]).Returns("invaliduser");
        configuration.Setup(c => c["Keycloak:Password"]).Returns("invalidpassword");

        var httpClient = new Mock<HttpClient>();
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        httpClient.Setup(h => h.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(response);

        var keycloakService = new KeycloakService(configuration.Object, httpClient.Object);

        // Act
        await keycloakService.Authenticate();

        // Assert
        Assert.Empty(keycloakService.GetAccessToken());
    }

    [Fact]
    public async Task Authenticate_MissingConfigurationValues_ThrowsException()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Keycloak:BaseUrl"]).Returns((string)null);

        var httpClient = new Mock<HttpClient>();

        var keycloakService = new KeycloakService(configuration.Object, httpClient.Object);

        // Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => keycloakService.Authenticate());
    }

    [Fact]
    public async Task Authenticate_InvalidHttpResponse_ThrowsException()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Keycloak:BaseUrl"]).Returns("https://example.com");
        configuration.Setup(c => c["Keycloak:Realm"]).Returns("myrealm");
        configuration.Setup(c => c["Keycloak:ClientId"]).Returns("myclient");
        configuration.Setup(c => c["Keycloak:ClientSecret"]).Returns("mysecret");
        configuration.Setup(c => c["Keycloak:Username"]).Returns("myuser");
        configuration.Setup(c => c["Keycloak:Password"]).Returns("mypassword");

        var httpClient = new Mock<HttpClient>();
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        httpClient.Setup(h => h.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(response);

        var keycloakService = new KeycloakService(configuration.Object, httpClient.Object);

        // Act and Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => keycloakService.Authenticate());
    }

    [Fact]
    public async Task Authenticate_Should_Set_AccessToken_When_Successful_Response()
    {
        // Arrange
        var tokenUrl = "http://localhost:8080/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("client_id", "client_id"),
        new KeyValuePair<string, string>("client_secret", "client_secret"),
        new KeyValuePair<string, string>("username", "username"),
        new KeyValuePair<string, string>("password", "password"),
        new KeyValuePair<string, string>("grant_type", "password")
    });
        var responseData = new Dictionary<string, object> { { "access_token", "fake_access_token" } };

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(responseData)
            });

        // Act
        await _keycloakService.Authenticate();

        // Assert
        Assert.Equal("fake_access_token", _keycloakService.GetAccessToken());
    }

    [Fact]
    public async Task Authenticate_Should_Throw_Exception_When_Unsuccessful_Response()
    {
        // Arrange
        var tokenUrl = "http://localhost/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("client_id", "client_id"),
        new KeyValuePair<string, string>("client_secret", "client_secret"),
        new KeyValuePair<string, string>("username", "username"),
        new KeyValuePair<string, string>("password", "password"),
        new KeyValuePair<string, string>("grant_type", "password")
    });

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _keycloakService.Authenticate());
    }

    [Fact]
    public async Task Authenticate_Should_Throw_Exception_When_Invalid_Response_Data()
    {
        // Arrange
        var tokenUrl = "http://localhost/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("client_id", "client_id"),
        new KeyValuePair<string, string>("client_secret", "client_secret"),
        new KeyValuePair<string, string>("username", "username"),
        new KeyValuePair<string, string>("password", "password"),
        new KeyValuePair<string, string>("grant_type", "password")
    });

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _keycloakService.Authenticate());
    }


}