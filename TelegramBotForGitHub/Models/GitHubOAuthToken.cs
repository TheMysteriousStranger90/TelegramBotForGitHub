using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubOAuthToken
{
    [JsonProperty("id")]
    public string Id { get; set; }
        
    [JsonProperty("userId")]
    public long UserId { get; set; }
        
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
        
    [JsonProperty("tokenType")]
    public string TokenType { get; set; } = "bearer";
        
    [JsonProperty("scope")]
    public string Scope { get; set; } = string.Empty;
        
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
        
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;

    public GitHubOAuthToken()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
    }
}