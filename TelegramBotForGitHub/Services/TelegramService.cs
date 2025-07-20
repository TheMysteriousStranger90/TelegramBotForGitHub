using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramService> _logger;
        private readonly CommandHandler _commandHandler;

        public TelegramService(ITelegramBotClient botClient, ILogger<TelegramService> logger, CommandHandler commandHandler)
        {
            _botClient = botClient;
            _logger = logger;
            _commandHandler = commandHandler;
        }

        public async Task HandleUpdateAsync(Message message)
        {
            try
            {
                _logger.LogInformation("TelegramService: Processing message from user {UserId}: '{Text}'", 
                    message.From?.Id, message.Text);
                
                if (message.Text != null)
                {
                    _logger.LogInformation("TelegramService: Message text is not null, calling command handler");
                    await _commandHandler.Execute(message);
                    _logger.LogInformation("TelegramService: Command handler executed successfully");
                }
                else
                {
                    _logger.LogInformation("TelegramService: Received non-text message from user {UserId}", message.From?.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelegramService: Error handling update for user {UserId}", message.From?.Id);
                
                try
                {
                    await _botClient.SendMessage(message.Chat.Id, 
                        "Sorry, I encountered an error while processing your message. Please try again later.");
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "TelegramService: Failed to send error message to user {UserId}", message.From?.Id);
                }
                
                throw;
            }
        }

        public async Task SendNotificationAsync(long chatId, string message)
        {
            try
            {
                await _botClient.SendMessage(chatId, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                _logger.LogInformation("Notification sent to chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task<string> FormatGitHubNotificationAsync(string eventType, object eventData)
        {
            return eventType switch
            {
                "push" => FormatPushNotification(eventData as GitHubPushEvent),
                "pull_request" => FormatPullRequestNotification(eventData as GitHubPullRequestEvent),
                "issues" => FormatIssueNotification(eventData as GitHubIssueEvent),
                _ => $"🔔 GitHub Event: {eventType}"
            };
        }

        private string FormatPushNotification(GitHubPushEvent? pushEvent)
        {
            if (pushEvent?.Repository == null || pushEvent.Pusher == null)
                return "🔔 New push event";

            var branchName = pushEvent.Ref?.Replace("refs/heads/", "") ?? "unknown";
            var commitCount = pushEvent.Commits?.Count ?? 0;
            
            var message = $"📤 <b>Push to {pushEvent.Repository.Name}</b>\n";
            message += $"👤 <b>Pusher:</b> {pushEvent.Pusher.Login}\n";
            message += $"🌿 <b>Branch:</b> {branchName}\n";
            message += $"📝 <b>Commits:</b> {commitCount}\n";

            if (pushEvent.Commits?.Any() == true)
            {
                message += "\n<b>Recent commits:</b>\n";
                foreach (var commit in pushEvent.Commits.Take(3))
                {
                    var shortId = commit.Id?.Substring(0, 7) ?? "unknown";
                    var commitMessage = commit.Message?.Split('\n')[0] ?? "No message";
                    message += $"• <code>{shortId}</code> {commitMessage}\n";
                }
            }

            if (!string.IsNullOrEmpty(pushEvent.Repository.HtmlUrl))
            {
                message += $"\n🔗 <a href=\"{pushEvent.Repository.HtmlUrl}\">View Repository</a>";
            }

            return message;
        }

        private string FormatPullRequestNotification(GitHubPullRequestEvent? prEvent)
        {
            if (prEvent?.PullRequest == null || prEvent.Repository == null)
                return "🔔 New pull request event";

            var actionEmoji = prEvent.Action switch
            {
                "opened" => "🟢",
                "closed" => "🔴",
                "reopened" => "🟡",
                "merged" => "🟣",
                _ => "🔔"
            };

            var message = $"{actionEmoji} <b>Pull Request {prEvent.Action}</b>\n";
            message += $"📂 <b>Repository:</b> {prEvent.Repository.Name}\n";
            message += $"🏷️ <b>#{prEvent.PullRequest.Number}:</b> {prEvent.PullRequest.Title}\n";
            message += $"👤 <b>Author:</b> {prEvent.PullRequest.User?.Login}\n";

            if (prEvent.PullRequest.Head?.Ref != null && prEvent.PullRequest.Base?.Ref != null)
            {
                message += $"🌿 <b>Branch:</b> {prEvent.PullRequest.Head.Ref} → {prEvent.PullRequest.Base.Ref}\n";
            }

            if (!string.IsNullOrEmpty(prEvent.PullRequest.HtmlUrl))
            {
                message += $"\n🔗 <a href=\"{prEvent.PullRequest.HtmlUrl}\">View Pull Request</a>";
            }

            return message;
        }

        private string FormatIssueNotification(GitHubIssueEvent? issueEvent)
        {
            if (issueEvent?.Issue == null || issueEvent.Repository == null)
                return "🔔 New issue event";

            var actionEmoji = issueEvent.Action switch
            {
                "opened" => "🆕",
                "closed" => "✅",
                "reopened" => "🔄",
                _ => "🔔"
            };

            var message = $"{actionEmoji} <b>Issue {issueEvent.Action}</b>\n";
            message += $"📂 <b>Repository:</b> {issueEvent.Repository.Name}\n";
            message += $"🏷️ <b>#{issueEvent.Issue.Number}:</b> {issueEvent.Issue.Title}\n";
            message += $"👤 <b>Author:</b> {issueEvent.Issue.User?.Login}\n";

            if (issueEvent.Issue.Labels?.Any() == true)
            {
                var labels = string.Join(", ", issueEvent.Issue.Labels.Select(l => l.Name));
                message += $"🏷️ <b>Labels:</b> {labels}\n";
            }

            if (!string.IsNullOrEmpty(issueEvent.Issue.HtmlUrl))
            {
                message += $"\n🔗 <a href=\"{issueEvent.Issue.HtmlUrl}\">View Issue</a>";
            }

            return message;
        }
    }
}