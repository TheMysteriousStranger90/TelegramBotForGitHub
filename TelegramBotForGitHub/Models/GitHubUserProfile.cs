using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class GitHubUserProfile
{
    [JsonProperty("id")]
    public long Id { get; set; }
        
    [JsonProperty("login")]
    public string Login { get; set; } = string.Empty;
        
    [JsonProperty("name")]
    public string? Name { get; set; }
        
    [JsonProperty("email")]
    public string? Email { get; set; }
        
    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
        
    [JsonProperty("bio")]
    public string? Bio { get; set; }
        
    [JsonProperty("public_repos")]
    public int PublicRepos { get; set; }
        
    [JsonProperty("followers")]
    public int Followers { get; set; }
        
    [JsonProperty("following")]
    public int Following { get; set; }
        
    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
        
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
        
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}