using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubPullRequestBranch
{
    [JsonProperty("ref")]
    public string? Ref { get; set; }
    
    [JsonProperty("sha")]
    public string? Sha { get; set; }
    
    [JsonProperty("repo")]
    public GitHubRepository? Repo { get; set; }
}