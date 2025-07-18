using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Configuration;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserSessionService _userSessionService;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        IOptions<BotConfiguration> config,
        IUserSessionService userSessionService,
        IGitHubService gitHubService,
        ILogger<TelegramBotService> logger)
    {
        _botClient = new TelegramBotClient(config.Value.TelegramBotToken);
        _userSessionService = userSessionService;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var messageText = message.Text;

        if (string.IsNullOrEmpty(messageText))
            return;

        var isAuthorized = await _userSessionService.IsUserAuthorizedAsync(chatId);

        switch (messageText.ToLower())
        {
            case "/start":
                await HandleStartCommand(chatId);
                break;
            case "/login":
                await HandleLoginCommand(chatId);
                break;
            case "/logout":
                await HandleLogoutCommand(chatId);
                break;
            case "/repos":
                if (isAuthorized)
                    await HandleReposCommand(chatId);
                else
                    await SendMessageAsync(chatId, "❌ Please login first with /login");
                break;
            case "/profile":
                if (isAuthorized)
                    await HandleProfileCommand(chatId);
                else
                    await SendMessageAsync(chatId, "❌ Please login first with /login");
                break;
            default:
                await HandleDefaultMessage(chatId, isAuthorized);
                break;
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message?.Chat.Id ?? 0;
        var data = callbackQuery.Data;

        if (string.IsNullOrEmpty(data))
            return;

        await _botClient.AnswerCallbackQuery(callbackQuery.Id);
    }

    private async Task HandleStartCommand(long chatId)
    {
        var session = await _userSessionService.GetUserSessionAsync(chatId);
        if (session == null)
        {
            await _userSessionService.CreateUserSessionAsync(chatId);
        }

        var welcomeMessage = "🤖 Welcome to the Telegram bot for GitHub!\n\n" +
                             "Available commands:\n" +
                             "/login - Authenticate with GitHub\n" +
                             "/logout - Logout from GitHub\n" +
                             "/profile - View your GitHub profile\n" +
                             "/repos - List your repositories\n\n" +
                             "To get started, please authenticate with /login";

        await SendMessageAsync(chatId, welcomeMessage);
    }

    private async Task HandleLoginCommand(long chatId)
    {
        var isAuthorized = await _userSessionService.IsUserAuthorizedAsync(chatId);
        if (isAuthorized)
        {
            await SendMessageAsync(chatId, "✅ You are already authenticated with GitHub!");
            return;
        }

        var authUrl = _gitHubService.GetAuthorizationUrl(chatId);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithUrl("🔗 Authenticate with GitHub", authUrl)
        });

        await _botClient.SendMessage(
            chatId,
            "🔐 Click the button below to authenticate with GitHub:",
            replyMarkup: keyboard
        );
    }

    private async Task HandleLogoutCommand(long chatId)
    {
        var session = await _userSessionService.GetUserSessionAsync(chatId);
        if (session != null)
        {
            session.GitHubToken = null;
            session.GitHubUsername = null;
            await _userSessionService.UpdateUserSessionAsync(session);
        }

        await SendMessageAsync(chatId, "✅ You have been logged out from GitHub");
    }

    private async Task HandleReposCommand(long chatId)
    {
        try
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            if (session?.GitHubToken == null)
            {
                await SendMessageAsync(chatId, "❌ Token not found. Please login again.");
                return;
            }

            await SendMessageAsync(chatId, "🔄 Fetching repository list...");

            var repos = await _gitHubService.GetUserRepositoriesAsync(session.GitHubToken);

            if (!repos.Any())
            {
                await SendMessageAsync(chatId, "📁 You have no repositories.");
                return;
            }

            var message = "📚 Your repositories:\n\n";
            foreach (var repo in repos.Take(10))
            {
                message += $"🔗 <a href='{repo.HtmlUrl}'>{repo.FullName}</a>\n";
                message += $"⭐ {repo.StargazersCount} | 🍴 {repo.ForksCount}\n";
                message += $"📝 {repo.Description ?? "No description"}\n\n";
            }

            if (repos.Count > 10)
            {
                message += $"... and {repos.Count - 10} more repositories.";
            }

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repositories for user {ChatId}", chatId);
            await SendMessageAsync(chatId, "❌ Failed to retrieve repositories. Please try again later.");
        }
    }

    private async Task HandleProfileCommand(long chatId)
    {
        try
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            if (session?.GitHubToken == null)
            {
                await SendMessageAsync(chatId, "❌ Token not found. Please login again.");
                return;
            }

            var user = await _gitHubService.GetCurrentUserAsync(session.GitHubToken);

            var message = $"👤 GitHub Profile:\n\n" +
                          $"🏷️ Username: {user.Login}\n" +
                          $"📛 Name: {user.Name ?? "Not specified"}\n" +
                          $"📧 Email: {user.Email ?? "Not specified"}\n" +
                          $"🏢 Company: {user.Company ?? "Not specified"}\n" +
                          $"📍 Location: {user.Location ?? "Not specified"}\n" +
                          $"📝 Bio: {user.Bio ?? "Not specified"}\n" +
                          $"📚 Public Repos: {user.PublicRepos}\n" +
                          $"👥 Followers: {user.Followers}\n" +
                          $"👤 Following: {user.Following}";

            await SendMessageAsync(chatId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile for user {ChatId}", chatId);
            await SendMessageAsync(chatId, "❌ Failed to retrieve profile. Please try again later.");
        }
    }

    private async Task HandleDefaultMessage(long chatId, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            await SendMessageAsync(chatId, "❌ Please login first with /login");
        }
        else
        {
            await SendMessageAsync(chatId, "🤔 Command not recognized. Use /start to see available commands.");
        }
    }

    public async Task SendMessageAsync(long chatId, string message)
    {
        await _botClient.SendMessage(chatId, message);
    }

    public async Task SendKeyboardAsync(long chatId, string message, List<List<string>> keyboard)
    {
        var keyboardMarkup = new ReplyKeyboardMarkup(
            keyboard.Select(row => row.Select(button => new KeyboardButton(button)).ToArray()).ToArray())
        {
            ResizeKeyboard = true
        };

        await _botClient.SendMessage(chatId, message, replyMarkup: keyboardMarkup);
    }
}