using TelegramBotForGitHub.Models;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface IGitHubAuthService
{
    Task<string> GetAuthorizationUrl(long userId);
    Task<GitHubOAuthToken?> ExchangeCodeForTokenAsync(string code, string state);
    Task<GitHubUserProfile?> GetUserProfileAsync(string accessToken);
    Task<bool> IsUserAuthorizedAsync(long userId);
    Task<GitHubOAuthToken?> GetUserTokenAsync(long userId);
    Task LogoutUserAsync(long userId);
}