using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubLabel
{
    [JsonProperty("id")]
    public long Id { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("color")]
    public string? Color { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
}