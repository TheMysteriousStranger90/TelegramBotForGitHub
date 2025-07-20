using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace TelegramBotForGitHub.Models;

public class ChatSubscriptionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "ChatSubscription";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long ChatId { get; set; }
    public string RepositoryUrl { get; set; } = string.Empty;
    public string EventsJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UserId { get; set; }

    public static ChatSubscriptionEntity FromChatSubscription(ChatSubscription subscription)
    {
        var rowKey = $"{subscription.ChatId}_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(subscription.RepositoryUrl))}";
            
        return new ChatSubscriptionEntity
        {
            RowKey = rowKey,
            ChatId = subscription.ChatId,
            RepositoryUrl = subscription.RepositoryUrl,
            EventsJson = JsonSerializer.Serialize(subscription.Events),
            IsActive = subscription.IsActive,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            UserId = subscription.UserId
        };
    }

    public ChatSubscription ToChatSubscription()
    {
        return new ChatSubscription
        {
            Id = RowKey,
            ChatId = ChatId,
            RepositoryUrl = RepositoryUrl,
            Events = JsonSerializer.Deserialize<List<string>>(EventsJson) ?? new List<string>(),
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            UserId = UserId
        };
    }
}