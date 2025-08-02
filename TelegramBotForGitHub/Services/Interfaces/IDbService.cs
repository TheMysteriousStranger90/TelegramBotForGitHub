using TelegramBotForGitHub.Models;

namespace TelegramBotForGitHub.Services.Interfaces
{
    public interface IDbService
    {
        // GitHub OAuth Methods
        Task<GitHubOAuthToken?> GetGitHubTokenAsync(long userId);
        Task CreateOrUpdateTokenAsync(GitHubOAuthToken token);
        Task DeactivateUserTokenAsync(long userId);

        // GitHub Auth State Methods
        Task CreateAuthStateAsync(GitHubAuthState authState);
        Task<GitHubAuthState?> GetAuthStateAsync(string state);
        Task UpdateAuthStateAsync(GitHubAuthState authState);

        // Chat Subscription Methods
        Task<List<ChatSubscription>> GetSubscriptionsAsync(string repositoryUrl);
        Task<ChatSubscription?> GetSubscriptionAsync(long chatId, string repositoryUrl);
        Task CreateSubscriptionAsync(ChatSubscription subscription);
        Task UpdateSubscriptionAsync(ChatSubscription subscription);
        Task<List<ChatSubscription>> GetChatSubscriptionsAsync(long chatId);
        Task<List<ChatSubscription>> GetSubscriptionsForRepositoryAsync(string repositoryUrl);

        // Notification Log Methods
        Task LogNotificationEntityAsync(NotificationLogEntity entity);
        Task<List<NotificationLogEntity>> GetNotificationLogsAsync(long chatId);
    }
}