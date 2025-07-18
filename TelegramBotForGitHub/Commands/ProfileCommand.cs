using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

public class ProfileCommand : TextBasedCommand
{
    protected override string Pattern => "profile";

    private readonly ITelegramBotService _telegramBotService;
    private readonly IUserSessionService _userSessionService;
    private readonly IGitHubService _gitHubService;

    public ProfileCommand(
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
                               "You need to authorize with GitHub first to view your profile.\n\n" +
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
            var user = await _gitHubService.GetCurrentUserAsync(session.GitHubToken);

            session.GitHubUsername = user.Login;
            await _userSessionService.UpdateUserSessionAsync(session);

            var profileMessage = $"👤 <b>GitHub Profile</b>\n\n" +
                               $"🏷️ <b>Username:</b> {user.Login}\n" +
                               $"📛 <b>Name:</b> {user.Name ?? "Not specified"}\n" +
                               $"📧 <b>Email:</b> {user.Email ?? "Not public"}\n" +
                               $"🏢 <b>Company:</b> {user.Company ?? "Not specified"}\n" +
                               $"📍 <b>Location:</b> {user.Location ?? "Not specified"}\n" +
                               $"📝 <b>Bio:</b> {user.Bio ?? "Not specified"}\n\n" +
                               $"📊 <b>Statistics:</b>\n" +
                               $"📚 Public repositories: {user.PublicRepos}\n" +
                               $"👥 Followers: {user.Followers}\n" +
                               $"👤 Following: {user.Following}\n\n" +
                               $"📅 <b>Joined:</b> {user.CreatedAt:dd.MM.yyyy}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("🔗 View on GitHub", user.HtmlUrl)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📚 View Repositories", "show_repositories"),
                    InlineKeyboardButton.WithCallbackData("🔄 Refresh", "show_profile")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, profileMessage, ParseMode.Html);
        }
        catch (Exception)
        {
            var errorMessage = "❌ <b>Error loading profile</b>\n\n" +
                             "Failed to load your GitHub profile. This might be due to:\n" +
                             "• Network issues\n" +
                             "• Expired authorization\n\n" +
                             "Try authorizing again with /auth command.";

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
