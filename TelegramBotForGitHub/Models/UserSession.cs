using System.Text.Json.Serialization;

namespace TelegramBotForGitHub.Models;

public class UserSession
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonPropertyName("telegramUserId")]
    public long TelegramUserId { get; set; }

    [JsonPropertyName("gitHubToken")]
    public string? GitHubToken { get; set; }

    [JsonPropertyName("gitHubUsername")]
    public string? GitHubUsername { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}