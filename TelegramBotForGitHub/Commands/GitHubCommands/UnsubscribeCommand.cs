using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class UnsubscribeCommand : TextBasedCommand
{
    protected override string Pattern => "unsubscribe";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IDbService _dbService;
    private readonly ILogger<UnsubscribeCommand> _logger;

    public UnsubscribeCommand(ITelegramBotClient telegramClient, IDbService cosmosDbService, ILogger<UnsubscribeCommand> logger)
    {
        _telegramClient = telegramClient;
        _dbService = cosmosDbService;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts?.Length < 2)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Please specify a repository.\n\n" +
                      "**Usage:** `/unsubscribe owner/repo`\n\n" +
                      "**Example:** `/unsubscribe microsoft/dotnet`",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        var repository = parts?[1];
        var repositoryUrl = $"https://github.com/{repository}";

        try
        {
            var subscription = await _dbService.GetSubscriptionAsync(message.Chat.Id, repositoryUrl);
            
            if (subscription == null)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"❌ **No subscription found for {repository}**\n\n" +
                          $"You are not subscribed to this repository.\n" +
                          $"Use `/repos` to see your active subscriptions.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            subscription.IsActive = false;
            await _dbService.UpdateSubscriptionAsync(subscription);
            
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ **Unsubscribed from {repository}**\n\n" +
                      $"You will no longer receive notifications for this repository.\n\n" +
                      $"Use `/subscribe {repository}` to re-subscribe at any time.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from repository {Repository} for chat {ChatId}", repository, message.Chat.Id);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"❌ Failed to unsubscribe from {repository}\n\n" +
                      $"Please try again later.",
                cancellationToken: CancellationToken.None);
        }
    }
}