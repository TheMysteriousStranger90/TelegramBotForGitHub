using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Tests;

public class GitHubAuthServiceTests
{
    private readonly Mock<IDbService> _dbServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly Mock<ILogger<GitHubAuthService>> _loggerMock;
    private readonly GitHubAuthService _service;

    public GitHubAuthServiceTests()
    {
        _dbServiceMock = new Mock<IDbService>();
        _configurationMock = new Mock<IConfiguration>();
        _httpClientMock = new Mock<HttpClient>();
        _loggerMock = new Mock<ILogger<GitHubAuthService>>();

        _configurationMock.Setup(x => x["GitHub:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(x => x["BaseUrl"]).Returns("https://example.com");

        _service = new GitHubAuthService(
            _dbServiceMock.Object,
            _configurationMock.Object,
            new HttpClient(),
            _loggerMock.Object);
    }

    [Fact]
    public async Task IsUserAuthorizedAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var userId = 123L;
        var token = new GitHubOAuthToken
        {
            UserId = userId,
            IsActive = true,
            AccessToken = "token",
            Id = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _dbServiceMock.Setup(x => x.GetGitHubTokenAsync(userId))
            .ReturnsAsync(token);

        // Act
        var result = await _service.IsUserAuthorizedAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserAuthorizedAsync_WithNoToken_ReturnsFalse()
    {
        // Arrange
        var userId = 123L;
        _dbServiceMock.Setup(x => x.GetGitHubTokenAsync(userId))
            .ReturnsAsync((GitHubOAuthToken)null);

        // Act
        var result = await _service.IsUserAuthorizedAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAuthorizationUrl_ReturnsValidUrl()
    {
        // Arrange
        var userId = 123L;

        // Act
        var url = await _service.GetAuthorizationUrl(userId);

        // Assert
        Assert.Contains("github.com/login/oauth/authorize", url);
        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains("scope=", url);
    }

    [Fact]
    public async Task LogoutUserAsync_CallsDeactivateToken()
    {
        // Arrange
        var userId = 123L;

        // Act
        await _service.LogoutUserAsync(userId);

        // Assert
        _dbServiceMock.Verify(x => x.DeactivateUserTokenAsync(userId), Times.Once);
    }
}