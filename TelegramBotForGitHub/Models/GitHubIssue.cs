using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubIssue
{
    [JsonProperty("id")]
    public long Id { get; set; }
    
    [JsonProperty("number")]
    public int Number { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    [JsonProperty("body")]
    public string? Body { get; set; }
    
    [JsonProperty("state")]
    public string? State { get; set; }
    
    [JsonProperty("html_url")]
    public string? HtmlUrl { get; set; }
    
    [JsonProperty("user")]
    public GitHubUser? User { get; set; }
    
    [JsonProperty("labels")]
    public List<GitHubLabel>? Labels { get; set; }
}