namespace TelegramBotForGitHub.Configuration;

public class BotConfiguration
{
    public string TelegramBotToken { get; set; } = string.Empty;
    public GitHubConfiguration GitHub { get; set; } = new();
    public CosmosDbConfiguration CosmosDB { get; set; } = new();
}