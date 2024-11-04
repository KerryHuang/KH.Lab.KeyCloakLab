using KH.Lab.KeyCloakLab.Models;
using KH.Lab.KeyCloakLab.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KH.Lab.KeyCloakLab.Tests;

public class Keycloak3ServiceTests
{
    public class KeycloakServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpClient> _httpClientMock;
        private readonly KeycloakService _keycloakService;

        public KeycloakServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientMock = new Mock<HttpClient>();
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);
            _keycloakService = new KeycloakService(_configurationMock.Object, _httpClientFactoryMock.Object.CreateClient());
        }

        [Fact]
        public async Task GetUsersAsync_SuccessfulRetrieval()
        {
            // Arrange
            var users = new[] { new User { Id = "1", Username = "user1" } };
            var responseContent = new StringContent(JsonSerializer.Serialize(users), Encoding.UTF8, "application/json");
            _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });

            // Act
            var result = await _keycloakService.GetUsersAsync();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(users[0].Id, result.First().Id);
        }

        [Fact]
        public async Task GetUsersAsync_AuthenticationFailure()
        {
            // Arrange
            _configurationMock.Setup(c => c["Keycloak:Username"]).Throws(new Exception("Authentication failed"));

            // Act and Assert
            await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetUsersAsync());
        }

        [Fact]
        public async Task GetUsersAsync_HttpRequestFailure()
        {
            // Arrange
            _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _keycloakService.GetUsersAsync());
        }

        [Fact]
        public async Task GetUsersAsync_JsonDeserializationFailure()
        {
            // Arrange
            var responseContent = new StringContent("Invalid JSON", Encoding.UTF8, "application/json");
            _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });

            // Act and Assert
            await Assert.ThrowsAsync<JsonException>(() => _keycloakService.GetUsersAsync());
        }

        [Fact]
        public async Task GetUsersAsync_EmptyUserList()
        {
            // Arrange
            var responseContent = new StringContent("[]", Encoding.UTF8, "application/json");
            _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });

            // Act
            var result = await _keycloakService.GetUsersAsync();

            // Assert
            Assert.Empty(result);
        }
    }
}
