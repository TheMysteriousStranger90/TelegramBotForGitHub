using Azure;
using Azure.Data.Tables;

namespace TelegramBotForGitHub.Models;

public class UserTokenEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "UserToken";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "bearer";
    public int ExpiresIn { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}