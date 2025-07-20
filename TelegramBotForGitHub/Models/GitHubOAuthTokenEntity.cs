using Azure;
using Azure.Data.Tables;

namespace TelegramBotForGitHub.Models;

public class GitHubOAuthTokenEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "GitHubOAuthToken";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public static GitHubOAuthTokenEntity FromGitHubOAuthToken(GitHubOAuthToken token)
    {
        return new GitHubOAuthTokenEntity
        {
            RowKey = token.UserId.ToString(),
            UserId = token.UserId,
            AccessToken = token.AccessToken,
            TokenType = token.TokenType,
            Scope = token.Scope,
            CreatedAt = token.CreatedAt,
            IsActive = token.IsActive
        };
    }

    public GitHubOAuthToken ToGitHubOAuthToken()
    {
        return new GitHubOAuthToken
        {
            Id = RowKey,
            UserId = UserId,
            AccessToken = AccessToken,
            TokenType = TokenType,
            Scope = Scope,
            CreatedAt = CreatedAt,
            IsActive = IsActive
        };
    }
}