using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubPushEvent
{
    [JsonProperty("ref")]
    public string? Ref { get; set; }
    
    [JsonProperty("before")]
    public string? Before { get; set; }
    
    [JsonProperty("after")]
    public string? After { get; set; }
    
    [JsonProperty("repository")]
    public GitHubRepository? Repository { get; set; }
    
    [JsonProperty("pusher")]
    public GitHubUser? Pusher { get; set; }
    
    [JsonProperty("commits")]
    public List<GitHubCommit>? Commits { get; set; }
}