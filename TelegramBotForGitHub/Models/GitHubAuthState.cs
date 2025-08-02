using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubAuthState
{
    [JsonProperty("id")]
    public string Id { get; set; }
        
    [JsonProperty("userId")]
    public long UserId { get; set; }
        
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
        
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
        
    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }
        
    [JsonProperty("isUsed")]
    public bool IsUsed { get; set; } = false;

    public GitHubAuthState()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
    }
}