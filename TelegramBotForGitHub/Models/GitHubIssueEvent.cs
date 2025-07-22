using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubIssueEvent
{
    [JsonProperty("action")]
    public string? Action { get; set; }
    
    [JsonProperty("issue")]
    public GitHubIssue? Issue { get; set; }
    
    [JsonProperty("repository")]
    public GitHubRepository? Repository { get; set; }
    
    [JsonProperty("sender")]
    public GitHubUserProfile? Sender { get; set; }
}