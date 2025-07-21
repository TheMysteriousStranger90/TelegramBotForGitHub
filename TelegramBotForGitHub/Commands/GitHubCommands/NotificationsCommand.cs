using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands
{
    public class NotificationsCommand : TextBasedCommand
    {
        protected override string Pattern => "notifications";

        private readonly ITelegramBotClient _telegramClient;
        private readonly IGitHubAuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationsCommand> _logger;

        public NotificationsCommand(
            ITelegramBotClient telegramClient,
            IGitHubAuthService authService,
            HttpClient httpClient,
            ILogger<NotificationsCommand> logger)
        {
            _telegramClient = telegramClient;
            _authService = authService;
            _httpClient = httpClient;
            _logger = logger;
        }

        public override async Task Execute(Message message)
        {
            var chatId = message.Chat.Id;
            var userId = message.From!.Id;

            // 1) Проверка авторизации в базе
            if (!await _authService.IsUserAuthorizedAsync(userId))
            {
                await _telegramClient.SendMessage(chatId,
                    "🔐 You need to authorize with GitHub first!\n\nUse `/auth` to connect your GitHub account.");
                return;
            }

            // 2) Получаем сохранённый токен
            var userToken = await _authService.GetUserTokenAsync(userId);
            if (userToken == null)
            {
                await _telegramClient.SendMessage(chatId,
                    "❌ Unable to retrieve your GitHub token.\n\nPlease try `/logout` and then `/auth` to re-authorize.");
                return;
            }

            try
            {
                // 3) Готовим HTTP-клиент
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {userToken.AccessToken}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                // 4) Делаем запрос уведомлений
                var url = "https://api.github.com/notifications?per_page=10";
                _logger.LogInformation("Requesting GitHub notifications: {Url}", url);
                var response = await _httpClient.GetAsync(url);

                // 5) Обработка ошибок
                if (!response.IsSuccessStatusCode)
                {
                    // токен протух
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await _telegramClient.SendMessage(chatId,
                            "🔐 Your GitHub token has expired.\n\nPlease use `/logout` and then `/auth` to re-authorize.");
                        return;
                    }

                    // нет нужных прав — Forbidden
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        // Логируем скоупы
                        response.Headers.TryGetValues("X-OAuth-Scopes", out var granted);
                        response.Headers.TryGetValues("X-Accepted-OAuth-Scopes", out var required);
                        _logger.LogWarning("Granted scopes: {Scopes}", granted is not null ? string.Join(", ", granted) : "(none)");
                        _logger.LogWarning("Required scopes: {Scopes}", required is not null ? string.Join(", ", required) : "(notifications)");

                        // Генерируем URL для переавторизации
                        var authUrl = await _authService.GetAuthorizationUrl(userId);

                        var grantedList = granted is not null ? string.Join(", ", granted) : "none";
                        var requiredList = required is not null ? string.Join(", ", required) : "notifications";

                        var sbErr = new StringBuilder();
                        sbErr.AppendLine("❌ Access forbidden.");
                        sbErr.AppendLine($"Granted scopes: `{grantedList}`");
                        sbErr.AppendLine($"Required scopes: `{requiredList}`");
                        sbErr.AppendLine("");
                        sbErr.AppendLine($"Please [re-authorize with GitHub]({authUrl}) granting at least `notifications` (or `repo`) scope.");

                        await _telegramClient.SendMessage(
                            chatId: chatId,
                            text: sbErr.ToString(),
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        return;
                    }

                    // остальные коды
                    await _telegramClient.SendMessage(chatId,
                        $"❌ Failed to get notifications.\n\nStatus: {response.StatusCode}\nPlease try again later.");
                    return;
                }

                // 6) Читаем и логируем сырые данные для дебага
                var raw = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw notifications response: {Json}", raw);

                // 7) Десериализация
                var notifications = JsonSerializer.Deserialize<JsonElement[]>(raw)
                                    ?? Array.Empty<JsonElement>();

                if (notifications.Length == 0)
                {
                    await _telegramClient.SendMessage(chatId,
                        "✅ No new notifications!\n\nYou're all caught up! 🎉");
                    return;
                }

                // 8) Формируем вывод (первые 5 уведомлений)
                var sb = new StringBuilder();
                sb.AppendLine("🔔 Recent Notifications:\n");

                foreach (var n in notifications.Take(5))
                {
                    var title     = n.GetProperty("subject").GetProperty("title").GetString();
                    var repo      = n.GetProperty("repository").GetProperty("full_name").GetString();
                    var type      = n.GetProperty("subject").GetProperty("type").GetString();
                    var reason    = n.GetProperty("reason").GetString();
                    var unread    = n.GetProperty("unread").GetBoolean();
                    var updatedAt = n.GetProperty("updated_at").GetString();

                    var lastUpdated = DateTime.TryParse(updatedAt, out var dt)
                        ? dt.ToString("MMM dd, HH:mm")
                        : "Unknown";

                    var typeEmoji = type switch
                    {
                        "Issue"       => "🐛",
                        "PullRequest" => "🔀",
                        "Release"     => "🚀",
                        "Discussion"  => "💬",
                        _             => "📄"
                    };

                    var reasonEmoji = reason switch
                    {
                        "author"           => "✍️",
                        "assign"           => "👤",
                        "comment"          => "💬",
                        "mention"          => "🏷️",
                        "review_requested" => "👀",
                        "team_mention"     => "👥",
                        "state_change"     => "🔄",
                        _                  => "📌"
                    };

                    sb.AppendLine($"{(unread ? "🔴" : "⚪")} *{title}*");
                    sb.AppendLine($"  📂 {repo}");
                    sb.AppendLine($"  {typeEmoji} {type}  {reasonEmoji} {reason}");
                    sb.AppendLine($"  📅 {lastUpdated}\n");
                }

                if (notifications.Length > 5)
                    sb.AppendLine($"... and {notifications.Length - 5} more notifications\n");

                sb.AppendLine("💡 Tip: Use `/clearnotifications` to mark all as read.");

                // 9) Отправляем пользователю
                await _telegramClient.SendMessage(
                    chatId: chatId,
                    text: sb.ToString(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "Error parsing GitHub notifications for user {UserId}", userId);
                await _telegramClient.SendMessage(chatId,
                    "❌ Error parsing GitHub response.\n\nPlease try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                await _telegramClient.SendMessage(chatId,
                    "❌ Unexpected error occurred.\n\nPlease try again later or contact support.");
            }
        }
    }
}
