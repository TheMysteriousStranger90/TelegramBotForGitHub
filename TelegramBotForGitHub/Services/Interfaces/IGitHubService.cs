using Octokit;

namespace TelegramBotForGitHub.Services.Interfaces
{
    public interface IGitHubService
    {
        string GetAuthorizationUrl(long telegramUserId);
        Task<(string AccessToken, long TelegramUserId)> ExchangeCodeForTokenAsync(string code, string state);
        Task<User> GetCurrentUserAsync(string accessToken);
        Task<IReadOnlyList<Repository>> GetUserRepositoriesAsync(string accessToken);
        Task<Repository> GetRepositoryAsync(string accessToken, string owner, string repo);
        Task<IReadOnlyList<Issue>> GetRepositoryIssuesAsync(string accessToken, string owner, string repo);
        Task<Issue> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body);
        Task<IReadOnlyList<PullRequest>> GetRepositoryPullRequestsAsync(string accessToken, string owner, string repo);
        Task<IReadOnlyList<GitHubCommit>> GetRepositoryCommitsAsync(string accessToken, string owner, string repo, int count = 10);
        Task<IReadOnlyList<Release>> GetRepositoryReleasesAsync(string accessToken, string owner, string repo);
        Task<IReadOnlyList<Organization>> GetUserOrganizationsAsync(string accessToken);
        bool IsValidAuthState(string state);
        long? GetTelegramUserIdByState(string state);
        void CleanExpiredStates();
    }
}