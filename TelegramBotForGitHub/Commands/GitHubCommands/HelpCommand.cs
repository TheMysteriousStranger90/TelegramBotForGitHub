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
/logout - Logout from GitHub

**Info:**
/help - Show this help message
/start - Show welcome message

**Profile:**
/profile - Show your GitHub profile
/myrepos - Show your GitHub repositories
/myissues - Show your GitHub issues
/mypullrequests - Show your GitHub pull requests
/mynotifications - Show your GitHub notifications

**Another User's Info:**
/userrepos <username> - Show user's repositories
/userissues <username> - Show user's issues
/userpullrequests <username> - Show user's pull requests
/usernotifications <username> - Show user's notifications

**Repository Management:**
/subrepos - List your subscribed repositories
/subscribe <repo> - Subscribe to repository notifications
/unsubscribe <repo> - Unsubscribe from repository
/subnotifications - List your subscribed notifications

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