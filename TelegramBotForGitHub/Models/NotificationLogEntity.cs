using Azure;
using Azure.Data.Tables;

namespace TelegramBotForGitHub.Models;

public class NotificationLogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "NotificationLog";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long ChatId { get; set; }
    public string RepositoryUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime CreatedAt { get; set; }
}