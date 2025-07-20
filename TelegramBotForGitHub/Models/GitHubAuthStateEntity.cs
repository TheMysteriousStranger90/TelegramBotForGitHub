using Azure;
using Azure.Data.Tables;

namespace TelegramBotForGitHub.Models;

public class GitHubAuthStateEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "GitHubAuthState";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long UserId { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public static GitHubAuthStateEntity FromGitHubAuthState(GitHubAuthState authState)
    {
        return new GitHubAuthStateEntity
        {
            RowKey = authState.Id,
            UserId = authState.UserId,
            State = authState.State,
            CreatedAt = authState.CreatedAt,
            ExpiresAt = authState.ExpiresAt,
            IsUsed = authState.IsUsed
        };
    }

    public GitHubAuthState ToGitHubAuthState()
    {
        return new GitHubAuthState
        {
            Id = RowKey,
            UserId = UserId,
            State = State,
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            IsUsed = IsUsed
        };
    }
}