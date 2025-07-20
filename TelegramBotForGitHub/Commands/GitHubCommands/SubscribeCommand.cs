using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services;
using TelegramBotForGitHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class SubscribeCommand : TextBasedCommand
{
    protected override string Pattern => "subscribe";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IDbService _dbService;
    private readonly IGitHubAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscribeCommand> _logger;

    public SubscribeCommand(
        ITelegramBotClient telegramClient, 
        IDbService cosmosDbService, 
        IGitHubAuthService authService, 
        IConfiguration configuration,
        ILogger<SubscribeCommand> logger)
    {
        _telegramClient = telegramClient;
        _dbService = cosmosDbService;
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts?.Length < 2)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Please specify a repository. \n\n" +
                      "**Usage:** `/subscribe owner/repo [events]`\n\n" +
                      "**Examples:**\n" +
                      "• `/subscribe microsoft/dotnet`\n" +
                      "• `/subscribe microsoft/dotnet push,pull_request`\n\n" +
                      "**Available events:** push, pull_request, issues",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        var repository = parts[1];
        
        if (!repository.Contains('/') || repository.Count(c => c == '/') != 1)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Invalid repository format. Use: `owner/repository`\n\n" +
                      "**Example:** `microsoft/dotnet`",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        var events = new List<string> { "push", "pull_request", "issues" };
        if (parts.Length > 2)
        {
            events = parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .Where(e => new[] { "push", "pull_request", "issues" }.Contains(e))
                .ToList();

            if (!events.Any())
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Invalid events. Available events: `push`, `pull_request`, `issues`",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }
        }

        try
        {
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;
            var repositoryUrl = $"https://github.com/{repository}";

            var existingSubscription = await _dbService.GetSubscriptionAsync(chatId, repositoryUrl);
            
            if (existingSubscription != null)
            {
                existingSubscription.Events = events;
                existingSubscription.IsActive = true;
                await _dbService.UpdateSubscriptionAsync(existingSubscription);
                
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ **Updated subscription for {repository}**\n\n" +
                          $"**Events:** {string.Join(", ", events)}\n\n" +
                          $"You'll receive notifications for these events.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
            }
            else
            {
                var subscription = new ChatSubscription
                {
                    ChatId = chatId,
                    RepositoryUrl = repositoryUrl,
                    Events = events
                };

                await _dbService.CreateSubscriptionAsync(subscription);
                
                var webhookUrl = GetWebhookUrl();
                var responseMessage = $"✅ **Subscribed to {repository}**\n\n" +
                                    $"**Events:** {string.Join(", ", events)}\n\n" +
                                    $"You'll receive notifications for these events.\n\n";

                if (!string.IsNullOrEmpty(webhookUrl))
                {
                    responseMessage += $"💡 **Tip:** Configure GitHub webhook to `{webhookUrl}` to receive real-time notifications.\n\n" +
                                     $"**Webhook setup:**\n" +
                                     $"1. Go to your repository Settings\n" +
                                     $"2. Click on \"Webhooks\" in the left sidebar\n" +
                                     $"3. Click \"Add webhook\"\n" +
                                     $"4. Set Payload URL to: `{webhookUrl}`\n" +
                                     $"5. Set Content type to: `application/json`\n" +
                                     $"6. Set Secret to your webhook secret\n" +
                                     $"7. Select events: {string.Join(", ", events)}";
                }

                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: responseMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to repository {Repository} for user {UserId}", repository, message.From?.Id);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"❌ Failed to subscribe to {repository}\n\n" +
                      $"Please try again later.",
                cancellationToken: CancellationToken.None);
        }
    }

    private string GetWebhookUrl()
    {
        var baseUrl = _configuration["BaseUrl"];
        
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogWarning("BaseUrl not configured in settings");
            return string.Empty;
        }

        baseUrl = baseUrl.TrimEnd('/');
        
        return $"{baseUrl}/api/webhook/github";
    }
}