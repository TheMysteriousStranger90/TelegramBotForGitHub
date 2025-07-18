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
            Scopes = { "repo", "user" },
            State = state
        };

        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string state)
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

            return token.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for token");
            throw;
        }
    }

    public async Task<User> GetCurrentUserAsync(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
        {
            Credentials = new Credentials(accessToken)
        };

        return await client.User.Current();
    }

    public async Task<IReadOnlyList<Repository>> GetUserRepositoriesAsync(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
        {
            Credentials = new Credentials(accessToken)
        };

        return await client.Repository.GetAllForCurrent();
    }

    public async Task<Repository> GetRepositoryAsync(string accessToken, string owner, string repo)
    {
        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
        {
            Credentials = new Credentials(accessToken)
        };

        return await client.Repository.Get(owner, repo);
    }

    public async Task<IReadOnlyList<Issue>> GetRepositoryIssuesAsync(string accessToken, string owner, string repo)
    {
        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
        {
            Credentials = new Credentials(accessToken)
        };

        return await client.Issue.GetAllForRepository(owner, repo);
    }

    public async Task<Issue> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body)
    {
        var client = new GitHubClient(new ProductHeaderValue("TelegramBotForGitHub"))
        {
            Credentials = new Credentials(accessToken)
        };

        var issue = new NewIssue(title) { Body = body };
        return await client.Issue.Create(owner, repo, issue);
    }
}