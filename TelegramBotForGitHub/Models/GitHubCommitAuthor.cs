using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubCommitAuthor
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("email")]
    public string? Email { get; set; }
    
    [JsonProperty("username")]
    public string? Username { get; set; }
}