using Octokit;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface IGitHubService
{
    string GetAuthorizationUrl(long telegramUserId);
    Task<string> ExchangeCodeForTokenAsync(string code, string state);
    Task<User> GetCurrentUserAsync(string accessToken);
    Task<IReadOnlyList<Repository>> GetUserRepositoriesAsync(string accessToken);
    Task<Repository> GetRepositoryAsync(string accessToken, string owner, string repo);
    Task<IReadOnlyList<Issue>> GetRepositoryIssuesAsync(string accessToken, string owner, string repo);
    Task<Issue> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body);
}