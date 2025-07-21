using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;


public class GitHubPullRequestEvent
{
    [JsonProperty("action")]
    public string? Action { get; set; }
    
    [JsonProperty("number")]
    public int Number { get; set; }
    
    [JsonProperty("pull_request")]
    public GitHubPullRequest? PullRequest { get; set; }
    
    [JsonProperty("repository")]
    public GitHubRepository? Repository { get; set; }
    
    [JsonProperty("sender")]
    public GitHubUserProfile? Sender { get; set; }
}
