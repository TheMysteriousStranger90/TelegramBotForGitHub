namespace TelegramBotForGitHub.Models;

public class GitHubAuthState
{
    public long TelegramUserId { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}