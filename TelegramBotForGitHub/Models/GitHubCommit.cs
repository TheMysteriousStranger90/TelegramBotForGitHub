using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubCommit
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    
    [JsonProperty("message")]
    public string? Message { get; set; }
    
    [JsonProperty("author")]
    public GitHubCommitAuthor? Author { get; set; }
    
    [JsonProperty("url")]
    public string? Url { get; set; }
    
    [JsonProperty("distinct")]
    public bool Distinct { get; set; }
}