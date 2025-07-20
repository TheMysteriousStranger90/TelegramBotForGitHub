using System.Text.Json.Serialization;

namespace TelegramBotForGitHub.Models;

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;
    
    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }
    
    [JsonPropertyName("followers")]
    public int Followers { get; set; }
    
    [JsonPropertyName("following")]
    public int Following { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("blog")]
    public string Blog { get; set; } = string.Empty;
}