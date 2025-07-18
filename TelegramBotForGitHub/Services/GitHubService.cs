using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System.Collections.Concurrent;
using TelegramBotForGitHub.Configuration;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubConfiguration _config;
    private readonly ILogger<GitHubService> _logger;
    private readonly ConcurrentDictionary<string, GitHubAuthState> _authStates = new();

    public GitHubService(IOptions<BotConfiguration> config, ILogger<GitHubService> logger)
    {
        _config = config.Value.GitHub;
        _logger = logger;
    }

    public string GetAuthorizationUrl(long telegramUserId)
    {
        var state = Guid.NewGuid().ToString();
        _authStates[state] = new GitHubAuthState
        {
            TelegramUserId = telegramUserId,
            State = state
        };

        var request = new OauthLoginRequest(_config.ClientId)
        {
            Scopes = { "repo", "user", "read:org" },
            State = state
        };

        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<(string AccessToken, long TelegramUserId)> ExchangeCodeForTokenAsync(string code, string state)
    {
        try
        {
            if (!_authStates.TryRemove(state, out var authState))
            {
                throw new InvalidOperationException("Invalid state parameter");
            }

            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"));
            var request = new OauthTokenRequest(_config.ClientId, _config.ClientSecret, code);
            var token = await client.Oauth.CreateAccessToken(request);

            return (token.AccessToken, authState.TelegramUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for token");
            throw;
        }
    }

    public async Task<User> GetCurrentUserAsync(string accessToken)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            return await client.User.Current();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            throw;
        }
    }

    public async Task<IReadOnlyList<Repository>> GetUserRepositoriesAsync(string accessToken)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            return await client.Repository.GetAllForCurrent(new RepositoryRequest
            {
                Sort = RepositorySort.Updated,
                Direction = SortDirection.Descending
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user repositories");
            throw;
        }
    }

    public async Task<Repository> GetRepositoryAsync(string accessToken, string owner, string repo)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            return await client.Repository.Get(owner, repo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<Issue>> GetRepositoryIssuesAsync(string accessToken, string owner, string repo)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
                SortProperty = IssueSort.Updated,
                SortDirection = SortDirection.Descending
            };

            return await client.Issue.GetAllForRepository(owner, repo, issueRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository issues for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<Issue> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            var issue = new NewIssue(title) { Body = body };
            return await client.Issue.Create(owner, repo, issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue in {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<PullRequest>> GetRepositoryPullRequestsAsync(string accessToken, string owner,
        string repo)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            var prRequest = new PullRequestRequest
            {
                State = ItemStateFilter.Open,
                SortProperty = PullRequestSort.Updated,
                SortDirection = SortDirection.Descending
            };

            return await client.PullRequest.GetAllForRepository(owner, repo, prRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull requests for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<GitHubCommit>> GetRepositoryCommitsAsync(string accessToken, string owner,
        string repo, int count = 10)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            var commitsRequest = new CommitRequest
            {
                Since = DateTimeOffset.Now.AddDays(-30)
            };

            var commits = await client.Repository.Commit.GetAll(owner, repo, commitsRequest);
            return commits.Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commits for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<Release>> GetRepositoryReleasesAsync(string accessToken, string owner, string repo)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            return await client.Repository.Release.GetAll(owner, repo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting releases for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<Organization>> GetUserOrganizationsAsync(string accessToken)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
            {
                Credentials = new Credentials(accessToken)
            };

            return await client.Organization.GetAllForCurrent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user organizations");
            throw;
        }
    }

    public bool IsValidAuthState(string state)
    {
        return _authStates.ContainsKey(state);
    }

    public long? GetTelegramUserIdByState(string state)
    {
        return _authStates.TryGetValue(state, out var authState) ? authState.TelegramUserId : null;
    }

    public void CleanExpiredStates()
    {
        var expiredStates = _authStates
            .Where(kvp => DateTime.UtcNow - kvp.Value.CreatedAt > TimeSpan.FromMinutes(10))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var state in expiredStates)
        {
            _authStates.TryRemove(state, out _);
        }
    }
}