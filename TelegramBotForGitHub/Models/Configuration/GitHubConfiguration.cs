namespace TelegramBotForGitHub.Models.Configuration;

public class GitHubConfiguration
{
    public string Token { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}