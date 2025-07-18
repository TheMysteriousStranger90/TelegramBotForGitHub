using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

public class ReposCommand : TextBasedCommand
{
    protected override string Pattern => "repos";

    private readonly ITelegramBotService _telegramBotService;
    private readonly IUserSessionService _userSessionService;
    private readonly IGitHubService _gitHubService;

    public ReposCommand(
        ITelegramBotService telegramBotService,
        IUserSessionService userSessionService,
        IGitHubService gitHubService)
    {
        _telegramBotService = telegramBotService;
        _userSessionService = userSessionService;
        _gitHubService = gitHubService;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var session = await _userSessionService.GetUserSessionAsync(chatId);

        if (session?.GitHubToken == null)
        {
            var notAuthMessage = "❌ <b>Not authorized</b>\n\n" +
                               "You need to authorize with GitHub first to view your repositories.\n\n" +
                               "Use /auth command to get started.";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔐 Authorize", "auth_github")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, notAuthMessage, ParseMode.Html);
            return;
        }

        try
        {
            await _telegramBotService.SendMessageAsync(chatId, "🔄 Loading repositories...");

            var repos = await _gitHubService.GetUserRepositoriesAsync(session.GitHubToken);

            InlineKeyboardMarkup? keyboard;
            if (repos.Count == 0)
            {
                var noReposMessage = "📁 <b>No repositories found</b>\n\n" +
                                   "You don't have any repositories yet.\n\n" +
                                   "Create your first repository on GitHub to get started!";

                keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("🔗 Create Repository", "https://github.com/new")
                    }
                });

                await _telegramBotService.SendMessageAsync(chatId, noReposMessage, ParseMode.Html);
                return;
            }

            var reposMessage = $"📚 <b>Your Repositories</b> ({repos.Count} total)\n\n";
            
            foreach (var repo in repos.Take(10))
            {
                reposMessage += $"🔗 <a href='{repo.HtmlUrl}'>{repo.FullName}</a>\n";
                reposMessage += $"⭐ {repo.StargazersCount} | 🍴 {repo.ForksCount} | 📝 {repo.Language ?? "N/A"}\n";
                
                if (!string.IsNullOrEmpty(repo.Description))
                {
                    var description = repo.Description.Length > 60 
                        ? repo.Description[..60] + "..." 
                        : repo.Description;
                    reposMessage += $"💭 {description}\n";
                }
                
                reposMessage += "\n";
            }

            if (repos.Count > 10)
            {
                reposMessage += $"... and {repos.Count - 10} more repositories";
            }

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 View Details", "show_repositories")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, reposMessage, ParseMode.Html);
        }
        catch (Exception)
        {
            var errorMessage = "❌ <b>Error loading repositories</b>\n\n" +
                             "Failed to load your repositories. Please try again later or re-authorize.";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔐 Re-authorize", "auth_github")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, errorMessage, ParseMode.Html);
        }
    }
}