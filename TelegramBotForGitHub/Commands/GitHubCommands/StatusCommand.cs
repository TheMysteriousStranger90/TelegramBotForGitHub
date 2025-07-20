using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class StatusCommand : TextBasedCommand
{
    protected override string Pattern => "status";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly IDbService _dbService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommand(
        ITelegramBotClient telegramClient,
        IGitHubAuthService authService,
        IDbService cosmosDbService,
        IConfiguration configuration,
        ILogger<StatusCommand> logger)
    {
        _telegramClient = telegramClient;
        _authService = authService;
        _dbService = cosmosDbService;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        try
        {
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;
            
            var isAuthorized = await _authService.IsUserAuthorizedAsync(userId);
            var subscriptions = await _dbService.GetChatSubscriptionsAsync(chatId);
            var activeSubscriptions = subscriptions.Where(s => s.IsActive).Count();
            
            var statusMessage = $"📊 **Bot Status Report**\n\n" +
                               $"**User Information:**\n" +
                               $"• User ID: `{userId}`\n" +
                               $"• Chat ID: `{chatId}`\n" +
                               $"• Name: {message.From.FirstName} {message.From.LastName}\n\n" +
                               $"**GitHub Authorization:**\n" +
                               $"• Status: {(isAuthorized ? "✅ Authorized" : "❌ Not authorized")}\n";

            if (isAuthorized)
            {
                try
                {
                    var token = await _authService.GetUserTokenAsync(userId);
                    var profile = token != null ? await _authService.GetUserProfileAsync(token.AccessToken) : null;
                    
                    if (profile != null)
                    {
                        statusMessage += $"• GitHub User: {profile.Login}\n";
                        statusMessage += $"• Profile: {profile.Name}\n";
                    }
                    
                    if (token != null)
                    {
                        statusMessage += $"• Token Created: {token.CreatedAt:yyyy-MM-dd HH:mm}\n";
                        statusMessage += $"• Token Scope: {token.Scope}\n";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting detailed auth info for user {UserId}", userId);
                    statusMessage += $"• Details: Unable to fetch\n";
                }
            }

            statusMessage += $"\n**Subscriptions:**\n" +
                            $"• Active: {activeSubscriptions}\n" +
                            $"• Total: {subscriptions.Count}\n\n" +
                            $"**Bot Configuration:**\n" +
                            $"• Telegram Token: {(!string.IsNullOrEmpty(_configuration["Telegram:Token"]) ? "✅ Set" : "❌ Missing")}\n" +
                            $"• GitHub Client ID: {(!string.IsNullOrEmpty(_configuration["GitHub:ClientId"]) ? "✅ Set" : "❌ Missing")}\n" +
                            $"• GitHub Client Secret: {(!string.IsNullOrEmpty(_configuration["GitHub:ClientSecret"]) ? "✅ Set" : "❌ Missing")}\n" +
                            $"• Webhook Secret: {(!string.IsNullOrEmpty(_configuration["GitHub:WebhookSecret"]) ? "✅ Set" : "❌ Missing")}\n" +
                            $"• Base URL: {_configuration["BaseUrl"] ?? "❌ Missing"}\n\n" +
                            $"**Database:**\n" +
                            $"• Cosmos Endpoint: {_configuration["Cosmos:Endpoint"]}\n" +
                            $"• Database: {_configuration["Cosmos:Database"]}\n" +
                            $"• Container: {_configuration["Cosmos:Container"]}\n\n" +
                            $"**System:**\n" +
                            $"• Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                            $"• Environment: Development\n" +
                            $"• Version: 1.0.0\n\n";

            if (!isAuthorized)
            {
                statusMessage += "**Next Steps:**\n" +
                               "• Use `/auth` to authorize with GitHub\n" +
                               "• Use `/help` to see available commands\n";
            }
            else
            {
                statusMessage += "**Available Actions:**\n" +
                               "• `/profile` - View GitHub profile\n" +
                               "• `/myrepos` - List your repositories\n" +
                               "• `/subscribe owner/repo` - Subscribe to notifications\n" +
                               "• `/notifications` - Check GitHub notifications\n";
            }

            statusMessage += "\n🚀 All systems operational!";

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: statusMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for user {UserId}", message.From?.Id);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Error getting bot status.\n\n" +
                      "Please try again later.",
                cancellationToken: CancellationToken.None);
        }
    }
}