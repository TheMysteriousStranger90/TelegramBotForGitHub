using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class ChatSubscription
{
    [JsonProperty("id")]
    public string Id { get; set; }
        
    [JsonProperty("chatId")]
    public long ChatId { get; set; }
        
    [JsonProperty("repositoryUrl")]
    public string RepositoryUrl { get; set; } = string.Empty;
        
    [JsonProperty("events")]
    public List<string> Events { get; set; } = new List<string>();
        
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;
        
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
        
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }
        
    [JsonProperty("userId")]
    public long? UserId { get; set; }
        
    public ChatSubscription()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}