using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Configuration;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserSessionService _userSessionService;
    private readonly IGitHubService _gitHubService;
    private readonly CommandHandler _commandHandler;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        IOptions<BotConfiguration> config,
        IUserSessionService userSessionService,
        IGitHubService gitHubService,
        CommandHandler commandHandler,
        ILogger<TelegramBotService> logger)
    {
        _botClient = new TelegramBotClient(config.Value.TelegramBotToken);
        _userSessionService = userSessionService;
        _gitHubService = gitHubService;
        _commandHandler = commandHandler;
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

        var session = await _userSessionService.GetUserSessionAsync(chatId);
        if (session == null)
        {
            session = await _userSessionService.CreateUserSessionAsync(chatId);
        }

        if (messageText.StartsWith('/'))
        {
            await _commandHandler.Execute(message);
        }
        else
        {
            await ShowMainMenu(chatId);
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message?.Chat.Id ?? 0;
        var data = callbackQuery.Data;

        if (string.IsNullOrEmpty(data) || chatId == 0)
            return;

        var isAuthorized = await _userSessionService.IsUserAuthorizedAsync(chatId);

        switch (data)
        {
            case "auth_github":
                await HandleGitHubAuth(chatId, isAuthorized);
                break;
            case "logout_github":
                await HandleLogout(chatId);
                break;
            case "show_profile":
                if (isAuthorized)
                    await ShowProfile(chatId);
                else
                    await ShowNotAuthorizedMessage(chatId);
                break;
            case "show_repositories":
                if (isAuthorized)
                    await ShowRepositories(chatId);
                else
                    await ShowNotAuthorizedMessage(chatId);
                break;
            case "show_issues":
                if (isAuthorized)
                    await ShowIssuesMenu(chatId);
                else
                    await ShowNotAuthorizedMessage(chatId);
                break;
            case "back_to_main":
                await ShowMainMenu(chatId);
                break;
            case "refresh_data":
                await ShowMainMenu(chatId, "🔄 Data refreshed");
                break;
            case "help":
                await ShowHelp(chatId);
                break;
            default:
                if (data.StartsWith("repo_"))
                    await HandleRepositoryAction(chatId, data);
                else if (data.StartsWith("issue_"))
                    await HandleIssueAction(chatId, data);
                break;
        }

        await _botClient.AnswerCallbackQuery(callbackQuery.Id);
    }
    
    private async Task ShowHelp(long chatId)
    {
        var helpMessage = $"📖 <b>GitHub Bot Help</b>\n\n" +
                          $"<b>📋 Available Commands:</b>\n" +
                          $"/start - Show welcome message\n" +
                          $"/auth - Authorize with GitHub\n" +
                          $"/profile - View GitHub profile\n" +
                          $"/repositories - View your repositories\n" +
                          $"/issues - Manage issues\n" +
                          $"/help - Show this help message";

        await _botClient.SendMessage(
            chatId,
            helpMessage,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
            )
        );
    }
            


    private async Task ShowMainMenu(long chatId, string? additionalMessage = null)
    {
        var isAuthorized = await _userSessionService.IsUserAuthorizedAsync(chatId);
        var message = "🤖 <b>GitHub Bot</b>\n\n";

        if (additionalMessage != null)
        {
            message += $"{additionalMessage}\n\n";
        }

        if (isAuthorized)
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            message += $"✅ You are authorized as: <b>{session?.GitHubUsername ?? "GitHub User"}</b>\n\n";
            message += "Select an action:";
        }
        else
        {
            message += "❌ You are not authorized with GitHub\n\n";
            message += "Please authorize to work with GitHub:";
        }

        var keyboard = CreateMainMenuKeyboard(isAuthorized);

        await _botClient.SendMessage(
            chatId,
            message,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private InlineKeyboardMarkup CreateMainMenuKeyboard(bool isAuthorized)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        if (!isAuthorized)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔐 Authorize", "auth_github")
            });
        }
        else
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("👤 Profile", "show_profile"),
                InlineKeyboardButton.WithCallbackData("📚 Repositories", "show_repositories")
            });

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🐛 Issues", "show_issues"),
                InlineKeyboardButton.WithCallbackData("🔄 Refresh", "refresh_data")
            });

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🚪 Logout", "logout_github")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private async Task HandleGitHubAuth(long chatId, bool isAuthorized)
    {
        if (isAuthorized)
        {
            await _botClient.SendMessage(
                chatId,
                "✅ You are already authorized with GitHub!",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
                )
            );
            return;
        }

        var authUrl = _gitHubService.GetAuthorizationUrl(chatId);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("🔗 Go to authorization", authUrl)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
            }
        });

        await _botClient.SendMessage(
            chatId,
            "🔐 <b>GitHub Authorization</b>\n\n" +
            "Click the button below to authorize.\n" +
            "After authorization, return to the bot and press 'Refresh'.",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleLogout(long chatId)
    {
        var session = await _userSessionService.GetUserSessionAsync(chatId);
        if (session != null)
        {
            session.GitHubToken = null;
            session.GitHubUsername = null;
            await _userSessionService.UpdateUserSessionAsync(session);
        }

        await ShowMainMenu(chatId, "✅ Successfully logged out of GitHub");
    }

    private async Task ShowProfile(long chatId)
    {
        try
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            if (session?.GitHubToken == null)
            {
                await ShowNotAuthorizedMessage(chatId);
                return;
            }

            var user = await _gitHubService.GetCurrentUserAsync(session.GitHubToken);

            session.GitHubUsername = user.Login;
            await _userSessionService.UpdateUserSessionAsync(session);

            var message = $"👤 <b>GitHub Profile</b>\n\n" +
                          $"🏷️ <b>Username:</b> {user.Login}\n" +
                          $"📛 <b>Name:</b> {user.Name ?? "Not specified"}\n" +
                          $"📧 <b>Email:</b> {user.Email ?? "Not specified"}\n" +
                          $"🏢 <b>Company:</b> {user.Company ?? "Not specified"}\n" +
                          $"📍 <b>Location:</b> {user.Location ?? "Not specified"}\n" +
                          $"📝 <b>Bio:</b> {user.Bio ?? "Not specified"}\n\n" +
                          $"📊 <b>Statistics:</b>\n" +
                          $"📚 Public repositories: {user.PublicRepos}\n" +
                          $"👥 Followers: {user.Followers}\n" +
                          $"👤 Following: {user.Following}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("🔗 View on GitHub", user.HtmlUrl) },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main") }
            });

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing profile for user {ChatId}", chatId);
            await _botClient.SendMessage(
                chatId,
                "❌ Error retrieving profile",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
                )
            );
        }
    }

    private async Task ShowRepositories(long chatId)
    {
        try
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            if (session?.GitHubToken == null)
            {
                await ShowNotAuthorizedMessage(chatId);
                return;
            }

            await _botClient.SendMessage(chatId, "🔄 Loading repositories...");

            var repos = await _gitHubService.GetUserRepositoriesAsync(session.GitHubToken);

            if (repos.Count == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    "📁 You have no repositories",
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
                    )
                );
                return;
            }

            var message = $"📚 <b>Your Repositories</b> ({repos.Count})\n\n";
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var repo in repos.Take(10))
            {
                message += $"🔗 <a href='{repo.HtmlUrl}'>{repo.FullName}</a>\n" +
                           $"⭐ {repo.StargazersCount} | 🍴 {repo.ForksCount} | 📝 {repo.Language ?? "N/A"}\n" +
                           (string.IsNullOrEmpty(repo.Description)
                               ? string.Empty
                               : $"💭 {repo.Description[..Math.Min(repo.Description.Length, 60)]}{(repo.Description.Length > 60 ? "..." : "")}\n") +
                           "\n";

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"📁 {repo.Name}", $"repo_{repo.Owner.Login}_{repo.Name}")
                });
            }

            if (repos.Count > 10)
            {
                message += $"... and {repos.Count - 10} more repositories";
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
            });

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing repositories for user {ChatId}", chatId);
            await _botClient.SendMessage(
                chatId,
                "❌ Error retrieving repositories",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
                )
            );
        }
    }

    private async Task ShowIssuesMenu(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Create Issue", "create_issue"),
                InlineKeyboardButton.WithCallbackData("👀 My Issues", "my_issues")
            },
            new[] { InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main") }
        });

        await _botClient.SendMessage(
            chatId,
            "🐛 <b>Issue Management</b>\n\nSelect an action:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleRepositoryAction(long chatId, string data)
    {
        var parts = data.Split('_', 3);
        if (parts.Length != 3) return;

        var owner = parts[1];
        var repoName = parts[2];

        try
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            if (session?.GitHubToken == null)
            {
                await ShowNotAuthorizedMessage(chatId);
                return;
            }

            var repo = await _gitHubService.GetRepositoryAsync(session.GitHubToken, owner, repoName);
            var issues = await _gitHubService.GetRepositoryIssuesAsync(session.GitHubToken, owner, repoName);

            var message = $"📁 <b>{repo.FullName}</b>\n\n" +
                          $"📝 {repo.Description ?? "No description"}\n\n" +
                          $"📊 <b>Statistics:</b>\n" +
                          $"⭐ Stars: {repo.StargazersCount}\n" +
                          $"🍴 Forks: {repo.ForksCount}\n" +
                          $"👀 Watchers: {repo.WatchersCount}\n" +
                          $"🐛 Open Issues: {issues.Count}\n" +
                          $"🏷️ Language: {repo.Language ?? "N/A"}\n" +
                          $"📅 Created: {repo.CreatedAt:dd.MM.yyyy}\n" +
                          $"🔄 Updated: {repo.UpdatedAt:dd.MM.yyyy}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("🔗 View on GitHub", repo.HtmlUrl) },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"🐛 Issues ({issues.Count})",
                        $"repo_issues_{owner}_{repoName}")
                },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Back to Repos", "show_repositories") }
            });

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing repository {Owner}/{Repo}", owner, repoName);
            await _botClient.SendMessage(
                chatId,
                "❌ Error retrieving repository information",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅️ Back", "show_repositories")
                )
            );
        }
    }

    private async Task HandleIssueAction(long chatId, string data)
    {
        await _botClient.SendMessage(
            chatId,
            "🚧 Feature under development",
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main")
            )
        );
    }

    private async Task ShowNotAuthorizedMessage(long chatId)
    {
        await _botClient.SendMessage(
            chatId,
            "❌ GitHub authorization required",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔐 Authorize", "auth_github") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Back", "back_to_main") }
            })
        );
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

    public async Task SendMessageAsync(long chatId, string message, ParseMode parseMode)
    {
        await _botClient.SendMessage(chatId, message, parseMode: parseMode);
    }
}