using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class HelpCommand : TextBasedCommand
{
    protected override string Pattern => "help";

    private const string HelpMessage =
        @"📖 **GitHub Bot Commands:**

**Authentication:**
/auth - Authorize with GitHub
/profile - Show your GitHub profile
/logout - Logout from GitHub
/status - Show bot and authorization status

**Repository Management:**
/myrepos - Show your repositories
/subscribe <repo> - Subscribe to repository notifications
/unsubscribe <repo> - Unsubscribe from repository
/repos - List your subscribed repositories

**Notifications:**
/notifications - Show recent GitHub notifications
/clearnotifications - Mark all notifications as read

**Info:**
/help - Show this help message
/start - Show welcome message
/ping - Test bot connection

**Examples:**
/subscribe microsoft/dotnet
/unsubscribe microsoft/dotnet

**Notification Types:**
• 🔄 Push events
• 🔀 Pull requests (opened, closed, merged)
• 🐛 Issues (opened, closed, reopened)
• ⭐ Stars and forks
• 🚀 Releases
• ✅ CI/CD status updates

**Tips:**
• Use /auth first to connect your GitHub account
• Subscribe to repositories to get real-time notifications
• Check /notifications regularly for updates";

    private readonly ITelegramBotClient _telegramClient;

    public HelpCommand(ITelegramBotClient telegramClient) => _telegramClient = telegramClient;

    public override async Task Execute(Message message) =>
        await _telegramClient.SendMessage(
            chatId: message.Chat.Id,
            text: HelpMessage,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: CancellationToken.None);
}