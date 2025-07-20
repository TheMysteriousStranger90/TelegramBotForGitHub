using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubEventRecord
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    [JsonProperty("eventType")]
    public string EventType { get; set; } = null!;
    
    [JsonProperty("repository")]
    public string Repository { get; set; } = null!;
    
    [JsonProperty("message")]
    public string Message { get; set; } = null!;
    
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
}