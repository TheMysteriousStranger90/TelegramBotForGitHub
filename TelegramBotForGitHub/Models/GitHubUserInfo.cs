using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubUserInfo
{
    [JsonProperty("id")]
    public long Id { get; set; }
    
    [JsonProperty("login")]
    public string Login { get; set; } = null!;
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("email")]
    public string? Email { get; set; }
    
    [JsonProperty("avatar_url")]
    public string? AvatarUrl { get; set; }
    
    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; } = null!;
    
    [JsonProperty("public_repos")]
    public int PublicRepos { get; set; }
    
    [JsonProperty("followers")]
    public int Followers { get; set; }
    
    [JsonProperty("following")]
    public int Following { get; set; }
}