using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubRepository
{
    [JsonProperty("id")]
    public long Id { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("full_name")]
    public string? FullName { get; set; }
    
    [JsonProperty("html_url")]
    public string? HtmlUrl { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonProperty("private")]
    public bool Private { get; set; }
    
    [JsonProperty("owner")]
    public GitHubUser? Owner { get; set; }
}