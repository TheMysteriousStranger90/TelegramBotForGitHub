using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using System.Text;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class ProfileCommand : TextBasedCommand
{
    protected override string Pattern => "profile";
    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;

    public ProfileCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService)
    {
        _telegramClient = telegramClient;
        _authService = authService;
    }

    public override async Task Execute(Message message)
    {
        var userId = message.From!.Id;
        
        try
        {
            var isAuthorized = await _authService.IsUserAuthorizedAsync(userId);
            
            if (!isAuthorized)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "🔐 You need to authorize first. Use `/auth` command to connect your GitHub account.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var token = await _authService.GetUserTokenAsync(userId);
            if (token == null)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Authorization token not found. Please use `/auth` to authorize again.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var userInfo = await _authService.GetUserProfileAsync(token.AccessToken);
            if (userInfo == null)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Failed to retrieve GitHub profile. Please try again later.",
                    cancellationToken: CancellationToken.None);
                return;
            }

            var profileMessage = FormatProfileMessage(userInfo);

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: profileMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while fetching your profile. Please try again later.",
                cancellationToken: CancellationToken.None);
        }
    }

    private string FormatProfileMessage(GitHubUserProfile userInfo)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("👤 **GitHub Profile**");
        sb.AppendLine();
        
        sb.AppendLine($"**Name:** {EscapeMarkdown(userInfo.Name ?? userInfo.Login)}");
        sb.AppendLine($"**Username:** `{userInfo.Login}`");
        
        if (!string.IsNullOrEmpty(userInfo.Bio))
        {
            sb.AppendLine($"**Bio:** {EscapeMarkdown(userInfo.Bio)}");
        }
        
        sb.AppendLine($"**Public Repos:** {userInfo.PublicRepos}");
        sb.AppendLine($"**Followers:** {userInfo.Followers}");
        sb.AppendLine($"**Following:** {userInfo.Following}");
        sb.AppendLine($"**Joined:** {userInfo.CreatedAt:yyyy-MM-dd}");
        
        sb.AppendLine();
        sb.AppendLine($"🔗 [View on GitHub]({userInfo.HtmlUrl})");
        
        return sb.ToString();
    }

    private string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        
        var specialChars = new[] { '*', '_', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        
        var escaped = text;
        foreach (var ch in specialChars)
        {
            escaped = escaped.Replace(ch.ToString(), $"\\{ch}");
        }
        
        return escaped;
    }
}